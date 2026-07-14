using System;

namespace RLHub2.Models
{
    // One completed match detected live from the Rocket League Stats API.
    public class SessionMatch
    {
        public DateTime Time { get; set; }
        public string Account { get; set; } = "";   // which account was playing
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
