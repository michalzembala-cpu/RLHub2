using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace RLHub2.Services
{
    public class RatingPoint
    {
        public DateTime Time { get; set; }
        public int Value { get; set; }
    }

    // Premier Rating, entered by hand. GSI does not expose rank or CS Rating (verified), and
    // Leetify — which does — forbids storing its data, so a rating the app keeps and charts has
    // to be the user's own entry. Same answer we reached for Rocket League MMR.
    //
    // Stored in %LocalAppData%\RLHub2\cs2_rating.json
    public class Cs2RatingStore
    {
        private readonly string _path;
        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        public Cs2RatingStore()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RLHub2");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "cs2_rating.json");
        }

        public List<RatingPoint> Load()
        {
            try
            {
                if (!File.Exists(_path)) return new List<RatingPoint>();
                var list = JsonSerializer.Deserialize<List<RatingPoint>>(File.ReadAllText(_path));
                return (list ?? new List<RatingPoint>()).OrderBy(p => p.Time).ToList();
            }
            catch { return new List<RatingPoint>(); }
        }

        public void Append(int value)
        {
            var list = Load();
            list.Add(new RatingPoint { Time = DateTime.Now, Value = value });
            try { File.WriteAllText(_path, JsonSerializer.Serialize(list, Options)); }
            catch { }
        }

        public RatingPoint? Latest() => Load().LastOrDefault();

        // The change from the previous entry — what the hero shows as "+86".
        public int Delta()
        {
            var list = Load();
            return list.Count < 2 ? 0 : list[^1].Value - list[^2].Value;
        }
    }
}
