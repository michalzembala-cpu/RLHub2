using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using RLHub2.Helpers;
using RLHub2.Models;

namespace RLHub2.Services
{
    public class CoachReport
    {
        public int Overall;
        public string Grade = "—";
        public List<(string Name, int Score, string Val)> Categories = new();
        public string Text = "";
        public bool HasData;
    }

    // Heuristic (rule-based) coaching from ballchasing / MMR / session data.
    public static class CoachAnalysis
    {
        private const int Window = 30; // matches analysed

        public static CoachReport Build()
        {
            bool pl = Localization.IsPolish;
            var all = new BallMatchStore().Load().OrderBy(m => m.Date).ToList();
            var mmr = new MmrStore().Load();
            var session = new SessionStore().Load();

            var rep = new CoachReport();
            if (all.Count == 0)
            {
                rep.Text = pl
                    ? "Brak meczów do analizy — zagraj kilka gier (a Ballchasing je wgra), i wróć tutaj."
                    : "No matches to analyze yet — play some games (Ballchasing will upload them) and come back.";
                return rep;
            }

            rep.HasData = true;
            var games = all.TakeLast(Window).ToList();
            int n = games.Count;
            var adv = games.Where(m => m.HasAdvanced).ToList();

            // ===== per-game averages =====
            double gpg = games.Average(m => m.Goals);
            double svg = games.Average(m => m.Saves);
            double apg = games.Average(m => m.Assists);
            double shpg = games.Average(m => m.Shots);
            double shootPct = games.Where(m => m.Shots > 0).Select(m => m.ShootingPct).DefaultIfEmpty(0).Average();
            if (shootPct <= 0)
            {
                int tShots = games.Sum(m => m.Shots), tGoals = games.Sum(m => m.Goals);
                shootPct = tShots > 0 ? 100.0 * tGoals / tShots : 0;
            }

            // ===== ratings 0-100 =====
            int atk = Blend(Scale(gpg, 0.2, 1.3), Scale(shpg, 1.0, 4.0), 0.7);
            int def = Scale(svg, 0.3, 2.2);
            int shot = Scale(shootPct, 8, 45);

            int boost = -1, pos = -1;
            if (adv.Count > 0)
            {
                double bpm = adv.Average(m => m.BoostBpm);
                double small = adv.Sum(m => m.BoostSmallPads);
                double big = adv.Sum(m => m.BoostBigPads);
                double smallRatio = (small + big) > 0 ? small / (small + big) : 0;
                double zero = adv.Average(m => m.BoostTimeZero);
                boost = (int)Math.Round((Scale(bpm, 250, 480) + Scale(smallRatio, 0.35, 0.7) + ScaleInv(zero, 45, 8)) / 3.0);

                double behind = adv.Average(m => m.PosBehindBallPct);
                pos = Scale(behind, 55, 82);
            }

            var cats = new List<(string Name, int Score, string Val)>
            {
                (pl ? "ATAK" : "ATTACK", atk, $"{gpg:0.0} {(pl ? "gole/mecz" : "goals/g")}"),
                (pl ? "OBRONA" : "DEFENSE", def, $"{svg:0.0} {(pl ? "obr./mecz" : "saves/g")}"),
                (pl ? "STRZAŁY" : "SHOOTING", shot, $"{shootPct:0}%"),
            };
            if (boost >= 0) cats.Add((pl ? "BOOST" : "BOOST", boost, adv.Count + (pl ? " meczów" : " games")));
            if (pos >= 0) cats.Add((pl ? "POZYCJA" : "POSITION", pos, $"{adv.Average(m => m.PosBehindBallPct):0}% {(pl ? "za piłką" : "behind ball")}"));

            rep.Categories = cats;
            rep.Overall = (int)Math.Round(cats.Average(c => c.Score));
            rep.Grade = GradeOf(rep.Overall);

            // ===== report text =====
            var sb = new StringBuilder();
            int wins = games.Count(m => m.Won);

            // key tip = weakest category
            var weak = cats.OrderBy(c => c.Score).First();
            sb.AppendLine(pl ? "💡 NAJWAŻNIEJSZA RADA" : "💡 KEY TIP");
            sb.AppendLine(KeyTip(weak.Name, pl));
            sb.AppendLine();

            // progress: last half vs previous half
            sb.AppendLine(pl ? "📈 PROGRES (ostatnie mecze vs wcześniej)" : "📈 PROGRESS (recent vs earlier)");
            AppendProgress(sb, games, pl);
            sb.AppendLine();

            // insights
            sb.AppendLine(pl ? "🧠 INSIGHTS" : "🧠 INSIGHTS");
            foreach (var line in Insights(all, session, pl)) sb.AppendLine("• " + line);
            sb.AppendLine();

            // weekly report
            sb.AppendLine(pl ? "📅 RAPORT TYGODNIA" : "📅 WEEKLY REPORT");
            AppendWeekly(sb, all, mmr, pl);
            sb.AppendLine();

            // road to next rank
            sb.AppendLine(pl ? "🏆 DROGA DO NASTĘPNEJ RANGI" : "🏆 ROAD TO NEXT RANK");
            AppendRoad(sb, games, gpg, svg, shootPct, wins, n, pl);
            sb.AppendLine();

            // training of the day
            sb.AppendLine(pl ? "🎯 TRENING DNIA" : "🎯 TRAINING OF THE DAY");
            foreach (var d in Training(weak.Name, pl)) sb.AppendLine("✔ " + d);

            rep.Text = sb.ToString().TrimEnd();
            return rep;
        }

