using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using RLHub2.Controls;
using RLHub2.Helpers;
using RLHub2.Models;
using RLHub2.Services;

namespace RLHub2
{
    // CS2 dashboard. A hero band up top (name + Premier rating + winrate), a grid of compact
    // stat cards, the last match, and a list of recent ones — all custom-painted so it reads
    // like a CS2 tool rather than an admin panel.
    //
    // Stats cover the last 30 matches of the chosen mode, not just this sitting: a session-only
    // K/D swings wildly on two games. Premier rating is the user's own entry (GSI has no rank,
    // and Leetify, which does, forbids storing its data).
    public partial class Cs2Page : UserControl
    {
        private const int MaxStatMatches = 30;
        private const int RecentRows = 8;

        private readonly Cs2SessionStore _store = new();
        private readonly Cs2RatingStore _rating = new();
        private readonly SettingsStore _settings = new();
        private readonly Cs2GsiClient _client = Cs2GsiClient.Instance;

        private static readonly string[] ModeKeys = { "", "premier", "competitive", "casual" };
        private string _mode = "";

        private SegmentedControl _segMode = null!;
        private Label _status = null!;
        private DashPanel _body = null!;

        private List<Cs2Match> _matches = new();   // last N of the mode, newest last
        private Rectangle _heroHit;                 // click target to set Premier rating

        public Cs2Page()
        {
            InitializeComponent();
            BackColor = Theme.PageBg;
            Dock = DockStyle.Fill;

            BuildUi();

            _mode = _settings.LoadCs2ModeFilter();
            int idx = Array.IndexOf(ModeKeys, _mode);
            _segMode.SetOptions(new[] { Localization.IsPolish ? "Wszystkie" : "All", "Premier", "Competitive", "Casual" });
            _segMode.SetSelectedSilent(idx < 0 ? 0 : idx);
            _segMode.SelectedIndexChanged += (s, e) =>
            {
                _mode = ModeKeys[Math.Clamp(_segMode.SelectedIndex, 0, ModeKeys.Length - 1)];
                _settings.SaveCs2ModeFilter(_mode);
                Reload();
            };

            _client.ConnectionChanged += OnConn;
            _client.MatchLogged += OnMatch;
            Theme.ThemeChanged += OnTheme;
            HandleDestroyed += (s, e) =>
            {
                _client.ConnectionChanged -= OnConn;
                _client.MatchLogged -= OnMatch;
                Theme.ThemeChanged -= OnTheme;
            };

            Load += (s, e) => { _client.Start(); Reload(); };
        }

        private void BuildUi()
        {
            var title = new Label
            {
                Text = "CS2",
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(24, 8, 0, 0),
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
            };

            _status = new Label
            {
                Dock = DockStyle.Top,
                Height = 26,
                Padding = new Padding(24, 0, 0, 0),
                ForeColor = Theme.TextMuted,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
            };

            _segMode = new SegmentedControl
            {
                Height = 40,
                Width = 430,
                Location = new Point(24, 0),
            };
            var segRow = new Panel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(0, 4, 0, 8) };
            _segMode.Top = 4;
            segRow.Controls.Add(_segMode);

            _body = new DashPanel(Render) { Dock = DockStyle.Fill };
            _body.MouseClick += BodyClick;

            Controls.Add(_body);
            Controls.Add(segRow);
            Controls.Add(_status);
            Controls.Add(title);
        }

        private void OnConn(bool c) { if (!IsDisposed) UpdateStatus(); }
        private void OnTheme() { if (!IsDisposed) { BackColor = Theme.PageBg; _body.Invalidate(); } }

        private void OnMatch(Cs2Match m)
        {
            Reload();
            string res = m.Draw ? (Localization.IsPolish ? "Remis" : "Draw")
                       : m.Won ? (Localization.IsPolish ? "Wygrana" : "Win")
                               : (Localization.IsPolish ? "Przegrana" : "Loss");
            Toast.Show(this, $"{res}  {m.RoundsWon}:{m.RoundsLost}  •  {PrettyMap(m.Map)}",
                m.Won ? ToastKind.Success : ToastKind.Info, 3500);
        }

