using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace RLHub2.Services
{
    public class UpdateInfo
    {
        public bool Available { get; set; }
        public string Version { get; set; } = "";
        public string Notes { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string FileName { get; set; } = "";
        public long Size { get; set; }
        public string Error { get; set; } = "";
        public bool Ok => string.IsNullOrEmpty(Error);
    }

    // Checks GitHub Releases for a newer build and installs it.
    //
    // Nothing here invents a source: it only ever talks to the repository configured in
    // Settings, over HTTPS, and only downloads an asset attached to that repo's own release.
    // Until a release is published it reports that plainly rather than pretending to work.
    public class UpdateService
    {
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

        public static Version CurrentVersion =>
            Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);

        public static string CurrentVersionText
        {
            get { var v = CurrentVersion; return $"{v.Major}.{v.Minor}.{v.Build}"; }
        }

        // repo is "owner/name" — exactly what you see in the GitHub URL.
        public async Task<UpdateInfo> CheckAsync(string repo)
        {
            repo = (repo ?? "").Trim().Trim('/');
            if (repo.Length == 0 || !repo.Contains('/'))
                return new UpdateInfo { Error = "not-configured" };

            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get,
                    $"https://api.github.com/repos/{repo}/releases/latest");
                // GitHub rejects requests without a User-Agent.
                req.Headers.UserAgent.ParseAdd("RLHub2-Updater");
                req.Headers.Accept.ParseAdd("application/vnd.github+json");

                using var resp = await Http.SendAsync(req);
                if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return new UpdateInfo { Error = "no-release" };
                if (!resp.IsSuccessStatusCode)
                    return new UpdateInfo { Error = $"http {(int)resp.StatusCode}" };

                using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                var root = doc.RootElement;

                string tag = root.TryGetProperty("tag_name", out var t) ? t.GetString() ?? "" : "";
                string notes = root.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "";

                var latest = ParseVersion(tag);
                if (latest == null) return new UpdateInfo { Error = "bad-tag" };

                // Releases often carry several assets (installers, portable builds, other
                // architectures, docs). Taking the first match picks the wrong file — score them
                // instead, so an unrelated zip or an arm64 build can't be installed over an x64 app.
                string url = "", file = "";
                long size = 0;
                if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
                {
                    JsonElement best = default;
                    int bestScore = int.MinValue;

                    foreach (var a in assets.EnumerateArray())
                    {
                        string n = Name(a).ToLowerInvariant();
                        bool exe = n.EndsWith(".exe"), zip = n.EndsWith(".zip");
                        if (!exe && !zip) continue;

                        int score = 0;
                        if (n.Contains("rlhub")) score += 100;   // our own build wins outright
                        if (n.Contains("setup")) score += 50;    // an installer knows how to install
                        if (exe) score += 30; else score += 10;
                        if (n.Contains("arm")) score -= 60;      // wrong architecture
                        if (n.Contains("x86") && !n.Contains("x86_64")) score -= 20;

                        if (score > bestScore) { bestScore = score; best = a; }
                    }

                    if (best.ValueKind == JsonValueKind.Object)
                    {
                        file = Name(best);
                        url = best.TryGetProperty("browser_download_url", out var u) ? u.GetString() ?? "" : "";
                        size = best.TryGetProperty("size", out var s) && s.TryGetInt64(out long sv) ? sv : 0;
                    }
                }

                return new UpdateInfo
                {
                    Available = latest > CurrentVersion,
                    Version = $"{latest.Major}.{latest.Minor}.{latest.Build}",
                    Notes = notes,
                    DownloadUrl = url,
                    FileName = file,
                    Size = size,
                };
            }
            catch (Exception ex)
            {
                return new UpdateInfo { Error = ex.Message };
            }

            static string Name(JsonElement a)
                => a.ValueKind == JsonValueKind.Object && a.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
        }

        // "v1.2.3" / "1.2.3" / "v1.2" -> Version. Anything else is rejected rather than guessed.
        private static Version? ParseVersion(string tag)
        {
            tag = (tag ?? "").Trim();
            if (tag.StartsWith("v", StringComparison.OrdinalIgnoreCase)) tag = tag.Substring(1);
            var parts = tag.Split('.', '-', '+');
            if (parts.Length == 0) return null;

            int[] nums = new int[3];
            for (int i = 0; i < 3; i++)
                nums[i] = i < parts.Length && int.TryParse(parts[i], out int n) ? n : 0;

            return parts.Length > 0 && int.TryParse(parts[0], out _) ? new Version(nums[0], nums[1], nums[2], 0) : null;
        }

        public async Task<string?> DownloadAsync(UpdateInfo info, IProgress<int>? progress = null)
        {
            if (string.IsNullOrEmpty(info.DownloadUrl)) return null;
            try
            {
                var dir = Path.Combine(Path.GetTempPath(), "RLHub2Update");
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, string.IsNullOrEmpty(info.FileName) ? "RLHub2-update.exe" : info.FileName);

                using var resp = await Http.GetAsync(info.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                if (!resp.IsSuccessStatusCode) return null;

                long total = resp.Content.Headers.ContentLength ?? info.Size;
                using var src = await resp.Content.ReadAsStreamAsync();
                using var dst = File.Create(path);

                var buf = new byte[81920];
                long done = 0;
                int read;
                while ((read = await src.ReadAsync(buf)) > 0)
                {
                    await dst.WriteAsync(buf.AsMemory(0, read));
                    done += read;
                    if (total > 0) progress?.Report((int)(100 * done / total));
                }
                return path;
            }
            catch { return null; }
        }

        // A running .exe can't overwrite itself, so hand the swap to a tiny script that waits
        // for this process to exit first, then restarts the app and deletes itself.
        public static bool ApplyAndRestart(string downloadedPath)
        {
            try
            {
                string current = Environment.ProcessPath ?? Application.ExecutablePath;
                int pid = Environment.ProcessId;

                // An installer knows how to install itself — just run it and step aside.
                if (downloadedPath.EndsWith("setup.exe", StringComparison.OrdinalIgnoreCase))
                {
                    Process.Start(new ProcessStartInfo(downloadedPath) { UseShellExecute = true });
                    return true;
                }

                var bat = Path.Combine(Path.GetTempPath(), "RLHub2Update", "apply.cmd");
                File.WriteAllText(bat,
                    "@echo off\r\n" +
                    ":wait\r\n" +
                    $"tasklist /fi \"PID eq {pid}\" | find \"{pid}\" >nul\r\n" +
                    "if not errorlevel 1 (ping -n 2 127.0.0.1 >nul & goto wait)\r\n" +
                    $"copy /y \"{downloadedPath}\" \"{current}\" >nul\r\n" +
                    $"start \"\" \"{current}\"\r\n" +
                    "del \"%~f0\"\r\n");

                Process.Start(new ProcessStartInfo(bat)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(bat)!,
                });
                return true;
            }
            catch { return false; }
        }
    }
}
