using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace RLHub2.Helpers
{
    // Loads rank icon images from Resources\ (copied to output). Cached.
    public static class RankIcons
    {
        private static readonly Dictionary<string, string> Map = new()
        {
            ["Bronze"] = "bronze.png",
            ["Silver"] = "silver.png",
            ["Gold"] = "gold.png",
            ["Platinum"] = "platinum.png",
            ["Diamond"] = "diamond.png",
            ["Champion"] = "champion.png",
            ["Grand Champion"] = "grandchampion.png",
            ["Supersonic Legend"] = "ssl.png",
        };

        private static readonly Dictionary<string, Image?> Cache = new();

        // Match a full rank-name string (e.g. "Champion I · Division II") to a tier icon.
        public static Image? GetForRankName(string rankName)
        {
            if (string.IsNullOrWhiteSpace(rankName)) return null;
            string s = rankName.ToLowerInvariant();
            if (s.Contains("supersonic") || s.Contains("ssl")) return Get("Supersonic Legend");
            if (s.Contains("grand")) return Get("Grand Champion");
            if (s.Contains("champion")) return Get("Champion");
            if (s.Contains("diamond")) return Get("Diamond");
            if (s.Contains("platinum")) return Get("Platinum");
            if (s.Contains("gold")) return Get("Gold");
            if (s.Contains("silver")) return Get("Silver");
            if (s.Contains("bronze")) return Get("Bronze");
            return null;
        }

        public static Image? Get(string tier)
        {
            if (Cache.TryGetValue(tier, out var cached)) return cached;

            Image? result = null;
            if (Map.TryGetValue(tier, out var file))
            {
                try
                {
                    var path = Path.Combine(AppContext.BaseDirectory, "Resources", file);
                    if (File.Exists(path))
                        result = Image.FromFile(path);
                }
                catch { }
            }
            Cache[tier] = result;
            return result;
        }
    }
}
