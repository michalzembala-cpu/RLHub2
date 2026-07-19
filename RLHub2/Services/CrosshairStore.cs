using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using RLHub2.Models;

namespace RLHub2.Services
{
    // The user's own crosshairs, in %LocalAppData%\RLHub2\crosshairs.json.
    // The shipped pro presets live in code and are never written here.
    public class CrosshairStore
    {
        private readonly string _path;
        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        public CrosshairStore()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RLHub2");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "crosshairs.json");
        }

        public List<CrosshairDef> Load()
        {
            try
            {
                if (!File.Exists(_path)) return new List<CrosshairDef>();
                var list = JsonSerializer.Deserialize<List<CrosshairDef>>(File.ReadAllText(_path));
                if (list == null) return new List<CrosshairDef>();
                foreach (var c in list) c.Custom = true;   // anything in this file is the user's
                return list;
            }
            catch { return new List<CrosshairDef>(); }
        }

        public void Save(List<CrosshairDef> list)
        {
            try { File.WriteAllText(_path, JsonSerializer.Serialize(list, Options)); }
            catch { }
        }

        public void Add(CrosshairDef def)
        {
            var list = Load();
            def.Custom = true;
            list.Add(def);
            Save(list);
        }

        // Replaces by position, because two crosshairs may legitimately share a name.
        public void Replace(int index, CrosshairDef def)
        {
            var list = Load();
            if (index < 0 || index >= list.Count) return;
            def.Custom = true;
            list[index] = def;
            Save(list);
        }

        public void RemoveAt(int index)
        {
            var list = Load();
            if (index < 0 || index >= list.Count) return;
            list.RemoveAt(index);
            Save(list);
        }
    }
}
