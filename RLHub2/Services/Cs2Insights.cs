using System;
using System.Collections.Generic;
using System.Linq;
using RLHub2.Helpers;
using RLHub2.Models;

namespace RLHub2.Services
{
    public class Insight
    {
        public string Title { get; set; } = "";
        public string Text { get; set; } = "";
        public bool Good { get; set; }          // green vs orange framing
    }

    public class Cs2Report
    {
        public bool HasData { get; set; }
        public string Headline { get; set; } = "";
        public List<Insight> Insights { get; set; } = new();

        // Overall numbers for the picked mode.
        public int Matches { get; set; }
        public int WinPct { get; set; }
        public float Kd { get; set; }
        public float Adr { get; set; }
        public float HsPct { get; set; }
    }

    // Findings drawn from the matches we logged ourselves.
    //
    // Everything here comes from GSI — no ranks, no external service, no invented numbers.
    // A claim is only made when there is enough of a sample to mean anything; the point is to
    // tell the user something true they didn't know, not to fill a page.
    public static class Cs2Insights
    {
        // Below this a "winrate" is just noise.
        private const int MinMatchesForClaim = 4;
        private const int MinMatchesPerMap = 3;

        public static Cs2Report Build(string mode)
        {
            bool pl = Localization.IsPolish;
            var all = new Cs2SessionStore().Load()
                .Where(m => mode.Length == 0 || string.Equals(m.Mode, mode, StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => m.Time)
                .ToList();

            var r = new Cs2Report { Matches = all.Count };

            if (all.Count == 0)
            {
                r.Headline = pl ? "Zagraj kilka meczów, a coś tu napiszę." : "Play a few matches and this fills in.";
                return r;
            }

            r.HasData = true;
            int wins = all.Count(m => m.Won);
            r.WinPct = (int)Math.Round(100.0 * wins / all.Count);

            int k = all.Sum(m => m.Kills), d = all.Sum(m => m.Deaths);
            r.Kd = d == 0 ? k : k / (float)d;

            int rounds = all.Sum(m => m.Rounds);
            r.Adr = rounds == 0 ? 0 : all.Sum(m => m.Damage) / (float)rounds;
            r.HsPct = k == 0 ? 0 : 100f * all.Sum(m => m.HeadshotKills) / k;

            r.Headline = all.Count < MinMatchesForClaim
                ? (pl ? $"Na razie {all.Count} mecz(e). Im więcej zagrasz, tym pewniejsze wnioski."
                      : $"Only {all.Count} match(es) so far. More games, better conclusions.")
                : (pl ? $"{all.Count} meczów • {r.WinPct}% wygranych • K/D {r.Kd:0.00}"
                      : $"{all.Count} matches • {r.WinPct}% wins • K/D {r.Kd:0.00}");

            AddMapInsight(r, all, pl);
            AddAdrInsight(r, all, pl);
            AddHsInsight(r, all, pl);
            AddHourInsight(r, all, pl);
            AddFatigueInsight(r, all, pl);
            AddFormInsight(r, all, pl);

            if (r.Insights.Count == 0)
                r.Insights.Add(new Insight
                {
                    Title = pl ? "Za mało danych" : "Not enough data",
                    Text = pl
                        ? "Nic sensownego jeszcze nie widać. Wnioski pojawią się po kilku meczach."
                        : "Nothing meaningful yet. Insights appear after a few matches.",
                });

            return r;
        }

        // Best vs worst map — the single most actionable thing in CS2 (ban/pick).
        private static void AddMapInsight(Cs2Report r, List<Cs2Match> all, bool pl)
        {
            var byMap = all.GroupBy(m => m.Map)
                .Where(g => g.Count() >= MinMatchesPerMap)
                .Select(g => new { Map = g.Key, Pct = 100.0 * g.Count(x => x.Won) / g.Count(), N = g.Count() })
                .OrderByDescending(x => x.Pct)
                .ToList();

            if (byMap.Count == 0) return;

            var best = byMap[0];
            if (byMap.Count == 1)
            {
                r.Insights.Add(new Insight
                {
                    Good = best.Pct >= 50,
                    Title = Pretty(best.Map),
                    Text = pl
                        ? $"{best.Pct:0}% wygranych z {best.N} meczów."
                        : $"{best.Pct:0}% wins across {best.N} matches.",
                });
                return;
            }

            var worst = byMap[^1];
            if (best.Pct - worst.Pct < 20) return;   // not a real difference

            r.Insights.Add(new Insight
            {
                Good = true,
                Title = pl ? "Twoja mapa" : "Your map",
                Text = pl
                    ? $"Na {Pretty(best.Map)} wygrywasz {best.Pct:0}% ({best.N} meczów), a na {Pretty(worst.Map)} tylko {worst.Pct:0}% ({worst.N}). Banuj {Pretty(worst.Map)}."
                    : $"You win {best.Pct:0}% on {Pretty(best.Map)} ({best.N} matches) but only {worst.Pct:0}% on {Pretty(worst.Map)} ({worst.N}). Ban {Pretty(worst.Map)}.",
            });
        }

        // The user's own example: does damage actually predict your wins?
        private static void AddAdrInsight(Cs2Report r, List<Cs2Match> all, bool pl)
        {
            var withDmg = all.Where(m => m.Rounds > 0 && m.Damage > 0).ToList();
            if (withDmg.Count < MinMatchesForClaim) return;

            var wins = withDmg.Where(m => m.Won).ToList();
            var losses = withDmg.Where(m => !m.Won).ToList();
            if (wins.Count == 0 || losses.Count == 0) return;

            float winAdr = wins.Average(m => m.Adr);
            float lossAdr = losses.Average(m => m.Adr);
            if (winAdr - lossAdr < 10) return;

            int cut = (int)Math.Round((winAdr + lossAdr) / 2);
            var above = withDmg.Where(m => m.Adr >= cut).ToList();
            if (above.Count < 2) return;
            int abovePct = (int)Math.Round(100.0 * above.Count(m => m.Won) / above.Count);

            r.Insights.Add(new Insight
            {
                Good = true,
                Title = "ADR",
                Text = pl
                    ? $"Gdy Twój ADR przekracza {cut}, wygrywasz {abovePct}% meczów. W wygranych masz średnio {winAdr:0} ADR, w przegranych {lossAdr:0}."
                    : $"With ADR above {cut} you win {abovePct}% of matches. Your wins average {winAdr:0} ADR, losses {lossAdr:0}.",
            });
        }

        private static void AddHsInsight(Cs2Report r, List<Cs2Match> all, bool pl)
        {
            int k = all.Sum(m => m.Kills);
            int hs = all.Sum(m => m.HeadshotKills);
            if (k < 30) return;   // percentage of a tiny number is meaningless

            float pct = 100f * hs / k;

            // Rough community benchmark; stated as a comparison, not a verdict.
            bool good = pct >= 45;
            r.Insights.Add(new Insight
            {
                Good = good,
                Title = pl ? "Celność (HS%)" : "Headshots (HS%)",
                Text = good
                    ? (pl ? $"{pct:0}% Twoich zabójstw to headshoty ({hs}/{k}). Solidnie — celujesz na wysokości głowy."
                          : $"{pct:0}% of your kills are headshots ({hs}/{k}). Solid — your crosshair sits at head level.")
                    : (pl ? $"{pct:0}% Twoich zabójstw to headshoty ({hs}/{k}). Popracuj nad wysokością celownika — typowy gracz ma około 45%."
                          : $"{pct:0}% of your kills are headshots ({hs}/{k}). Work on crosshair placement — a typical player sits near 45%."),
            });
        }

        private static void AddHourInsight(Cs2Report r, List<Cs2Match> all, bool pl)
        {
            if (all.Count < 8) return;

            var byHour = all.GroupBy(m => m.Time.Hour)
                .Where(g => g.Count() >= 3)
                .Select(g => new { H = g.Key, Pct = 100.0 * g.Count(x => x.Won) / g.Count(), N = g.Count() })
                .OrderByDescending(x => x.Pct)
                .ToList();
            if (byHour.Count < 2) return;

            var best = byHour[0];
            var worst = byHour[^1];
            if (best.Pct - worst.Pct < 25) return;

            r.Insights.Add(new Insight
            {
                Good = true,
                Title = pl ? "Pora gry" : "Time of day",
                Text = pl
                    ? $"Najlepiej grasz około {best.H}:00 — {best.Pct:0}% wygranych. Najgorzej około {worst.H}:00 ({worst.Pct:0}%)."
                    : $"You play best around {best.H}:00 — {best.Pct:0}% wins. Worst around {worst.H}:00 ({worst.Pct:0}%).",
            });
        }

        // Long sessions: do the later matches get worse?
        private static void AddFatigueInsight(Cs2Report r, List<Cs2Match> all, bool pl)
        {
            var sessions = Sessions(all).Where(s => s.Count >= 5).ToList();
            if (sessions.Count == 0) return;

            var early = new List<Cs2Match>();
            var late = new List<Cs2Match>();
            foreach (var s in sessions)
            {
                early.AddRange(s.Take(3));
                late.AddRange(s.Skip(3));
            }
            if (late.Count < 3) return;

            double e = 100.0 * early.Count(m => m.Won) / early.Count;
            double l = 100.0 * late.Count(m => m.Won) / late.Count;
            if (e - l < 20) return;

            r.Insights.Add(new Insight
            {
                Good = false,
                Title = pl ? "Długie sesje" : "Long sessions",
                Text = pl
                    ? $"Pierwsze 3 mecze sesji: {e:0}% wygranych. Kolejne: {l:0}%. Kończ sesję wcześniej albo rób przerwy."
                    : $"First 3 matches of a session: {e:0}% wins. After that: {l:0}%. Stop earlier or take breaks.",
            });
        }

        // Recent form vs everything before it.
        private static void AddFormInsight(Cs2Report r, List<Cs2Match> all, bool pl)
        {
            if (all.Count < 10) return;

            var recent = all.TakeLast(5).ToList();
            var before = all.Take(all.Count - 5).ToList();

            double rp = 100.0 * recent.Count(m => m.Won) / recent.Count;
            double bp = 100.0 * before.Count(m => m.Won) / before.Count;
            double diff = rp - bp;
            if (Math.Abs(diff) < 20) return;

            r.Insights.Add(new Insight
            {
                Good = diff > 0,
                Title = pl ? "Forma" : "Form",
                Text = diff > 0
                    ? (pl ? $"Ostatnie 5 meczów: {rp:0}% wygranych, wcześniej {bp:0}%. Jesteś w formie."
                          : $"Last 5 matches: {rp:0}% wins, before that {bp:0}%. You're on form.")
                    : (pl ? $"Ostatnie 5 meczów: {rp:0}% wygranych, wcześniej {bp:0}%. Spadek formy."
                          : $"Last 5 matches: {rp:0}% wins, before that {bp:0}%. You're slumping."),
            });
        }

        // A gap of over two hours starts a new sitting.
        private static List<List<Cs2Match>> Sessions(List<Cs2Match> all)
        {
            var result = new List<List<Cs2Match>>();
            var cur = new List<Cs2Match>();
            foreach (var m in all)
            {
                if (cur.Count > 0 && (m.Time - cur[^1].Time).TotalHours > 2)
                {
                    result.Add(cur);
                    cur = new List<Cs2Match>();
                }
                cur.Add(m);
            }
            if (cur.Count > 0) result.Add(cur);
            return result;
        }

        private static string Pretty(string map)
        {
            if (string.IsNullOrEmpty(map)) return "";
            var s = map.StartsWith("de_") || map.StartsWith("cs_") ? map.Substring(3) : map;
            return s.Length == 0 ? map : char.ToUpperInvariant(s[0]) + s.Substring(1);
        }
    }
}
