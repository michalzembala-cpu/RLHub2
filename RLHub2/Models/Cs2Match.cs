using System;

namespace RLHub2.Models
{
    // One finished Counter-Strike 2 match, seen live through Game State Integration.
    //
    // GSI reports no rank and no CS Rating — same shape of gap as Rocket League's Stats API —
    // so this is session data only: what happened while the app was open.
    public class Cs2Match
    {
        public DateTime Time { get; set; }
        public string SteamId { get; set; } = "";   // which Steam account played it

        public string Map { get; set; } = "";       // de_dust2, de_mirage, ...
        public string Mode { get; set; } = "";      // premier, competitive, casual, ...

        public bool Won { get; set; }
        public bool Draw { get; set; }

        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public int Mvps { get; set; }
        public int Score { get; set; }

        public int RoundsWon { get; set; }
        public int RoundsLost { get; set; }

        public float Kd => Deaths == 0 ? Kills : Kills / (float)Deaths;
    }
}
