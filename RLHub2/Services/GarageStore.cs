using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using RLHub2.Models;

namespace RLHub2.Services
{
    // Local JSON persistence for car presets and custom achievements.
    public class GarageStore
    {
        private readonly string _dir;
        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        public GarageStore()
        {
            _dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RLHub2");
            Directory.CreateDirectory(_dir);
        }

        private string PresetsPath => Path.Combine(_dir, "presets.json");
        private string AchievementsPath => Path.Combine(_dir, "achievements.json");

        public List<CarPreset> LoadPresets() => LoadList<CarPreset>(PresetsPath);
        public void SavePresets(List<CarPreset> p) => SaveList(PresetsPath, p);

        public List<Achievement> LoadAchievements() => LoadList<Achievement>(AchievementsPath);
        public void SaveAchievements(List<Achievement> a) => SaveList(AchievementsPath, a);

        public string ExportPresets(string path, List<CarPreset> presets)
        {
            File.WriteAllText(path, JsonSerializer.Serialize(presets ?? new List<CarPreset>(), Options));
            return path;
        }

        private static List<T> LoadList<T>(string path)
        {
            try
            {
                if (!File.Exists(path)) return new List<T>();
                return JsonSerializer.Deserialize<List<T>>(File.ReadAllText(path)) ?? new List<T>();
            }
            catch { return new List<T>(); }
        }

        private static void SaveList<T>(string path, List<T> list)
        {
            try { File.WriteAllText(path, JsonSerializer.Serialize(list ?? new List<T>(), Options)); }
            catch { }
        }
    }
}