        private void Reload()
        {
            _matches = _store.Load()
                .Where(m => _mode.Length == 0 || string.Equals(m.Mode, _mode, StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => m.Time)
                .TakeLast(MaxStatMatches)
                .ToList();
            UpdateStatus();
            _body.Relayout();
        }

        private void UpdateStatus()
        {
            if (!Cs2Install.IsInstalled)
                Set("⚪  " + (Localization.IsPolish ? "Nie znaleziono CS2 na tym komputerze" : "CS2 not found on this PC"), Theme.TextMuted);
            else if (_client.IsConnected)
            {
                string where = string.IsNullOrEmpty(_client.CurrentMap)
                    ? (Localization.IsPolish ? "w menu" : "in menu")
                    : $"{PrettyMap(_client.CurrentMap)}  {_client.RoundsWon}:{_client.RoundsLost}";
                Set($"🟢  CS2  •  {where}", Color.FromArgb(46, 204, 113));
            }
            else
                Set("⚪  " + (Localization.IsPolish ? "Czekam na CS2 — jeśli gra jest włączona, zrestartuj ją" : "Waiting for CS2 — if it's running, restart it"), Theme.TextMuted);

            void Set(string t, Color c) { _status.Text = t; _status.ForeColor = c; }
        }

        // ===== click: set Premier rating by tapping the hero =====
        private void BodyClick(object? sender, MouseEventArgs e)
        {
            var p = new Point(e.X - _body.AutoScrollPosition.X * 0, e.Y);
            var hit = _heroHit;
            hit.Offset(0, _body.AutoScrollPosition.Y);
            if (hit.Contains(e.Location))
            {
                int? v = RatingDialog.Ask(FindForm(), _rating.Latest()?.Value);
                if (v.HasValue) { _rating.Append(v.Value); _body.Invalidate(); }
            }
        }

        // ===== rendering =====

        private int Render(Graphics g, int W)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int pad = 24, gap = 16;
            int x = pad, y = pad, cw = W - pad * 2;

            y = DrawHero(g, x, y, cw); y += gap + 2;

            if (_matches.Count == 0)
            {
                DrawEmpty(g, x, y, cw);
                return y + 120;
            }

            y = DrawStatGrid(g, x, y, cw, gap); y += gap + 2;
            y = DrawLastMatch(g, x, y, cw); y += gap + 2;
            y = DrawRecent(g, x, y, cw);
            return y + pad;
        }

