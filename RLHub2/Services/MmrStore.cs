using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using RLHub2.Models;

namespace RLHub2.Services
{
    // Local JSON persistence for MMR entries.
    // Stored under %LocalAppData%\RLHub2\mmr_entries.json
    public class MmrStore
    {
        private readonly string _path;
        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        public MmrStore()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RLHub2");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "mmr_entries.json");
        }

        public bool FileExists => File.Exists(_path);

        public string FilePath => _path;

        public string DirectoryPath => Path.GetDirectoryName(_path) ?? _path;

        public List<MmrEntry> ImportFrom(string path)
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<MmrEntry>>(json) ?? new List<MmrEntry>();
        }

        public List<MmrEntry> Load()
        {
            try
            {
                if (!File.Exists(_path))
                    return new List<MmrEntry>();

                var json = File.ReadAllText(_path);
                var list = JsonSerializer.Deserialize<List<MmrEntry>>(json);
                return list ?? new List<MmrEntry>();
            }
            catch
            {
                return new List<MmrEntry>();
            }
        }

        public void Save(List<MmrEntry> entries)
        {
            var json = JsonSerializer.Serialize(entries ?? new List<MmrEntry>(), Options);
            File.WriteAllText(_path, json);
        }
    }
}
