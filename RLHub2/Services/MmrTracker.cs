using System;
using System.Linq;
using RLHub2.Models;

namespace RLHub2.Services
{
    // Derives MMR from live match results: every win adds a step, every loss takes one away.
    //
    // Nothing reports real MMR any more — the Stats API never had it, anti-cheat blocks
    // BakkesMod online, and ballchasing stopped returning ranks. But wins and losses DO arrive
    // live, so the curve can be rebuilt from them. It is an approximation, and the step is a
    // rough average rather than the true per-match delta, which depends on the opponents.
    //
    // Each playlist keeps its own curve — the mode comes from how many players were on each
    // side, since the feed never names the playlist directly.
    public static class MmrTracker
    {
        public const int Step = 9;

        private static readonly MmrStore Store = new();

        // Records a finished match and returns the new value. Returns null when the mode is
        // unknown, or when that mode has no starting point yet: counting up from zero would
        // draw a nonsense curve instead of continuing from where the player actually is.
        public static int? Record(string account, bool won, DateTime when, string mode)
        {
            if (string.IsNullOrEmpty(mode)) return null;

            var all = Store.Load();

            var last = all
                .Where(e => e.Mode == mode)
                .Where(e => string.IsNullOrEmpty(account)
                            || string.IsNullOrEmpty(e.Account)
                            || e.Account == account)
                .OrderBy(e => e.Timestamp)
                .LastOrDefault();

            if (last == null) return null;

            int value = Math.Max(0, last.Value + (won ? Step : -Step));

            all.Add(new MmrEntry(when, value, mode) { Account = account });
            all.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            Store.Save(all);

            return value;
        }

        // Moves a match's MMR point from one playlist to another, for when the mode was wrong —
        // Rumble recorded as plain 3v3, say. Without this, correcting a match would leave the
        // points behind and both curves would stay wrong.
        public static void Retag(string account, DateTime when, string oldMode, string newMode)
        {
            if (oldMode == newMode) return;

            var all = Store.Load();

            // The entry was written with the match's timestamp; allow a second of slack rather
            // than requiring an exact tick match.
            var entry = all.FirstOrDefault(e =>
                e.Mode == oldMode &&
                Math.Abs((e.Timestamp - when).TotalSeconds) < 2);

            if (entry != null) all.Remove(entry);

            if (newMode.Length > 0)
            {
                var last = all
                    .Where(e => e.Mode == newMode)
                    .Where(e => string.IsNullOrEmpty(account)
                                || string.IsNullOrEmpty(e.Account)
                                || e.Account == account)
                    .OrderBy(e => e.Timestamp)
                    .LastOrDefault();

                // Only re-record when the target playlist has a starting point of its own.
                if (last != null)
                {
                    bool won = entry != null && entry.Value > 0 && IsWin(all, entry, oldMode, account);
                    int value = Math.Max(0, last.Value + (won ? Step : -Step));
                    all.Add(new MmrEntry(when, value, newMode) { Account = account });
                }
            }

            all.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            Store.Save(all);
        }

        // Was the removed point a win? Its value rose above the one before it in that playlist.
        private static bool IsWin(System.Collections.Generic.List<MmrEntry> remaining,
                                  MmrEntry entry, string mode, string account)
        {
            var prev = remaining
                .Where(e => e.Mode == mode && e.Timestamp < entry.Timestamp)
                .Where(e => string.IsNullOrEmpty(account)
                            || string.IsNullOrEmpty(e.Account)
                            || e.Account == account)
                .OrderBy(e => e.Timestamp)
                .LastOrDefault();
            return prev == null || entry.Value > prev.Value;
        }
    }
}
