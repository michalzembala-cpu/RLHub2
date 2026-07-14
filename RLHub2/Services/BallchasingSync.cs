using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RLHub2.Helpers;
using RLHub2.Models;

namespace RLHub2.Services
{
    public class SyncResult
    {
        public int Uploaded { get; set; }
        public int NewMatches { get; set; }
        public int MmrPoints { get; set; }
        public int Total { get; set; }
        public int Deleted { get; set; }
        public string Error { get; set; } = "";
        public bool Ok => string.IsNullOrEmpty(Error);
    }

    // Orchestrates: auto-upload new local replays (for ANY of the user's accounts) →
    // fetch parsed matches from ballchasing (tagged per account) → persist matches and
    // append approximated MMR to the chart.
    public class BallchasingSync
    {
        private const int DailyUploadCap = 10;    // max replays uploaded per calendar day
        private const int MaxUploadAttempts = 40;
        private const int MaxScanPerSync = 700;   // scan deep enough to reach older replays
        private const int HeaderScanBytes = 65536; // player names live in the header

        private static int _running; // guards against overlapping syncs

        public async Task<SyncResult> SyncAsync(Action<string>? progress = null)
        {
            if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
                return new SyncResult { Error = "busy" };

            try
            {
                var s = new SettingsStore();
                string key = s.LoadBallchasingKey();
                if (string.IsNullOrWhiteSpace(key)) return new SyncResult { Error = "no key" };
                if (Accounts.All.Count == 0) return new SyncResult { Error = "no accounts" };

                var svc = new BallchasingService();
                int uploaded = 0;
                if (s.LoadBallchasingAutoUpload())
                {
                    progress?.Invoke("upload");
                    uploaded = await UploadNewReplays(svc, key, progress);
                }

                progress?.Invoke("fetch");
                var matches = await svc.GetOwnMatchesAsync(60);

                int added = new BallMatchStore().Merge(matches);
                int mmrAdded = AppendMmr(matches);

                // Housekeeping: recycle local replays that are already safely on ballchasing.
                int deleted = CleanupOldReplays(s.LoadDeleteReplaysAfterDays(), progress);

                return new SyncResult
                {
                    Uploaded = uploaded,
                    NewMatches = added,
                    MmrPoints = mmrAdded,
                    Total = matches.Count,
                    Deleted = deleted
                };
            }
            catch (Exception ex) { return new SyncResult { Error = ex.Message }; }
            finally { Interlocked.Exchange(ref _running, 0); }
        }

        // Rocket League stores replays in a platform-specific folder: "Demos" (Steam),
        // "DemosEpic" (Epic), etc. Scan every "Demos*" folder under TAGame.
        private static List<FileInfo> DemoFiles()
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var tagame = Path.Combine(docs, "My Games", "Rocket League", "TAGame");
            var list = new List<FileInfo>();
            if (!Directory.Exists(tagame)) return list;
            foreach (var dir in Directory.GetDirectories(tagame, "Demos*"))
                list.AddRange(new DirectoryInfo(dir).GetFiles("*.replay"));
            return list;
        }