        private int DrawHero(Graphics g, int x, int y, int W)
        {
            int h = 190;
            var rect = new Rectangle(x, y, W, h);
            _heroHit = rect;
            Glass(g, rect, Theme.Accent, 20);

            var stats = Totals();
            var latest = _rating.Latest();
            int delta = _rating.Delta();
            string name = _matches.LastOrDefault()?.Name is { Length: > 0 } n ? n : "Player";

            using var nameFont = new Font("Segoe UI", 25f, FontStyle.Bold);
            using var labelFont = new Font("Segoe UI", 9f, FontStyle.Bold);
            using var ratingFont = new Font("Segoe UI", 34f, FontStyle.Bold);
            using var small = new Font("Segoe UI", 10f);
            using var recFont = new Font("Segoe UI", 20f, FontStyle.Bold);
            using var tp = new SolidBrush(Theme.TextPrimary);
            using var tm = new SolidBrush(Theme.TextMuted);
            using var ac = new SolidBrush(Theme.Accent);

            g.DrawString(name, nameFont, tp, x + 26, y + 18);

            // Premier rating (or a hint to set it)
            g.DrawString("PREMIER RATING", labelFont, tm, x + 28, y + 68);
            if (latest != null)
            {
                g.DrawString(latest.Value.ToString("N0"), ratingFont, ac, x + 26, y + 82);
                float rw = g.MeasureString(latest.Value.ToString("N0"), ratingFont).Width;
                if (delta != 0)
                {
                    var dc = delta > 0 ? Color.FromArgb(46, 204, 113) : Color.FromArgb(230, 80, 90);
                    using var db = new SolidBrush(dc);
                    g.DrawString((delta > 0 ? "▲ +" : "▼ ") + Math.Abs(delta), small, db, x + 36 + rw, y + 102);
                }
            }
            else
            {
                using var hint = new Font("Segoe UI", 14f, FontStyle.Bold);
                using var hb = new SolidBrush(Theme.TextSecondary);
                g.DrawString(Localization.IsPolish ? "— kliknij, aby ustawić" : "— click to set", hint, hb, x + 26, y + 88);
            }

            // right: record
            int w = stats.Wins, l = stats.Matches - stats.Wins;
            string rec = $"{w}–{l}";
            var recSize = g.MeasureString(rec, recFont);
            g.DrawString(rec, recFont, tp, x + W - recSize.Width - 28, y + 22);
            string recLbl = Localization.IsPolish ? "BILANS" : "RECORD";
            g.DrawString(recLbl, labelFont, tm, x + W - g.MeasureString(recLbl, labelFont).Width - 28, y + 54);

            // winrate bar along the bottom, clear of the rating number
            int barY = y + h - 30, barX = x + 26, barW = W - 52, barH = 10;
            g.DrawString($"Winrate {stats.WinPct}%  •  {stats.Matches} {(Localization.IsPolish ? "meczów" : "matches")}",
                small, tm, barX, barY - 22);
            using (var track = new SolidBrush(Theme.SurfaceAlt))
            using (var tpath = Rounded(new Rectangle(barX, barY, barW, barH), 5))
                g.FillPath(track, tpath);
            int fillW = (int)(barW * stats.WinPct / 100f);
            if (fillW > 4)
                using (var fp = Rounded(new Rectangle(barX, barY, fillW, barH), 5))
                using (var fb = new LinearGradientBrush(new Rectangle(barX, barY, barW, barH), Theme.Accent, Theme.AccentSoft, 0f))
                    g.FillPath(fb, fp);

            return y + h;
        }

        private int DrawStatGrid(Graphics g, int x, int y, int W, int gap)
        {
            var s = Totals();
            var cells = new (string Label, string Value, string Sub, Color Accent)[]
            {
                ("K/D", s.Kd.ToString("0.00"), $"{s.Kills} / {s.Deaths}", Theme.Accent),
                ("ADR", s.Adr > 0 ? s.Adr.ToString("0") : "—", Localization.IsPolish ? "na rundę" : "per round", Theme.Cs2Blue),
                ("HS%", s.HsPct > 0 ? s.HsPct.ToString("0") + "%" : "—", $"{s.Headshots} / {s.Kills}", Theme.Accent),
                (Localization.IsPolish ? "WYGRANE" : "WINRATE", s.WinPct + "%", $"{s.Wins}–{s.Matches - s.Wins}", Color.FromArgb(46, 204, 113)),
                (Localization.IsPolish ? "ZABÓJSTWA/MECZ" : "KILLS/GAME", s.Matches > 0 ? ((float)s.Kills / s.Matches).ToString("0.0") : "—", "", Theme.Cs2Blue),
                ("MVP", s.Mvps.ToString(), Localization.IsPolish ? "łącznie" : "total", Theme.Accent),
            };

            int cols = 3;
            int cw = (W - gap * (cols - 1)) / cols;
            int ch = 92;
            for (int i = 0; i < cells.Length; i++)
            {
                int r = i / cols, c = i % cols;
                var rect = new Rectangle(x + c * (cw + gap), y + r * (ch + gap), cw, ch);
                StatCard(g, rect, cells[i].Label, cells[i].Value, cells[i].Sub, cells[i].Accent);
            }
            int rows = (cells.Length + cols - 1) / cols;
            return y + rows * ch + (rows - 1) * gap;
        }

