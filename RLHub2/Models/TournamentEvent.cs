using System;

namespace RLHub2.Models
{
    public class TournamentEvent
    {
        public string Name { get; set; } = "";
        public string Source { get; set; } = "";    // publisher
        public string Category { get; set; } = "";   // RLCS / MAJOR / REGIONAL
        public string Link { get; set; } = "";
        public DateTime Date { get; set; }
    }
}
