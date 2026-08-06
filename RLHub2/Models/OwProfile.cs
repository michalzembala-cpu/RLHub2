using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace RLHub2.Models
{
    // Overwatch 2 career data, as read from a public Blizzard profile via OverFast. Everything
    // here is what the API actually exposes — there is deliberately no match history, per-map or
    // per-match data, because Blizzard does not publish it (see the OverFast notes).

    // A competitive rank for one role: division is the tier name ("silver"), tier the 1-5 sub-rank.
    public class OwRank
    {
        public string Role = "";      // "tank" | "damage" | "support"
        public string Division = "";  // "bronze".."grandmaster" | "champion"
        public int Tier;              // 1..5 (5 is the bottom of the tier)
        public string RankIcon = "";

        public string Display =>
            string.IsNullOrEmpty(Division) ? "—" : Cap(Division) + " " + Tier;

        private static string Cap(string s) =>
            string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);
    }

    // Aggregated stats for the whole career ("general") or one role. The combat figures come in
    // two flavours from the API: career totals, and per-10-minutes averages.
    public class OwStatBlock
    {
        public string Key = "";       // "general" | "tank" | "damage" | "support"
        public int GamesPlayed;
        public int GamesWon;
        public int GamesLost;
        public long TimePlayedSeconds;
        public double Winrate;        // 0-100
        public double Kda;

        // per-10-minutes averages
        public double AvgEliminations;
        public double AvgAssists;
        public double AvgDeaths;
        public double AvgDamage;
        public double AvgHealing;

        // career totals
        public long TotalEliminations;
        public long TotalAssists;
        public long TotalDeaths;
        public long TotalDamage;
        public long TotalHealing;

        public string TimePlayedText => FormatDuration(TimePlayedSeconds);

        public static string FormatDuration(long seconds)
        {
            if (seconds <= 0) return "0h";
            var t = TimeSpan.FromSeconds(seconds);
            int h = (int)t.TotalHours;
            return h >= 1 ? $"{h}h {t.Minutes}m" : $"{t.Minutes}m";
        }
    }

    public class OwHero
    {
        public string Key = "";
        public string Name = "";
        public int GamesPlayed;
        public double Winrate;
        public double Kda;
        public long TimePlayedSeconds;

        public string TimePlayedText => OwStatBlock.FormatDuration(TimePlayedSeconds);
    }

    public class OwProfile
    {
        public bool Found;
        public bool Private;        // profile exists but career is hidden
        public string? Error;       // "no-tag" | "not-found" | "rate-limited" | other message

        public string Username = "";
        public string AvatarUrl = "";
        public string Title = "";
        public int Endorsement;

        public List<OwRank> Ranks = new();     // 0-3 roles with a rank
        public OwStatBlock? General;
        public List<OwStatBlock> Roles = new();
        public List<OwHero> Heroes = new();

        public OwRank? Rank(string role) =>
            Ranks.FirstOrDefault(r => string.Equals(r.Role, role, StringComparison.OrdinalIgnoreCase));

        public OwStatBlock? Role(string role) =>
            Roles.FirstOrDefault(r => string.Equals(r.Key, role, StringComparison.OrdinalIgnoreCase));

        // Most-played hero by time — the API has no "main", so this is the honest stand-in.
        public OwHero? MostPlayed =>
            Heroes.OrderByDescending(h => h.TimePlayedSeconds).FirstOrDefault();

        // The highest of the three role ranks, for a single "main rank" headline.
        public OwRank? TopRank =>
            Ranks.OrderByDescending(RankOrder).FirstOrDefault();

        private static int RankOrder(OwRank r)
        {
            string[] tiers = { "bronze", "silver", "gold", "platinum", "diamond", "master", "grandmaster", "champion" };
            int t = Array.IndexOf(tiers, r.Division?.ToLowerInvariant());
            // Within a tier, division 1 is highest, 5 lowest — invert so 1 ranks above 5.
            return t * 10 + (6 - Math.Clamp(r.Tier, 1, 5));
        }
    }
}
