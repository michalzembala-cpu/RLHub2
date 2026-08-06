using System;

namespace RLHub2.Models
{
    // One reading of the three role ranks at a point in time. Built from a screen OCR the user
    // confirms, so it is reliable even when the OCR guess is imperfect. Feeds a future rank-history
    // chart, and works when Blizzard's public career page (OverFast) is unavailable.
    public class OwRankSnapshot
    {
        public DateTime Time { get; set; }
        public string Tank { get; set; } = "";     // e.g. "Diamond 3", or "" if unset
        public string Damage { get; set; } = "";
        public string Support { get; set; } = "";

        public bool Any => Tank.Length > 0 || Damage.Length > 0 || Support.Length > 0;
    }
}
