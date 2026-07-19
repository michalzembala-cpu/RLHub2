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
    }
}
