namespace RLHub2.Helpers
{
    // Approximates a numeric MMR from a ballchasing rank (tier 0-22 + division 0-3).
    // Ballchasing does not expose raw MMR, so this is a rough 2v2-style mapping — good
    // enough to auto-fill the MMR chart with real match-by-match rank movement.
    public static class RankMmr
    {
        // Baseline MMR per tier index (0 = unranked, 1 = Bronze I … 22 = Supersonic Legend).
        private static readonly int[] TierBase =
        {
            0,                       // 0  unranked
            150, 175, 200,           // 1-3   Bronze I-III
            260, 300, 340,           // 4-6   Silver I-III
            420, 460, 500,           // 7-9   Gold I-III
            560, 610, 660,           // 10-12 Platinum I-III
            720, 770, 820,           // 13-15 Diamond I-III
            880, 940, 1000,          // 16-18 Champion I-III
            1075, 1175, 1275,        // 19-21 Grand Champion I-III
            1400                     // 22    Supersonic Legend
        };

        public static int Approx(int tier, int division)
        {
            if (tier <= 0) return 0;
            if (tier > 22) tier = 22;
            // ballchasing divisions are 1-4 ("Division 1"…"Division 4"); make them a 0-3 offset
            int off = division >= 1 ? division - 1 : 0;
            if (off > 3) off = 3;
            return TierBase[tier] + off * 15;
        }
    }
}