        // ===== helpers =====

        private static int Scale(double v, double lo, double hi)
        {
            if (hi <= lo) return 0;
            double t = (v - lo) / (hi - lo);
            return (int)Math.Round(Math.Clamp(t, 0, 1) * 100);
        }
        private static int ScaleInv(double v, double lo, double hi) => 100 - Scale(v, hi, lo);
        private static int Blend(int a, int b, double wa) => (int)Math.Round(a * wa + b * (1 - wa));

        private static string GradeOf(int s) => s switch
        {
            >= 92 => "S", >= 85 => "A+", >= 78 => "A", >= 70 => "B+",
            >= 62 => "B", >= 54 => "C+", >= 46 => "C", >= 38 => "D", _ => "E"
        };

        private static string KeyTip(string weakCat, bool pl)
        {
            string c = weakCat.ToUpperInvariant();
            if (c.Contains("OBRON") || c.Contains("DEF"))
                return pl ? "Za mało obron — częściej wracaj do defensywy i graj shadow defense zamiast podwójnie commitować."
                         : "Too few saves — get back on defense more and play shadow defense instead of double-committing.";
            if (c.Contains("BOOST"))
                return pl ? "Słabe zarządzanie boostem — zbieraj małe pady po drodze zamiast gonić za pełnym boostem, i unikaj jazdy na zerze."
                         : "Weak boost management — grab small pads along your path instead of chasing full boost, and avoid running on empty.";
            if (c.Contains("POZYC") || c.Contains("POSITION"))
                return pl ? "Pozycjonowanie — spędzasz za mało czasu za piłką. Trzymaj się z tyłu i rotuj, zamiast napierać z partnerem."
                         : "Positioning — you spend too little time behind the ball. Hold back and rotate instead of pushing with your teammate.";
            if (c.Contains("STRZA") || c.Contains("SHOOT"))
                return pl ? "Skuteczność strzałów — bierz pewniejsze strzały (bliżej, z lepszego kąta) zamiast strzelać na oślep."
                         : "Shooting accuracy — take higher-quality shots (closer, better angle) instead of forcing them.";
            return pl ? "Atak — szukaj więcej okazji: kreuj gry z partnerem i częściej celuj w bramkę."
                      : "Attack — create more chances: play off your teammate and put more shots on target.";
        }

        private static void AppendProgress(StringBuilder sb, List<BallMatch> games, bool pl)
        {
            int half = games.Count / 2;
            if (half < 2) { sb.AppendLine(pl ? "• Za mało meczów na porównanie." : "• Not enough matches to compare."); return; }
            var older = games.Take(half).ToList();
            var recent = games.Skip(games.Count - half).ToList();

            double wOld = 100.0 * older.Count(m => m.Won) / older.Count;
            double wNew = 100.0 * recent.Count(m => m.Won) / recent.Count;
            AppendDelta(sb, pl ? "Winrate" : "Win rate", wOld, wNew, "%", pl);
            AppendDelta(sb, pl ? "Gole/mecz" : "Goals/g", older.Average(m => m.Goals), recent.Average(m => m.Goals), "", pl);
            AppendDelta(sb, pl ? "Obrony/mecz" : "Saves/g", older.Average(m => m.Saves), recent.Average(m => m.Saves), "", pl);
        }