        private int DrawLastMatch(Graphics g, int x, int y, int W)
        {
            int h = 116;
            var rect = new Rectangle(x, y, W, h);
            var m = _matches.Last();
            var col = m.Draw ? Color.FromArgb(170, 170, 180) : m.Won ? Color.FromArgb(46, 204, 113) : Color.FromArgb(230, 80, 90);
            Glass(g, rect, col, 18);

            using var labelFont = new Font("Segoe UI", 9f, FontStyle.Bold);
            using var mapFont = new Font("Segoe UI", 17f, FontStyle.Bold);
            using var scoreFont = new Font("Segoe UI", 30f, FontStyle.Bold);
            using var statFont = new Font("Segoe UI", 11f);
            using var tp = new SolidBrush(Theme.TextPrimary);
            using var tm = new SolidBrush(Theme.TextMuted);
            using var cb = new SolidBrush(col);

            g.DrawString(Localization.IsPolish ? "OSTATNI MECZ" : "LAST MATCH", labelFont, tm, x + 24, y + 16);
            g.DrawString(PrettyMap(m.Map), mapFont, tp, x + 24, y + 34);
            string res = m.Draw ? "DRAW" : m.Won ? "WIN" : "LOSS";
            g.DrawString(res, statFont, cb, x + 24, y + 70);

            string score = $"{m.RoundsWon} : {m.RoundsLost}";
            var ss = g.MeasureString(score, scoreFont);
            g.DrawString(score, scoreFont, tp, x + W / 2 - ss.Width / 2, y + h / 2 - ss.Height / 2);

            // right: K-D-A / ADR / MVP
            string line1 = $"K {m.Kills}   D {m.Deaths}   A {m.Assists}";
            string line2 = $"ADR {(m.Rounds > 0 ? m.Adr.ToString("0") : "—")}   MVP {m.Mvps}   HS {(m.Kills > 0 ? m.HeadshotPct.ToString("0") + "%" : "—")}";
            var s1 = g.MeasureString(line1, statFont);
            var s2 = g.MeasureString(line2, statFont);
            g.DrawString(line1, statFont, tp, x + W - Math.Max(s1.Width, s2.Width) - 24, y + 42);
            g.DrawString(line2, statFont, tm, x + W - Math.Max(s1.Width, s2.Width) - 24, y + 66);

            return y + h;
        }

