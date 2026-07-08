using System;
using System.IO;
using System.Text.Json;

namespace RLHub2.Services
{
    // Tracks how many replays were uploaded today, so the per-day cap holds across
    // app launches (%LocalAppData%\RLHub2\upload_quota.json).
    public class UploadQuotaStore
    {
        private class Quota { public string Day { get; set; } = ""; public int Count { get; set; } }

        private readonly string _path;
        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        public UploadQuotaStore()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RLHub2");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "upload_quota.json");
        }

        private static string Today() => DateTime.Now.ToString("yyyy-MM-dd");

        private Quota Load()
        {
            try
            {
                if (File.Exists(_path))
                    return JsonSerializer.Deserialize<Quota>(File.ReadAllText(_path)) ?? new Quota();
            }
            catch { }
            return new Quota();
        }

        public int UsedToday()
        {
            var q = Load();
            return q.Day == Today() ? q.Count : 0;
        }

        public void Add(int n)
        {
            if (n <= 0) return;
            var q = Load();
            string today = Today();
            if (q.Day != today) { q.Day = today; q.Count = 0; }
            q.Count += n;
            try { File.WriteAllText(_path, JsonSerializer.Serialize(q, Options)); }
            catch { }
        }
    }
}
