using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using RLHub2.Helpers;
using RLHub2.Models;
using RLHub2.Services;

namespace RLHub2
{
    public partial class RecordsPage : Controls.ArenaControl
    {
        protected override string ArenaFile => "stadin3.jpg";

        private List<SeasonSnapshot> _history = new();

        public RecordsPage()
        {
            InitializeComponent();
            ApplyLanguage();

            historyPanel.Paint += DrawHistory;
            historyPanel.Resize += (s, e) => historyPanel.Invalidate();

            Load += (s, e) =>
            {
                new SeasonHistoryStore().ArchiveIfEnded();
                Compute();
            };
        }

        private void ApplyLanguage()
        {
            lblTitle.Text = Localization.T("records_title");
            lblHistory.Text = Localization.T("records_history");
            tileMmr.Title = Localization.T("records_mmr");
            tileStreak.Title = Localization.T("records_streak");
            tileGoals.Title = Localization.T("records_goals");
            tileGain.Title = Localization.T("records_gain");
            tileSession.Title = Localization.T("records_session");
        }

        private void Compute()
        {
            var mmr = new MmrStore().Load();
            var ball = new BallMatchStore().Load();
            var session = new SessionStore().Load();

            // Highest MMR ever
            if (mmr.Count > 0)
            {
                var top = mmr.OrderByDescending(e => e.Value).First();
                tileMmr.Value = top.Value.ToString();
                tileMmr.Subtitle = $"{top.Mode}  ·  {top.Timestamp:dd.MM.yyyy}";
            }

            // Longest win streak (prefer ballchasing history, fall back to live sessions)
            var wl = ball.OrderBy(m => m.Date).Select(m => m.Won).ToList();
            if (wl.Count == 0) wl = session.OrderBy(m => m.Time).Select(m => m.Won).ToList();
            int streak = LongestRun(wl);
            if (wl.Count > 0)
            {
                tileStreak.Value = $"{streak}W";
                tileStreak.Subtitle = $"{wl.Count} {Localization.T("records_games")}";
            }

            // Most goals in one match
            if (ball.Count > 0)
            {
                var g = ball.OrderByDescending(m => m.Goals).First();
                tileGoals.Value = g.Goals.ToString();
                tileGoals.Subtitle = $"{(g.Mode.Length > 0 ? g.Mode + "  ·  " : "")}{g.Date:dd.MM.yyyy}";
            }

            // Biggest MMR gain in one day (net change within a calendar day)
            int bestGain = 0; DateTime bestDay = default;
            foreach (var day in mmr.GroupBy(e => e.Timestamp.Date))
            {
                var ord = day.OrderBy(e => e.Timestamp).ToList();
                int gain = ord[^1].Value - ord[0].Value;
                if (gain > bestGain) { bestGain = gain; bestDay = day.Key; }
            }
            if (bestGain > 0)
            {
                tileGain.Value = $"+{bestGain}";
                tileGain.Subtitle = bestDay.ToString("dd.MM.yyyy");
            }

            // Best session (live matches grouped by >2h gaps; best win/loss record)
            var groups = new List<List<SessionMatch>>();
            foreach (var m in session.OrderBy(x => x.Time))
            {
                if (groups.Count == 0 || (m.Time - groups[^1][^1].Time).TotalHours > 2)
                    groups.Add(new List<SessionMatch>());
                groups[^1].Add(m);
            }
            if (groups.Count > 0)
            {
                var best = groups
                    .Select(gp => new { W = gp.Count(x => x.Won), L = gp.Count(x => !x.Won), When = gp[0].Time })
                    .OrderByDescending(x => x.W - x.L).ThenByDescending(x => x.W).First();
                tileSession.Value = $"{best.W}–{best.L}";
                tileSession.Subtitle = best.When.ToString("dd.MM.yyyy");
            }

            // season history (current in-progress + archived)
            _history = new List<SeasonSnapshot>();
            var current = SeasonStats.ComputeCurrent();
            if (current.Matches > 0) _history.Add(current);
            _history.AddRange(new SeasonHistoryStore().Load());
            _history = _history.OrderByDescending(s => s.InProgress).ThenByDescending(s => s.EndedOn).ToList();

            historyPanel.Invalidate();
        }

        private static int LongestRun(List<bool> results)
        {
            int best = 0, cur = 0;
            foreach (var w in results)
            {
                cur = w ? cur + 1 : 0;
                if (cur > best) best = cur;
            }
            return best;
        }

        private void DrawHistory(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int W = historyPanel.Width, H = historyPanel.Height;
            if (W < 40 || H < 40) return;

            using (var path = RoundRect(new Rectangle(0, 0, W - 1, H - 1), 18))
            {
                using (var bg = new LinearGradientBrush(new Rectangle(0, 0, W, H), Theme.CardTop, Theme.CardBottom, 90f))
                    g.FillPath(bg, path);
                using (var border = new Pen(Color.FromArgb(55, Theme.Accent), 1f))
                    g.DrawPath(border, path);
            }

            if (_history.Count == 0)
            {
                using var ef = new Font("Segoe UI", 11f);
                using var eb = new SolidBrush(Theme.TextMuted);
                using var esf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(Localization.T("records_history_empty"), ef, eb, new RectangleF(0, 0, W, H), esf);
                return;
            }

            using var nameFont = new Font("Segoe UI", 13f, FontStyle.Bold);
            using var statFont = new Font("Segoe UI", 10f);
            using var tagFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            var accent = Theme.Accent;

            int pad = 18, rowH = 60, y = 12;
            foreach (var s in _history)
            {
                if (y + rowH > H) break;

                using (var nb = new SolidBrush(Theme.TextPrimary))
                    g.DrawString(s.Season, nameFont, nb, pad, y + 4);

                if (s.InProgress)
                {
                    var tag = new Rectangle(pad + 120, y + 8, 62, 18);
                    using (var tp = RoundRect(tag, 8))
                    using (var tb = new SolidBrush(Color.FromArgb(40, accent)))
                        g.FillPath(tb, tp);
                    using (var tt = new SolidBrush(accent))
                        g.DrawString(Localization.T("records_in_progress"), tagFont, tt, tag.X + 8, tag.Y + 2);
                }

                string line = $"Peak {Dash(s.PeakRank)}   ·   Final {Dash(s.FinalRank)}";
                string line2 = $"MMR {s.HighestMmr}   ·   WR {s.WinRate}%   ·   {s.Matches} {Localization.T("records_games")}";
                using (var sb = new SolidBrush(Theme.TextSecondary))
                    g.DrawString(line, statFont, sb, pad, y + 30);
                using (var mb = new SolidBrush(Theme.TextMuted))
                {
                    var sz = g.MeasureString(line2, statFont);
                    g.DrawString(line2, statFont, mb, W - pad - sz.Width, y + 8);
                }

                y += rowH;
                using (var sep = new Pen(Color.FromArgb(30, Theme.TextMuted), 1f))
                    if (y + rowH <= H) g.DrawLine(sep, pad, y - 6, W - pad, y - 6);
            }
        }

        private static string Dash(string s) => string.IsNullOrWhiteSpace(s) ? "—" : s;

        private static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseAllFigures();
            return p;
        }
    }
}
