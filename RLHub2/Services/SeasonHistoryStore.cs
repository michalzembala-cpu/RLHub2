using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RLHub2.Models;

namespace RLHub2.Services
{
    // Archived season snapshots (%LocalAppData%\RLHub2\season_history.json).
    public class SeasonHistoryStore
    {
        private readonly string _path;
        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        public SeasonHistoryStore()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RLHub2");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "season_history.json");
        }

        public List<SeasonSnapshot> Load()
        {
            try
            {
                if (!File.Exists(_path)) return new List<SeasonSnapshot>();
                return JsonSerializer.Deserialize<List<SeasonSnapshot>>(File.ReadAllText(_path)) ?? new List<SeasonSnapshot>();
            }
            catch { return new List<SeasonSnapshot>(); }
        }

        public void Save(List<SeasonSnapshot> list)
        {
            try { File.WriteAllText(_path, JsonSerializer.Serialize(list ?? new List<SeasonSnapshot>(), Options)); }
            catch { }
        }

        // Archived seasons for the currently active account.
        public List<SeasonSnapshot> LoadForActive()
        {
            var mine = Helpers.Accounts.ActiveFilter();
            return Load().Where(s => mine(s.Account)).ToList();
        }

        public bool Contains(string season, string account)
            => Load().Any(s => s.Season == season && s.Account == account);

        public void Add(SeasonSnapshot snap)
        {
            var list = Load();
            if (list.Any(s => s.Season == snap.Season && s.Account == snap.Account)) return;
            list.Add(snap);
            Save(list);
        }

        // Archive the active account's current season the first time it has actually ended.
        public void ArchiveIfEnded()
        {
            if (DateTime.UtcNow < SeasonService.CurrentSeasonEnd) return;
            string acc = Helpers.Accounts.ActiveName;
            if (Contains(SeasonService.CurrentSeasonName, acc)) return;

            var snap = SeasonStats.ComputeCurrent();
            snap.InProgress = false;
            snap.EndedOn = SeasonService.CurrentSeasonEnd;
            Add(snap);
        }
    }

    // Computes a season snapshot from the stored ballchasing matches + MMR entries.
    public static class SeasonStats
    {
        public static SeasonSnapshot ComputeCurrent()
        {
            var start = SeasonService.CurrentSeasonStart;

            // Matches and win rate come from the live Stats API (real games this season); the peak
            // MMR from the recorded MMR (now the real on-screen number). Ballchasing is not used —
            // it stopped returning ranks and is often empty, which left this whole section blank.
            var matches = new SessionStore().LoadForActive()
                .Where(m => m.Time.ToUniversalTime() >= start).ToList();
            var mmr = new MmrStore().LoadForActive()
                .Where(e => e.Timestamp.ToUniversalTime() >= start).ToList();

            var snap = new SeasonSnapshot
            {
                Season = SeasonService.CurrentSeasonName,
                Account = Helpers.Accounts.ActiveName,
                InProgress = true,
                Matches = matches.Count,
            };

            if (matches.Count > 0)
                snap.WinRate = (int)Math.Round(100.0 * matches.Count(m => m.Won) / matches.Count);

            // No rank name: RL hides MMR-to-rank, and the value we read is the MMR, not the tier.
            snap.HighestMmr = mmr.Count > 0 ? mmr.Max(e => e.Value) : 0;
            return snap;
        }
    }
}
