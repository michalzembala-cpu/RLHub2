using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace RLHub2.Services
{
    // Local JSON list of friend nicks (%LocalAppData%\RLHub2\friends.json).
    public class FriendStore
    {
        private readonly string _path;
        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        public FriendStore()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RLHub2");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "friends.json");
        }

        public List<string> Load()
        {
            try
            {
                if (!File.Exists(_path)) return new List<string>();
                return JsonSerializer.Deserialize<List<string>>(File.ReadAllText(_path)) ?? new List<string>();
            }
            catch { return new List<string>(); }
        }

        public void Save(List<string> friends)
        {
            try { File.WriteAllText(_path, JsonSerializer.Serialize(friends ?? new List<string>(), Options)); }
            catch { }
        }
    }
}
