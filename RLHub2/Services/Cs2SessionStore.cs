using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using RLHub2.Models;

namespace RLHub2.Services
{
    // Local JSON persistence for CS2 matches (%LocalAppData%\RLHub2\cs2_matches.json).
    public class Cs2SessionStore
    {
        private const int MaxKept = 200;
        private readonly string _path;
        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        public Cs2SessionStore()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RLHub2");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "cs2_matches.json");
        }

        public List<Cs2Match> Load()
        {
            try
            {
                if (!File.Exists(_path)) return new List<Cs2Match>();
                return JsonSerializer.Deserialize<List<Cs2Match>>(File.ReadAllText(_path)) ?? new List<Cs2Match>();
            }
            catch { return new List<Cs2Match>(); }
        }

        public void Save(List<Cs2Match> list)
        {
            try { File.WriteAllText(_path, JsonSerializer.Serialize(list ?? new List<Cs2Match>(), Options)); }
            catch { }
        }

        public void Append(Cs2Match m)
        {
            var list = Load();
            list.Add(m);
            if (list.Count > MaxKept) list.RemoveRange(0, list.Count - MaxKept);
            Save(list);
        }

        public void Clear() => Save(new List<Cs2Match>());
    }
}
