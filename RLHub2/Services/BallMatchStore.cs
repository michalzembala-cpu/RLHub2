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

        // Only the matches belonging to the currently active account.
        public List<BallMatch> LoadForActive()
        {
            var mine = Helpers.Accounts.ActiveFilter();
            return Load().Where(m => mine(m.Account)).ToList();
        }

        public void Save(List<BallMatch> list)
        {
            try { File.WriteAllText(_path, JsonSerializer.Serialize(list ?? new List<BallMatch>(), Options)); }
            catch { }
        }

        // Upserts matches by Id (a re-fetched match replaces the old one so advanced
        // stats fill in), keeps newest, returns how many were brand new.
        public int Merge(IEnumerable<BallMatch> incoming)
        {
            var byId = new Dictionary<string, BallMatch>(StringComparer.OrdinalIgnoreCase);
            foreach (var m in Load())
                if (!string.IsNullOrEmpty(m.Id)) byId[m.Id] = m;

            int added = 0;
            foreach (var m in incoming)
            {
                if (string.IsNullOrEmpty(m.Id)) continue;
                if (!byId.ContainsKey(m.Id)) added++;
                byId[m.Id] = m;
            }

            var list = byId.Values.OrderByDescending(m => m.Date).Take(MaxKept).ToList();
            Save(list);
            return added;
        }
    }
}
