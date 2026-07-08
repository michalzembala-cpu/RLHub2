using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RLHub2.Models;

namespace RLHub2.Services
{
    // Persists matches pulled from ballchasing (%LocalAppData%\RLHub2\ball_matches.json).
    public class BallMatchStore
    {
        private const int MaxKept = 400;
        private readonly string _path;
        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        public BallMatchStore()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RLHub2");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "ball_matches.json");
        }

        public List<BallMatch> Load()
        {
            try
            {
                if (!File.Exists(_path)) return new List<BallMatch>();
                return JsonSerializer.Deserialize<List<BallMatch>>(File.ReadAllText(_path)) ?? new List<BallMatch>();
            }
            catch { return new List<BallMatch>(); }
        }

        public void Save(List<BallMatch> list)
        {
            try { File.WriteAllText(_path, JsonSerializer.Serialize(list ?? new List<BallMatch>(), Options)); }
            catch { }
        }

        // Adds new matches (by Id), keeps newest, returns how many were new.
        public int Merge(IEnumerable<BallMatch> incoming)
        {
            var list = Load();
            var seen = new HashSet<string>(list.Select(m => m.Id), StringComparer.OrdinalIgnoreCase);
            int added = 0;
            foreach (var m in incoming)
            {
                if (string.IsNullOrEmpty(m.Id) || seen.Contains(m.Id)) continue;
                list.Add(m);
                seen.Add(m.Id);
                added++;
            }
            list = list.OrderByDescending(m => m.Date).Take(MaxKept).ToList();
            Save(list);
            return added;
        }
    }
}
