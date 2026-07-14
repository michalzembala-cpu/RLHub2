using System;

namespace RLHub2.Models
{
    // One match parsed from a ballchasing.com replay, from the tracked player's view.
    public class BallMatch
    {
        public string Id { get; set; } = "";
        public string Account { get; set; } = "";   // which of the user's accounts this belongs to
        public DateTime Date { get; set; }

        public string PlaylistId { get; set; } = "";  // e.g. "ranked-doubles"
        public string Mode { get; set; } = "";        // "1v1" / "2v2" / "3v3" / ""
        public bool Ranked { get; set; }

        public bool Won { get; set; }
        public int TeamGoals { get; set; }
        public int OppGoals { get; set; }

        public int Goals { get; set; }
        public int Saves { get; set; }
        public int Assists { get; set; }
        public int Shots { get; set; }
        public int Score { get; set; }
        public bool Mvp { get; set; }

        public string RankName { get; set; } = "";    // e.g. "Grand Champion II"
        public int RankTier { get; set; }             // 0-22
        public int RankDivision { get; set; }         // 0-3
        public int MmrApprox { get; set; }            // approximated from rank

        // ===== advanced stats (from ballchasing boost/positioning) =====
        public bool HasAdvanced { get; set; }
        public double ShootingPct { get; set; }
        public double BoostBpm { get; set; }          // boost used per minute
        public double BoostAvg { get; set; }          // average amount 0-100
        public int BoostBigPads { get; set; }
        public int BoostSmallPads { get; set; }
        public double BoostStolen { get; set; }
        public double BoostTimeZero { get; set; }      // seconds at 0 boost
        public double PosBehindBallPct { get; set; }
        public double PosDefThirdPct { get; set; }
        public double PosOffThirdPct { get; set; }
        public double PosMostBackPct { get; set; }
        public double AvgDistToBall { get; set; }
        public int DemosInflicted { get; set; }
        public int DemosTaken { get; set; }
    }
}
