using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using RLHub2.Models;

namespace RLHub2.Services
{
    // Local JSON persistence for goals (%LocalAppData%\RLHub2\goals.json).
    public class GoalStore
    {
        private readonly string _path;
        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        public GoalStore()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RLHub2");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "goals.json");
        }

        public List<Goal> Load()
        {
            try
            {
                if (!File.Exists(_path)) return new List<Goal>();
                return JsonSerializer.Deserialize<List<Goal>>(File.ReadAllText(_path)) ?? new List<Goal>();
            }
            catch { return new List<Goal>(); }
        }

        public void Save(List<Goal> goals)
        {
            try { File.WriteAllText(_path, JsonSerializer.Serialize(goals ?? new List<Goal>(), Options)); }
            catch { }
        }
    }
}