        private static void AppendDelta(StringBuilder sb, string label, double a, double b, string unit, bool pl)
        {
            double d = b - a;
            string arrow = d > 0.05 ? "▲" : d < -0.05 ? "▼" : "▬";
            string dv = (d >= 0 ? "+" : "") + (unit == "%" ? d.ToString("0") : d.ToString("0.0"));
            sb.AppendLine($"• {label}: {a.ToString(unit == "%" ? "0" : "0.0")}{unit} → {b.ToString(unit == "%" ? "0" : "0.0")}{unit}  {arrow} {dv}{unit}");
        }

        private static IEnumerable<string> Insights(List<BallMatch> all, List<SessionMatch> session, bool pl)
        {
            var list = new List<string>();

            // mode comparison
            var byMode = all.Where(m => m.Mode.Length > 0).GroupBy(m => m.Mode)
                .Select(g => new { Mode = g.Key, Wr = 100.0 * g.Count(x => x.Won) / g.Count(), N = g.Count() })
                .Where(x => x.N >= 4).OrderByDescending(x => x.Wr).ToList();
            if (byMode.Count >= 2)
                list.Add(pl
                    ? $"Lepiej radzisz sobie w {byMode[0].Mode} ({byMode[0].Wr:0}% WR) niż w {byMode[^1].Mode} ({byMode[^1].Wr:0}%)."
                    : $"You do better in {byMode[0].Mode} ({byMode[0].Wr:0}% WR) than {byMode[^1].Mode} ({byMode[^1].Wr:0}%).");

            // time of day
            var byHour = all.GroupBy(m => m.Date.ToLocalTime().Hour)
                .Select(g => new { H = g.Key, Wr = 100.0 * g.Count(x => x.Won) / g.Count(), N = g.Count() })
                .Where(x => x.N >= 3).ToList();
            if (byHour.Count >= 2)
            {
                var best = byHour.OrderByDescending(x => x.Wr).First();
                var worst = byHour.OrderBy(x => x.Wr).First();
                list.Add(pl
                    ? $"Najlepiej grasz ok. {best.H}:00 ({best.Wr:0}% WR), najgorzej ok. {worst.H}:00 ({worst.Wr:0}%)."
                    : $"You play best around {best.H}:00 ({best.Wr:0}% WR), worst around {worst.H}:00 ({worst.Wr:0}%).");
            }

            // fatigue: sessions grouped by >2h gap
            var groups = new List<List<BallMatch>>();
            foreach (var m in all.OrderBy(x => x.Date))
            {
                if (groups.Count == 0 || (m.Date - groups[^1][^1].Date).TotalHours > 2) groups.Add(new List<BallMatch>());
                groups[^1].Add(m);
            }
            var early = groups.SelectMany(g => g.Take(3)).ToList();
            var late = groups.SelectMany(g => g.Skip(3)).ToList();
            if (early.Count >= 5 && late.Count >= 5)
            {
                double we = 100.0 * early.Count(m => m.Won) / early.Count;
                double wl = 100.0 * late.Count(m => m.Won) / late.Count;
                if (we - wl >= 8)
                    list.Add(pl
                        ? $"Forma spada w długich sesjach: {we:0}% WR w pierwszych 3 meczach vs {wl:0}% później. Rozważ przerwy."
                        : $"Your form drops in long sessions: {we:0}% WR in the first 3 games vs {wl:0}% after. Consider breaks.");
            }

            if (list.Count == 0)
                list.Add(pl ? "Za mało danych na wnioski — zagraj więcej meczów." : "Not enough data for insights yet — play more matches.");
            return list;
        }