        private static async Task<int> UploadNewReplays(BallchasingService svc, string key, Action<string>? progress)
        {
            var quota = new UploadQuotaStore();
            int remaining = DailyUploadCap - quota.UsedToday();
            if (remaining <= 0) return 0;

            var uploadedStore = new UploadedStore();
            var done = uploadedStore.Load();

            var files = DemoFiles().OrderByDescending(f => f.LastWriteTimeUtc).ToList();
            if (files.Count == 0) return 0;

            // Only upload replays that one of OUR accounts played (matched by any of their
            // in-game names, including old names from before a rename).
            var names = Accounts.AllNames().ToList();
            bool filterByAccounts = names.Count > 0;

            // Header scans are the expensive part (a disk read per replay), and their answer
            // never changes — so ask each file only once, ever.
            var scanStore = new ReplayScanStore();
            var verdicts = filterByAccounts ? scanStore.Load(names) : new Dictionary<string, bool>();
            bool verdictsChanged = false;

            int uploaded = 0, attempts = 0, scans = 0;
            foreach (var f in files)
            {
                if (done.Contains(f.Name)) continue;
                if (uploaded >= remaining || attempts >= MaxUploadAttempts) break;

                if (filterByAccounts)
                {
                    if (!verdicts.TryGetValue(f.Name, out bool mine))
                    {
                        if (scans >= MaxScanPerSync) break;
                        scans++;
                        mine = FileContainsAnyName(f.FullName, names);
                        verdicts[f.Name] = mine;
                        verdictsChanged = true;
                    }
                    if (!mine) continue;
                }

                attempts++;
                progress?.Invoke($"upload {uploaded + 1}");
                var res = await svc.UploadReplayAsync(f.FullName, key);

                if (res == BallchasingService.UploadResult.RateLimited) break;
                if (res == BallchasingService.UploadResult.Uploaded) { done.Add(f.Name); uploaded++; }
                else if (res == BallchasingService.UploadResult.Duplicate) done.Add(f.Name);
                // Failed → leave unmarked so a transient error retries next sync

                await Task.Delay(1200);
            }

            if (verdictsChanged) scanStore.Save(verdicts, names);
            uploadedStore.Save(done);
            quota.Add(uploaded);
            return uploaded;
        }

        // Player names live as readable bytes in the replay header, so we can tell which
        // account a .replay belongs to without uploading or parsing it.
        private static bool FileContainsAnyName(string path, List<string> names)
        {
            try
            {
                using var fs = File.OpenRead(path);
                int len = (int)Math.Min(HeaderScanBytes, fs.Length);
                var buf = new byte[len];
                int read = fs.Read(buf, 0, len);
                string text = System.Text.Encoding.Latin1.GetString(buf, 0, read);
                foreach (var n in names)
                    if (text.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                return false;
            }
            catch { return false; }
        }

        // Sends local .replay files to the RECYCLE BIN (never a hard delete) once they are:
        //   1) confirmed uploaded to ballchasing (so the file can be downloaded back), and
        //   2) older than `days`.
        // Replays whose upload failed are never touched. 0 days = feature off.
        private static int CleanupOldReplays(int days, Action<string>? progress)
        {
            if (days <= 0) return 0;

            var done = new UploadedStore().Load();
            if (done.Count == 0) return 0;

            var cutoff = DateTime.Now.AddDays(-days);
            int deleted = 0;

            foreach (var f in DemoFiles())
            {
                if (!done.Contains(f.Name)) continue;      // not confirmed on ballchasing
                if (f.LastWriteTime > cutoff) continue;     // still within the keep window

                try
                {
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                        f.FullName,
                        Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                        Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                    deleted++;
                }
                catch { /* file locked / already gone — skip */ }
            }

            if (deleted > 0) progress?.Invoke($"recycled {deleted}");
            return deleted;
        }

        private static int AppendMmr(List<BallMatch> matches)
        {
            var store = new MmrStore();
            var entries = store.Load();
            int added = 0;

            foreach (var m in matches
                .Where(x => x.Ranked && x.Mode.Length > 0 && x.MmrApprox > 0)
                .OrderBy(x => x.Date))
            {
                bool exists = entries.Any(e =>
                    e.Account == m.Account &&
                    e.Mode == m.Mode &&
                    Math.Abs((e.Timestamp - m.Date).TotalMinutes) < 2);
                if (exists) continue;

                entries.Add(new MmrEntry(m.Date, m.MmrApprox, m.Mode) { Account = m.Account });
                added++;
            }

            if (added > 0)
            {
                entries = entries.OrderBy(e => e.Timestamp).ToList();
                store.Save(entries);
            }
            return added;
        }
    }
}
