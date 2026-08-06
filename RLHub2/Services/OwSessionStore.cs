using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using RLHub2.Models;

namespace RLHub2.Services
{
    // Local JSON persistence for manually-logged Overwatch matches
    // (%LocalAppData%\RLHub2\ow_matches.json). Keeps the whole history plus the current session's
    // start time, so the page can show "this sitting" without losing the running totals.
    public class OwSessionStore
    {
        private const int MaxKept = 500;
        private readonly string _path;
        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        public class Data
        {
            public DateTime SessionStart { get; set; } = DateTime.Now;
            public List<OwMatch> Matches { get; set; } = new();
        }

        public OwSessionStore()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RLHub2");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "ow_matches.json");
        }

        public Data Load()
        {
            try
            {
                if (!File.Exists(_path)) return new Data();
                return JsonSerializer.Deserialize<Data>(File.ReadAllText(_path)) ?? new Data();
            }
            catch { return new Data(); }
        }

        public void Save(Data data)
        {
            try { File.WriteAllText(_path, JsonSerializer.Serialize(data ?? new Data(), Options)); }
            catch { }
        }

        public void Append(OwMatch m)
        {
            var data = Load();
            data.Matches.Add(m);
            if (data.Matches.Count > MaxKept)
                data.Matches.RemoveRange(0, data.Matches.Count - MaxKept);
            Save(data);
        }

        // Undo the most recent match (mis-click recovery).
        public void RemoveLast()
        {
            var data = Load();
            if (data.Matches.Count > 0)
            {
                data.Matches.RemoveAt(data.Matches.Count - 1);
                Save(data);
            }
        }

        // Start a fresh session without erasing history.
        public void NewSession()
        {
            var data = Load();
            data.SessionStart = DateTime.Now;
            Save(data);
        }
    }
}
