using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RLHub2.Models;

namespace RLHub2.Services
{
    // Generates plausible, deterministic profile data from the nickname.
    // (Real tracker.gg integration lives in ProfileServiceTracker.)
    public class ProfileServiceMock : IProfileService
    {
        public Task<Profile> GetProfileAsync(string nick)
        {
            nick = string.IsNullOrWhiteSpace(nick) ? "DemoPlayer" : nick.Trim();

            int seed = 17;
            foreach (char c in nick)
                seed = unchecked(seed * 31 + c);
            var rnd = new Random(seed & 0x7fffffff);

            var p = new Profile { Nick = nick };

            // Ranks per playlist.
            foreach (var (mode, min, max) in new[] { ("1v1", 500, 1100), ("2v2", 700, 1500), ("3v3", 600, 1400) })
            {
                int mmr = rnd.Next(min, max);
                p.Ranks.Add(new PlaylistRank { Mode = mode, Mmr = mmr, RankName = RankFromMmr(mmr) });
            }

            // Headline values from 2v2.
            var main = p.Ranks[1];
            p.Rank = main.RankName;
            p.MMR = main.Mmr;

            // Stats.
            p.Wins = rnd.Next(200, 4000);
            p.Matches = p.Wins + rnd.Next(150, 4000);
            p.Goals = rnd.Next(p.Wins, p.Wins * 4);
            p.Assists = rnd.Next(p.Wins / 2, p.Wins * 2);

            // Season history.
            for (int s = 18; s >= 14; s--)
            {
                int peak = rnd.Next(700, 1500);
                p.SeasonHistory.Add(new SeasonRecord
                {
                    Season = $"Season {s}",
                    PeakRank = RankFromMmr(peak),
                    PeakMmr = peak
                });
            }

            return Task.FromResult(p);
        }

        private static string RankFromMmr(int mmr)
        {
            string tier =
                mmr < 400 ? "Bronze" :
                mmr < 550 ? "Silver" :
                mmr < 700 ? "Gold" :
                mmr < 850 ? "Platinum" :
                mmr < 1000 ? "Diamond" :
                mmr < 1200 ? "Champion" :
                mmr < 1400 ? "Grand Champion" :
                "Supersonic Legend";

            if (tier == "Supersonic Legend")
                return tier;

            string div = ((mmr / 50) % 3) switch { 0 => "I", 1 => "II", _ => "III" };
            return $"{tier} {div}";
        }
    }
}
