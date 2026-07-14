using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace RLHub2.Services
{
    // Remembers, per .replay file, whether one of the user's accounts actually played it.
    //
    // Answering that means reading the file's header off disk. A replay's contents never
    // change, so without this the sync re-read every replay that wasn't ours (and every one
    // still waiting behind the daily upload cap) on EVERY launch — hundreds of files, tens of
    // megabytes, forever. Keyed by file name, which is unique per replay.
    //
    // The verdict depends on which names we matched against, so the name list is stored with
    // it: add an account (or an alias) and every cached "not mine" is thrown away and re-scanned.
    //
    // Stored in %LocalAppData%\RLHub2\replay_scan.json
    public class ReplayScanStore
    {
        private class ScanCache
        {
            public string Names { get; set; } = "";
            public Dictionary<string, bool> Files { get; set; } = new();
        }

        private readonly string _path;
        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        public ReplayScanStore()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RLHub2");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "replay_scan.json");
        }

        private static string Signature(IEnumerable<string> names)
        {
            var sorted = new List<string>(names);
            sorted.Sort(StringComparer.OrdinalIgnoreCase);
            return string.Join("|", sorted).ToLowerInvariant();
        }

        // Cached verdicts, but only if they were produced for this exact set of names.
        public Dictionary<string, bool> Load(IEnumerable<string> names)
        {
            var empty = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!File.Exists(_path)) return empty;

                var cache = JsonSerializer.Deserialize<ScanCache>(File.ReadAllText(_path));
                if (cache?.Files == null || cache.Names != Signature(names)) return empty;

                return new Dictionary<string, bool>(cache.Files, StringComparer.OrdinalIgnoreCase);
            }
            catch { return empty; }
        }

        public void Save(Dictionary<string, bool> verdicts, IEnumerable<string> names)
        {
            try
            {
                var cache = new ScanCache { Names = Signature(names), Files = verdicts };
                File.WriteAllText(_path, JsonSerializer.Serialize(cache, Options));
            }
            catch { }
        }
    }
}
