using System;

namespace RLHub2.Models
{
    // A manually-logged Overwatch 2 match. OW2 exposes no live match feed, so the user records
    // the outcome themselves — this is the honest, always-available data source for OW.
    public class OwMatch
    {
        public DateTime Time { get; set; }
        public string Result { get; set; } = "";   // "W" | "L" | "D"
        public string Role { get; set; } = "";      // "" | "tank" | "damage" | "support"

        // Optional per-match stats read from the end-of-match scoreboard (user-confirmed). 0 = not
        // recorded — quick-logged matches have none.
        public int Eliminations { get; set; }
        public int Assists { get; set; }
        public int Deaths { get; set; }
        public int Damage { get; set; }
        public int Healing { get; set; }

        public bool Won => Result == "W";
        public bool Lost => Result == "L";
        public bool Draw => Result == "D";

        public bool HasStats => Eliminations > 0 || Assists > 0 || Deaths > 0 || Damage > 0 || Healing > 0;
    }
}