        private int DrawRecent(Graphics g, int x, int y, int W)
        {
            using var head = new Font("Segoe UI", 12f, FontStyle.Bold);
            using var hb = new SolidBrush(Theme.AccentSoft);
            g.DrawString(Localization.IsPolish ? "OSTATNIE MECZE" : "RECENT MATCHES", head, hb, x + 2, y);
            y += 30;

            using var resFont = new Font("Segoe UI", 11f, FontStyle.Bold);
            using var mapFont = new Font("Segoe UI", 11f, FontStyle.Bold);
            using var statFont = new Font("Segoe UI", 10f);
            using var timeFont = new Font("Segoe UI", 9f);
            var win = Color.FromArgb(46, 204, 113);
            var loss = Color.FromArgb(230, 80, 90);
            var draw = Color.FromArgb(170, 170, 190);

            int rowH = 52;
            foreach (var m in Enumerable.Reverse(_matches).Take(RecentRows))
            {
                var rect = new Rectangle(x, y, W, rowH - 8);
                using (var p = Rounded(rect, 12))
                using (var bg = new LinearGradientBrush(rect, Theme.CardTop, Theme.CardBottom, 90f))
                    g.FillPath(bg, p);

                var col = m.Draw ? draw : m.Won ? win : loss;
                string tag = m.Draw ? "D" : m.Won ? "W" : "L";
                var badge = new Rectangle(rect.X + 10, rect.Y + 8, 40, rect.Height - 16);
                using (var bp = Rounded(badge, 8))
                {
                    using var bb = new SolidBrush(Color.FromArgb(40, col));
                    g.FillPath(bb, bp);
                    using var bpen = new Pen(col, 1.4f);
                    g.DrawPath(bpen, bp);
                }
                using (var tb = new SolidBrush(col))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(tag, resFont, tb, badge, sf);
                }

                using (var tp = new SolidBrush(Theme.TextPrimary))
                    g.DrawString($"{PrettyMap(m.Map)}", mapFont, tp, badge.Right + 14, rect.Y + 6);
                using (var tm = new SolidBrush(Theme.TextSecondary))
                    g.DrawString($"{m.RoundsWon}:{m.RoundsLost}   •   K {m.Kills}  D {m.Deaths}  A {m.Assists}   •   ADR {(m.Rounds > 0 ? m.Adr.ToString("0") : "—")}   •   MVP {m.Mvps}",
                        statFont, tm, badge.Right + 14, rect.Y + 24);

                using (var tmb = new SolidBrush(Theme.TextMuted))
                {
                    string t = m.Time.ToString("HH:mm");
                    var sz = g.MeasureString(t, timeFont);
                    g.DrawString(t, timeFont, tmb, rect.Right - sz.Width - 12, rect.Y + 8);
                }

                y += rowH;
            }
            return y;
        }

        private void DrawEmpty(Graphics g, int x, int y, int W)
        {
            var rect = new Rectangle(x, y, W, 110);
            Glass(g, rect, Theme.Accent, 18);
            using var f = new Font("Segoe UI", 12f);
            using var b = new SolidBrush(Theme.TextMuted);
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            bool filtered = _mode.Length > 0;
            string msg = filtered
                ? (Localization.IsPolish ? $"Brak meczów w trybie {Cap(_mode)} — zagraj kilka" : $"No {Cap(_mode)} matches yet — play a few")
                : (Localization.IsPolish ? "Brak meczów — włącz CS2 i zagraj" : "No matches yet — launch CS2 and play");
            g.DrawString(msg, f, b, rect, sf);
        }

        // ===== stat card =====
        private void StatCard(Graphics g, Rectangle rect, string label, string value, string sub, Color accent)
        {
            Glass(g, rect, accent, 14);
            using (var stripe = new SolidBrush(accent))
                g.FillRectangle(stripe, rect.X + 1, rect.Y + 12, 4, rect.Height - 24);

            using var labelFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            using var valueFont = new Font("Segoe UI", 24f, FontStyle.Bold);
            using var subFont = new Font("Segoe UI", 8.5f);
            using var lb = new SolidBrush(Theme.TextMuted);
            using var vb = new SolidBrush(Theme.TextPrimary);
            using var sb = new SolidBrush(Theme.TextSecondary);

            g.DrawString(label, labelFont, lb, rect.X + 16, rect.Y + 14);
            g.DrawString(value, valueFont, vb, rect.X + 14, rect.Y + 32);
            if (!string.IsNullOrEmpty(sub))
                g.DrawString(sub, subFont, sb, rect.X + 16, rect.Bottom - 22);
        }

        // ===== aggregate =====
        private struct Agg
        {
            public int Matches, Wins, Kills, Deaths, Headshots, Mvps, Rounds, Damage;
            public float Kd, Adr, HsPct, WinPct;
        }

        private Agg Totals()
        {
            var a = new Agg { Matches = _matches.Count };
            if (a.Matches == 0) return a;
            a.Wins = _matches.Count(m => m.Won);
            a.Kills = _matches.Sum(m => m.Kills);
            a.Deaths = _matches.Sum(m => m.Deaths);
            a.Headshots = _matches.Sum(m => m.HeadshotKills);
            a.Mvps = _matches.Sum(m => m.Mvps);
            a.Rounds = _matches.Sum(m => m.Rounds);
            a.Damage = _matches.Sum(m => m.Damage);
            a.Kd = a.Deaths == 0 ? a.Kills : a.Kills / (float)a.Deaths;
            a.Adr = a.Rounds == 0 ? 0 : a.Damage / (float)a.Rounds;
            a.HsPct = a.Kills == 0 ? 0 : 100f * a.Headshots / a.Kills;
            a.WinPct = (int)Math.Round(100.0 * a.Wins / a.Matches);
            return a;
        }

        // ===== helpers =====
        private static void Glass(Graphics g, Rectangle rect, Color accent, int radius)
        {
            using var p = Rounded(rect, radius);
            using var bg = new LinearGradientBrush(rect, Theme.CardTop, Theme.CardBottom, 90f);
            g.FillPath(bg, p);
            using var pen = new Pen(Color.FromArgb(60, accent), 1f);
            g.DrawPath(pen, p);
        }

        private static GraphicsPath Rounded(Rectangle r, int radius)
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

        private static string PrettyMap(string map)
        {
            if (string.IsNullOrEmpty(map)) return "—";
            var s = map.StartsWith("de_") || map.StartsWith("cs_") ? map.Substring(3) : map;
            return s.Length == 0 ? map : char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        private static string Cap(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

        // ===== scrolling custom-paint panel =====
        // Draws through the render callback into a content area whose height it reports, so the
        // panel scrolls when the dashboard is taller than the view.
        private sealed class DashPanel : Panel
        {
            private readonly Func<Graphics, int, int> _render;
            private int _contentH;

            public DashPanel(Func<Graphics, int, int> render)
            {
                _render = render;
                DoubleBuffered = true;
                AutoScroll = true;
                BackColor = Theme.PageBg;
                Resize += (s, e) => Relayout();
            }

            // Measure once (offscreen) to know the scroll height, then set it so a scrollbar
            // appears exactly when the content overflows.
            public void Relayout()
            {
                int w = ClientSize.Width;
                if (w < 40) return;
                using (var bmp = new Bitmap(1, 1))
                using (var g = Graphics.FromImage(bmp))
                    _contentH = _render(g, w);
                AutoScrollMinSize = new Size(0, _contentH + 8);
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;

                // CS2 art behind the glass, fixed to the viewport (drawn before the scroll
                // transform) so it stays put while the dashboard scrolls over it — same subtle
                // backdrop role the arena plays on the Rocket League pages.
                ArenaBackground.Paint(g, ClientSize.Width, ClientSize.Height,
                    ArenaBackground.Load("cs2_bg.png"), Theme.IsDark);

                g.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);
                _render(g, ClientSize.Width);
            }
        }

        // ===== tiny modal to enter Premier rating =====
        private static class RatingDialog
        {
            public static int? Ask(IWin32Window? owner, int? current)
            {
                using var f = new Form
                {
                    Text = Localization.IsPolish ? "Premier Rating" : "Premier Rating",
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    ClientSize = new Size(360, 150),
                    BackColor = Color.FromArgb(20, 23, 29),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10f),
                };
                var lbl = new Label
                {
                    Text = Localization.IsPolish ? "Wpisz aktualny Premier Rating (z gry):" : "Enter your current Premier Rating (from the game):",
                    AutoSize = false, Location = new Point(20, 18), Size = new Size(320, 40),
                    ForeColor = Color.FromArgb(180, 186, 198),
                };
                var box = new TextBox
                {
                    Location = new Point(20, 60), Size = new Size(320, 28),
                    BackColor = Color.FromArgb(30, 35, 44), ForeColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                    Text = current?.ToString() ?? "",
                };
                var ok = new Button
                {
                    Text = "OK", DialogResult = DialogResult.OK,
                    Location = new Point(230, 104), Size = new Size(110, 30),
                    FlatStyle = FlatStyle.Flat, BackColor = Theme.Accent, ForeColor = Color.Black,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Cursor = Cursors.Hand,
                };
                ok.FlatAppearance.BorderSize = 0;
                var cancel = new Button
                {
                    Text = Localization.IsPolish ? "Anuluj" : "Cancel", DialogResult = DialogResult.Cancel,
                    Location = new Point(110, 104), Size = new Size(110, 30),
                    FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(40, 45, 55), ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5f), Cursor = Cursors.Hand,
                };
                cancel.FlatAppearance.BorderSize = 0;
                f.Controls.AddRange(new Control[] { lbl, box, ok, cancel });
                f.AcceptButton = ok;
                f.CancelButton = cancel;
                box.SelectAll();

                if (f.ShowDialog(owner) != DialogResult.OK) return null;
                var digits = new string(box.Text.Where(char.IsDigit).ToArray());
                return int.TryParse(digits, out int v) && v > 0 ? v : null;
            }
        }
    }
}
