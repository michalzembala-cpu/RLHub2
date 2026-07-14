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

            var ball = new BallMatchStore().LoadForActive().Where(m => m.Date.ToUniversalTime() >= start).ToList();
            var mmr = new MmrStore().LoadForActive().Where(e => e.Timestamp.ToUniversalTime() >= start).ToList();

            var snap = new SeasonSnapshot
            {
                Season = SeasonService.CurrentSeasonName,
                Account = Helpers.Accounts.ActiveName,
                InProgress = true,
                Matches = ball.Count,
            };

            if (ball.Count > 0)
            {
                snap.WinRate = (int)Math.Round(100.0 * ball.Count(m => m.Won) / ball.Count);

                var ranked = ball.Where(m => m.RankTier > 0).ToList();
                if (ranked.Count > 0)
                {
                    var peak = ranked.OrderByDescending(m => m.RankTier).ThenByDescending(m => m.RankDivision).First();
                    var final = ranked.OrderByDescending(m => m.Date).First();
                    snap.PeakRank = peak.RankName;
                    snap.FinalRank = final.RankName;
                }
            }

            snap.HighestMmr = mmr.Count > 0 ? mmr.Max(e => e.Value) : 0;
            return snap;
        }
    }
}
