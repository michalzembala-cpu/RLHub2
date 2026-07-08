using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace RLHub2.Services
{
    // Tracks which local .replay files have already been uploaded to ballchasing,
    // so we never re-upload the same file (%LocalAppData%\RLHub2\uploaded_replays.json).
    public class UploadedStore
    {
        private readonly string _path;
        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        public UploadedStore()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RLHub2");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "uploaded_replays.json");
        }

        public HashSet<string> Load()
        {
            try
            {
                if (!File.Exists(_path)) return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var list = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(_path));
                return new HashSet<string>(list ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            }
            catch { return new HashSet<string>(StringComparer.OrdinalIgnoreCase); }
        }

        public void Save(HashSet<string> set)
        {
            try { File.WriteAllText(_path, JsonSerializer.Serialize(new List<string>(set), Options)); }
            catch { }
        }
    }
}
