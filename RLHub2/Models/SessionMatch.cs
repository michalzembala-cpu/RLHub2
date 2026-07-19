using System;

namespace RLHub2.Models
{
    // One completed match detected live from the Rocket League Stats API.
    public class SessionMatch
    {
        public DateTime Time { get; set; }
        public string Account { get; set; } = "";   // which account was playing

        // Playlist worked out from how many players were on each side ("1v1", "2v2", "3v3").
        // The Stats API never reports the playlist itself, but it does list every player and
        // their team, so the size gives it away. Empty when the size wasn't one of those.
        public string Mode { get; set; } = "";
        public bool Won { get; set; }
        public int Goals { get; set; }
        public int Saves { get; set; }
        public int Assists { get; set; }
        public int Shots { get; set; }
        public int Score { get; set; }
        public int TeamGoals { get; set; }
        public int OppGoals { get; set; }
    }
}
