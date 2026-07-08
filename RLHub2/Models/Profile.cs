using System.Collections.Generic;

namespace RLHub2.Models
{
    public class Profile
    {
        public string Nick { get; set; } = "";

        // Legacy summary fields (still used by ProfileServiceTracker).
        public string Rank { get; set; } = "";
        public int MMR { get; set; }
        public int Wins { get; set; }
        public int Goals { get; set; }
        public int Assists { get; set; }
        public int Saves { get; set; }
        public int Mvps { get; set; }

        // Richer data for the Profile page.
        public int Matches { get; set; }
        public List<PlaylistRank> Ranks { get; set; } = new();
        public List<SeasonRecord> SeasonHistory { get; set; } = new();
    }

    public class PlaylistRank
    {
        public string Mode { get; set; } = "";     // 1v1 / 2v2 / 3v3
        public string RankName { get; set; } = "";  // e.g. "Champion I"
        public int Mmr { get; set; }
    }

    public class SeasonRecord
    {
        public string Season { get; set; } = "";    // e.g. "Season 18"
        public string PeakRank { get; set; } = "";
        public int PeakMmr { get; set; }
    }
}
