using System;

namespace RLHub2.Models
{
    // A frozen summary of one season, saved when that season ends.
    public class SeasonSnapshot
    {
        public string Season { get; set; } = "";
        public string Account { get; set; } = "";
        public string PeakRank { get; set; } = "";
        public string FinalRank { get; set; } = "";
        public int HighestMmr { get; set; }
        public int WinRate { get; set; }   // percent
        public int Matches { get; set; }
        public DateTime EndedOn { get; set; }
        public bool InProgress { get; set; }
    }
}
