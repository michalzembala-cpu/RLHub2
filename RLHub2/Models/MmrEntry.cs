using System;

namespace RLHub2.Models
{
    public class MmrEntry
    {
        public DateTime Timestamp { get; set; }
        public int Value { get; set; }

        // Playlist: "1v1", "2v2" or "3v3".
        public string Mode { get; set; } = "2v2";

        public MmrEntry() { }

        public MmrEntry(DateTime timestamp, int value)
        {
            Timestamp = timestamp;
            Value = value;
        }

        public MmrEntry(DateTime timestamp, int value, string mode)
        {
            Timestamp = timestamp;
            Value = value;
            Mode = mode;
        }

        public override string ToString() => $"{Timestamp:O}|{Value}";

        public static bool TryParse(string line, out MmrEntry? entry)
        {
            entry = null;
            if (string.IsNullOrWhiteSpace(line)) return false;
            var parts = line.Split('|');
            if (parts.Length == 2)
            {
                if (DateTime.TryParse(parts[0], null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
                    && int.TryParse(parts[1], out var v))
                {
                    entry = new MmrEntry(dt, v);
                    return true;
                }
            }
            // backward compatibility: single int
            if (int.TryParse(line.Trim(), out var vv))
            {
                entry = new MmrEntry(DateTime.UtcNow, vv);
                return true;
            }
            return false;
        }
    }
}
