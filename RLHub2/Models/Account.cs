using System.Collections.Generic;

namespace RLHub2.Models
{
    // One of the user's Rocket League accounts. Aliases hold every in-game name the
    // account has ever used, so replays from before a rename still match.
    public class Account
    {
        public string Name { get; set; } = "";          // display name / current in-game name
        public List<string> Aliases { get; set; } = new();

        public bool Matches(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName)) return false;
            playerName = playerName.Trim();
            if (string.Equals(Name, playerName, System.StringComparison.OrdinalIgnoreCase)) return true;
            foreach (var a in Aliases)
                if (string.Equals(a, playerName, System.StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        // Every name this account can appear under (current + old).
        public IEnumerable<string> AllNames()
        {
            if (!string.IsNullOrWhiteSpace(Name)) yield return Name;
            foreach (var a in Aliases)
                if (!string.IsNullOrWhiteSpace(a) &&
                    !string.Equals(a, Name, System.StringComparison.OrdinalIgnoreCase))
                    yield return a;
        }
    }
}
