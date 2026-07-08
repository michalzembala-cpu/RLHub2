using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RLHub2.Models;

namespace RLHub2.Services
{
    public class SyncResult
    {
        public int Uploaded { get; set; }
        public int NewMatches { get; set; }
        public int MmrPoints { get; set; }
        public int Total { get; set; }
        public string Error { get; set; } = "";
        public bool Ok => string.IsNullOrEmpty(Error);
    }

    // Orchestrates: auto-upload new local replays → fetch parsed matches from
    // ballchasing → persist matches and append approximated MMR to the chart.
    public class BallchasingSync
    {
        private const int DailyUploadCap = 10; // max replays uploaded per calendar day
        private const int MaxUploadAttempts = 40;
        private const int MaxScanPerSync = 700;   // scan deep enough to reach the main account's older replays
        private const int HeaderScanBytes = 65536; // player names live in the first ~64 KB of the header

        private static int _running; // guards against overlapping syncs

        public async Task<SyncResult> SyncAsync(Action<string>? progress = null)
        {
            if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
                return new SyncResult { Error = "busy" };

            try
            {
                var s = new SettingsStore();
                string key = s.LoadBallchasingKey();
                string nick = s.LoadTrackedNick();
                if (string.IsNullOrWhiteSpace(key)) return new SyncResult { Error = "no key" };

                var svc = new BallchasingService();
                int uploaded = 0;
                if (s.LoadBallchasingAutoUpload())
                {
                    progress?.Invoke("upload");
                    uploaded = await UploadNewReplays(svc, key, nick, progress);
                }

                if (string.IsNullOrWhiteSpace(nick))
                    return new SyncResult { Uploaded = uploaded, Error = "no nick" };

                progress?.Invoke("fetch");
                // own uploads — track the chosen main account (or auto-detect if none set)
                var (matches, detected, autoDetected) = await svc.GetOwnMatchesAsync(nick, 30);

                // Only fill in the main account automatically when the user hasn't set one.
                // A user-chosen main is never overwritten.
                if (autoDetected && string.IsNullOrWhiteSpace(nick) && !string.IsNullOrWhiteSpace(detected))
                    s.SaveTrackedNick(detected);

                int added = new BallMatchStore().Merge(matches);
                int mmrAdded = AppendMmr(matches);

                return new SyncResult
                {
                    Uploaded = uploaded,
                    NewMatches = added,
                    MmrPoints = mmrAdded,
                    Total = matches.Count
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

        private static async Task<int> UploadNewReplays(BallchasingService svc, string key, string mainNick, Action<string>? progress)
        {
            var quota = new UploadQuotaStore();
            int remaining = DailyUploadCap - quota.UsedToday();
            if (remaining <= 0) return 0; // daily cap already reached

            var uploadedStore = new UploadedStore();
            var done = uploadedStore.Load();

            var files = DemoFiles()
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .ToList();
            if (files.Count == 0) return 0;

            // When a main account is set, only upload ITS replays (found by scanning the
            // replay header for the name), so quota isn't spent on other accounts' games.
            bool filterByMain = !string.IsNullOrWhiteSpace(mainNick);

            int uploaded = 0, attempts = 0, scans = 0;
            foreach (var f in files)
            {
                if (done.Contains(f.Name)) continue;
                if (uploaded >= remaining || attempts >= MaxUploadAttempts) break;

                if (filterByMain)
                {
                    if (scans >= MaxScanPerSync) break;        // bound disk reads (OneDrive-friendly)
                    scans++;
                    if (!FileContainsName(f.FullName, mainNick)) continue; // not a main-account replay
                }

                attempts++;

                progress?.Invoke($"upload {uploaded + 1}");
                var res = await svc.UploadReplayAsync(f.FullName, key);

                if (res == BallchasingService.UploadResult.RateLimited) break;
                if (res == BallchasingService.UploadResult.Uploaded)
                {
                    done.Add(f.Name);
                    uploaded++;
                }
                else if (res == BallchasingService.UploadResult.Duplicate)
                {
                    done.Add(f.Name); // already on ballchasing — never retry
                }
                // Failed → leave unmarked so a transient error retries next sync

                await Task.Delay(1200); // upload endpoint is rate-limited (~1/sec free)
            }

            uploadedStore.Save(done);
            quota.Add(uploaded);
            return uploaded;
        }

        // Rocket League stores player names as readable bytes in the replay header, so we
        // can tell which account a .replay belongs to without uploading/parsing it.
        private static bool FileContainsName(string path, string name)
        {
            try
            {
                using var fs = File.OpenRead(path);
                int len = (int)Math.Min(HeaderScanBytes, fs.Length);
                var buf = new byte[len];
                int read = fs.Read(buf, 0, len);
                string text = System.Text.Encoding.Latin1.GetString(buf, 0, read);
                return text.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch { return false; }
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
                bool exists = entries.Any(e => e.Mode == m.Mode &&
                    Math.Abs((e.Timestamp - m.Date).TotalMinutes) < 2);
                if (exists) continue;
                entries.Add(new MmrEntry(m.Date, m.MmrApprox, m.Mode));
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