        private static void AppendWeekly(StringBuilder sb, List<BallMatch> all, List<MmrEntry> mmr, bool pl)
        {
            var now = DateTime.Now;
            int diff = (7 + (int)now.DayOfWeek - (int)DayOfWeek.Monday) % 7;
            var weekStart = now.Date.AddDays(-diff);

            var wk = all.Where(m => m.Date >= weekStart).ToList();
            if (wk.Count == 0) { sb.AppendLine(pl ? "• Brak meczów w tym tygodniu." : "• No matches this week."); return; }

            int wins = wk.Count(m => m.Won);
            int wr = (int)Math.Round(100.0 * wins / wk.Count);
            var mmrWk = mmr.Where(e => e.Timestamp >= weekStart).OrderBy(e => e.Timestamp).ToList();
            int mmrChange = mmrWk.Count >= 2 ? mmrWk[^1].Value - mmrWk[0].Value : 0;

            sb.AppendLine($"• {(pl ? "Mecze" : "Matches")}: {wk.Count}   ·   Winrate: {wr}%   ·   MMR: {(mmrChange >= 0 ? "+" : "")}{mmrChange}");

            var byDay = wk.GroupBy(m => m.Date.DayOfWeek)
                .Select(g => new { D = g.Key, Wr = 100.0 * g.Count(x => x.Won) / g.Count(), N = g.Count() })
                .Where(x => x.N >= 2).ToList();
            if (byDay.Count >= 1)
            {
                var best = byDay.OrderByDescending(x => x.Wr).First();
                var worst = byDay.OrderBy(x => x.Wr).First();
                sb.AppendLine($"• {(pl ? "Najlepszy dzień" : "Best day")}: {DayName(best.D, pl)} ({best.Wr:0}%)   ·   {(pl ? "najgorszy" : "worst")}: {DayName(worst.D, pl)} ({worst.Wr:0}%)");
            }
        }

        private static void AppendRoad(StringBuilder sb, List<BallMatch> games, double gpg, double svg, double shootPct, int wins, int n, bool pl)
        {
            var ranked = games.Where(m => m.RankTier > 0).OrderByDescending(m => m.Date).ToList();
            string rank = ranked.Count > 0 ? ranked[0].RankName : (pl ? "brak danych" : "no data");
            double wr = n > 0 ? 100.0 * wins / n : 0;

            sb.AppendLine($"• {(pl ? "Aktualnie" : "Currently")}: {rank}");
            sb.AppendLine(Check(wr >= 55, $"Winrate {wr:0}% ", "≥ 55%", pl));
            sb.AppendLine(Check(svg >= 1.4, $"{(pl ? "Obrony" : "Saves")} {svg:0.0} ", "≥ 1.4", pl));
            sb.AppendLine(Check(gpg >= 1.3, $"{(pl ? "Gole" : "Goals")} {gpg:0.0} ", "≥ 1.3", pl));
            sb.AppendLine(Check(shootPct >= 30, $"{(pl ? "Skuteczność" : "Shooting")} {shootPct:0}% ", "≥ 30%", pl));
        }

        private static string Check(bool ok, string label, string target, bool pl)
            => $"   {(ok ? "✔" : "✗")} {label} ({(pl ? "cel" : "target")} {target})";

        private static IEnumerable<string> Training(string weakCat, bool pl)
        {
            string c = weakCat.ToUpperInvariant();
            var list = new List<string>();
            if (c.Contains("OBRON") || c.Contains("DEF")) { list.Add("Shadow Defense"); list.Add(pl ? "Backboard Clears (obrona zza bramki)" : "Backboard Clears"); }
            else if (c.Contains("BOOST")) { list.Add(pl ? "Trasy zbierania małych padów" : "Small-pad boost routes"); list.Add(pl ? "Gra z 30-50 boosta" : "Playing with 30-50 boost"); }
            else if (c.Contains("POZYC") || c.Contains("POSITION")) { list.Add(pl ? "Trening rotacji 1-2-3" : "Rotation training 1-2-3"); list.Add(pl ? "Kick-off + retreat" : "Kick-off + retreat"); }
            else if (c.Contains("STRZA") || c.Contains("SHOOT")) { list.Add(pl ? "Striker (celność strzałów)" : "Striker (shot accuracy)"); list.Add(pl ? "Power Shots" : "Power Shots"); }
            else { list.Add(pl ? "Striker / kreowanie okazji" : "Striker / creating chances"); list.Add(pl ? "Passing plays" : "Passing plays"); }
            list.Add("Fast Aerial (5 min)");
            list.Add(pl ? "Czas: ~20-25 min" : "Time: ~20-25 min");
            return list;
        }

        private static string DayName(DayOfWeek d, bool pl)
        {
            if (!pl) return d.ToString();
            return d switch
            {
                DayOfWeek.Monday => "poniedziałek", DayOfWeek.Tuesday => "wtorek",
                DayOfWeek.Wednesday => "środa", DayOfWeek.Thursday => "czwartek",
                DayOfWeek.Friday => "piątek", DayOfWeek.Saturday => "sobota", _ => "niedziela"
            };
        }
    }
}
