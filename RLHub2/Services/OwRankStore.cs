using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RLHub2.Models;

namespace RLHub2.Services
{
    // Local JSON history of Overwatch role ranks (%LocalAppData%\RLHub2\ow_ranks.json).
    public class OwRankStore
    {
        private const int MaxKept = 300;
        private readonly string _path;
        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        public OwRankStore()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RLHub2");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "ow_ranks.json");
        }

        public List<OwRankSnapshot> Load()
        {
            try
            {
                if (!File.Exists(_path)) return new List<OwRankSnapshot>();
                return JsonSerializer.Deserialize<List<OwRankSnapshot>>(File.ReadAllText(_path)) ?? new List<OwRankSnapshot>();
            }
            catch { return new List<OwRankSnapshot>(); }
        }

        public void Append(OwRankSnapshot snap)
        {
            var list = Load();
            list.Add(snap);
            if (list.Count > MaxKept) list.RemoveRange(0, list.Count - MaxKept);
            try { File.WriteAllText(_path, JsonSerializer.Serialize(list, Options)); } catch { }
        }

        public OwRankSnapshot? Latest() => Load().OrderByDescending(s => s.Time).FirstOrDefault();
    }
}
