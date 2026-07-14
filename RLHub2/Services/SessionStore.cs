using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using RLHub2.Models;

namespace RLHub2.Services
{
    // Local JSON persistence for detected matches (%LocalAppData%\RLHub2\session_matches.json).
    public class SessionStore
    {
        private const int MaxKept = 200;
        private readonly string _path;
        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        public SessionStore()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RLHub2");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "session_matches.json");
        }

        public List<SessionMatch> Load()
        {
            try
            {
                if (!File.Exists(_path)) return new List<SessionMatch>();
                return JsonSerializer.Deserialize<List<SessionMatch>>(File.ReadAllText(_path)) ?? new List<SessionMatch>();
            }
            catch { return new List<SessionMatch>(); }
        }

        // Only the matches belonging to the currently active account.
        public List<SessionMatch> LoadForActive()
        {
            var mine = Helpers.Accounts.ActiveFilter();
            return Load().FindAll(m => mine(m.Account));
        }

        public void Save(List<SessionMatch> list)
        {
            try { File.WriteAllText(_path, JsonSerializer.Serialize(list ?? new List<SessionMatch>(), Options)); }
            catch { }
        }

        public void Append(SessionMatch m)
        {
            var list = Load();
            list.Add(m);
            if (list.Count > MaxKept) list.RemoveRange(0, list.Count - MaxKept);
            Save(list);
        }

        public void Clear() => Save(new List<SessionMatch>());
    }
}
