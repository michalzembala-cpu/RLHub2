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
    // Per-map breakdown: which maps you actually win on, and how you play them. Everything is
    // computed from the matches we logged ourselves — GSI reports the map with every packet.
    //
    // Map art is optional: drop Resources\map_<name>.jpg (e.g. map_mirage.jpg) and it shows as
    // the card's thumbnail; without it the card falls back to a tinted plate with the initial.
    public class Cs2MapsPage : UserControl
    {
        private readonly Cs2SessionStore _store = new();
        private readonly SettingsStore _settings = new();

        private static readonly string[] ModeKeys = { "", "premier", "competitive", "casual" };
        private string _mode = "";

        private SegmentedControl _segMode = null!;
        private MapsPanel _body = null!;
        private List<MapStat> _maps = new();

        private sealed class MapStat
        {
            public string Map = "";
            public int Matches, Wins, Kills, Deaths, Headshots, Rounds, Damage, Mvps;
            public float WinPct => Matches == 0 ? 0 : 100f * Wins / Matches;
            public float Kd => Deaths == 0 ? Kills : Kills / (float)Deaths;
            public float Adr => Rounds == 0 ? 0 : Damage / (float)Rounds;
            public float HsPct => Kills == 0 ? 0 : 100f * Headshots / Kills;
        }

        public Cs2MapsPage()
        {
            BackColor = Theme.PageBg;
            Dock = DockStyle.Fill;

            var title = new Label
            {
                Text = Localization.IsPolish ? "MAPY" : "MAPS",
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(24, 8, 0, 0),
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
            };
            var hint = new Label
            {
                Text = Localization.IsPolish
                    ? "Na czym wygrywasz, a co banować"
                    : "What you win on, and what to ban",
                Dock = DockStyle.Top,
                Height = 26,
                Padding = new Padding(24, 0, 0, 0),
                ForeColor = Theme.TextMuted,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            };

            _segMode = new SegmentedControl { Height = 40, Width = 430, Location = new Point(24, 4) };
            var segRow = new Panel { Dock = DockStyle.Top, Height = 52 };
            segRow.Controls.Add(_segMode);

            _body = new MapsPanel(Render) { Dock = DockStyle.Fill };

            Controls.Add(_body);
            Controls.Add(segRow);
            Controls.Add(hint);
            Controls.Add(title);

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

            Theme.ThemeChanged += OnTheme;
            HandleDestroyed += (s, e) => Theme.ThemeChanged -= OnTheme;

            Load += (s, e) => Reload();
        }

        private void OnTheme() { if (!IsDisposed) { BackColor = Theme.PageBg; _body.Invalidate(); } }

        private void Reload()
        {
            var matches = _store.Load()
                .Where(m => _mode.Length == 0 || string.Equals(m.Mode, _mode, StringComparison.OrdinalIgnoreCase))
                .Where(m => !string.IsNullOrEmpty(m.Map));

            _maps = matches
                .GroupBy(m => m.Map)
                .Select(gr => new MapStat
                {
                    Map = gr.Key,
                    Matches = gr.Count(),
                    Wins = gr.Count(m => m.Won),
                    Kills = gr.Sum(m => m.Kills),
                    Deaths = gr.Sum(m => m.Deaths),
                    Headshots = gr.Sum(m => m.HeadshotKills),
                    Rounds = gr.Sum(m => m.Rounds),
                    Damage = gr.Sum(m => m.Damage),
                    Mvps = gr.Sum(m => m.Mvps),
                })
                // most-played first: a 100% winrate off one game shouldn't top the list
                .OrderByDescending(s => s.Matches)
                .ThenByDescending(s => s.WinPct)
                .ToList();

            _body.Relayout();
        }

        private int Render(Graphics g, int W)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int pad = 20, y = 12, x = pad, cw = W - pad * 2;

            if (_maps.Count == 0)
            {
                var rect = new Rectangle(x, y, cw, 110);
                Glass(g, rect, Theme.Accent, 16);
                using var f = new Font("Segoe UI", 12f);
                using var b = new SolidBrush(Theme.TextMuted);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(_mode.Length > 0
                        ? (Localization.IsPolish ? $"Brak meczów w trybie {Cap(_mode)}" : $"No {Cap(_mode)} matches yet")
                        : (Localization.IsPolish ? "Brak meczów — zagraj kilka w CS2" : "No matches yet — play some CS2"),
                    f, b, rect, sf);
                return y + 130;
            }

            foreach (var m in _maps)
            {
                y = DrawMapCard(g, x, y, cw, m) + 12;
            }
            return y + 8;
        }

        private int DrawMapCard(Graphics g, int x, int y, int W, MapStat m)
        {
            int h = 92;
            var rect = new Rectangle(x, y, W, h);
            // tint the card edge by result: green when you win here, orange when you don't
            var edge = m.WinPct >= 50 ? Color.FromArgb(46, 204, 113) : Theme.Accent;
            Glass(g, rect, edge, 16);

            // thumbnail
            var thumb = new Rectangle(x + 14, y + 16, 108, 60);
            DrawThumb(g, thumb, m.Map);

            using var nameFont = new Font("Segoe UI", 14f, FontStyle.Bold);
            using var subFont = new Font("Segoe UI", 9f);
            using var labelFont = new Font("Segoe UI", 8f, FontStyle.Bold);
            using var valFont = new Font("Segoe UI", 15f, FontStyle.Bold);
            using var tp = new SolidBrush(Theme.TextPrimary);
            using var tm = new SolidBrush(Theme.TextMuted);

            int tx = thumb.Right + 16;
            g.DrawString(Pretty(m.Map), nameFont, tp, tx, y + 20);
            g.DrawString($"{m.Matches} {(Localization.IsPolish ? "meczów" : "matches")}  •  {m.Wins}–{m.Matches - m.Wins}",
                subFont, tm, tx, y + 46);

            // stat columns from the right: HS% | ADR | K/D | WINRATE(+bar)
            int colW = 92;
            var cols = new (string Label, string Value)[]
            {
                (Localization.IsPolish ? "WYGRANE" : "WINRATE", $"{m.WinPct:0}%"),
                ("K/D", m.Kd.ToString("0.00")),
                ("ADR", m.Adr > 0 ? m.Adr.ToString("0") : "—"),
                ("HS%", m.HsPct > 0 ? $"{m.HsPct:0}%" : "—"),
            };

            int right = x + W - 16;
            for (int i = cols.Length - 1; i >= 0; i--)
            {
                int cxr = right - (cols.Length - i) * colW;
                g.DrawString(cols[i].Label, labelFont, tm, cxr, y + 22);
                g.DrawString(cols[i].Value, valFont, tp, cxr, y + 38);

                // a bar under the winrate makes the column scannable at a glance
                if (i == 0)
                {
                    int barX = cxr, barY = y + 64, barW = colW - 16, barH = 6;
                    using (var track = new SolidBrush(Theme.SurfaceAlt))
                    using (var tp2 = Rounded(new Rectangle(barX, barY, barW, barH), 3))
                        g.FillPath(track, tp2);
                    int fill = (int)(barW * m.WinPct / 100f);
                    if (fill > 2)
                        using (var fp = Rounded(new Rectangle(barX, barY, fill, barH), 3))
                        using (var fb = new SolidBrush(edge))
                            g.FillPath(fb, fp);
                }
            }

            return y + h;
        }

        private static void DrawThumb(Graphics g, Rectangle r, string map)
        {
            using var path = Rounded(r, 10);
            var img = ArenaBackground.Load(ImageFor(map));

            if (img != null)
            {
                var old = g.Clip;
                g.SetClip(path);
                float ir = img.Width / (float)img.Height, tr = r.Width / (float)r.Height;
                Rectangle dst = ir > tr
                    ? new Rectangle(r.X - (int)((r.Height * ir - r.Width) / 2), r.Y, (int)(r.Height * ir), r.Height)
                    : new Rectangle(r.X, r.Y - (int)((r.Width / ir - r.Height) / 2), r.Width, (int)(r.Width / ir));
                g.DrawImage(img, dst);
                g.Clip = old;
            }
            else
            {
                // No art for this map — a tinted plate with its initial still reads as "a map".
                using var bg = new LinearGradientBrush(r, Theme.SurfaceAlt, Theme.CardBottom, 45f);
                g.FillPath(bg, path);
                using var f = new Font("Segoe UI", 20f, FontStyle.Bold);
                using var b = new SolidBrush(Color.FromArgb(120, Theme.Accent));
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(Initial(map), f, b, r, sf);
            }

            using var pen = new Pen(Color.FromArgb(50, Theme.Accent), 1f);
            g.DrawPath(pen, path);
        }

        // de_mirage -> map_mirage.jpg
        private static string ImageFor(string map)
        {
            var s = map.StartsWith("de_") || map.StartsWith("cs_") ? map.Substring(3) : map;
            return "map_" + s.ToLowerInvariant() + ".jpg";
        }

        private static string Initial(string map)
        {
            var s = Pretty(map);
            return s.Length > 0 ? s.Substring(0, 1).ToUpperInvariant() : "?";
        }

        private static string Pretty(string map)
        {
            if (string.IsNullOrEmpty(map)) return "—";
            var s = map.StartsWith("de_") || map.StartsWith("cs_") ? map.Substring(3) : map;
            return s.Length == 0 ? map : char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        private static string Cap(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

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

        // Scrolling custom-paint host: measures the content to size the scrollbar, then paints
        // it under the scroll offset. Backdrop stays fixed to the viewport.
        private sealed class MapsPanel : Panel
        {
            private readonly Func<Graphics, int, int> _render;

            public MapsPanel(Func<Graphics, int, int> render)
            {
                _render = render;
                DoubleBuffered = true;
                AutoScroll = true;
                BackColor = Theme.PageBg;
                Resize += (s, e) => Relayout();
            }

            public void Relayout()
            {
                int w = ClientSize.Width;
                if (w < 40) return;
                int h;
                using (var bmp = new Bitmap(1, 1))
                using (var g = Graphics.FromImage(bmp))
                    h = _render(g, w);
                AutoScrollMinSize = new Size(0, h);
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                ArenaBackground.Paint(g, ClientSize.Width, ClientSize.Height,
                    ArenaBackground.Load("cs2_bg.png"), Theme.IsDark);
                g.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);
                _render(g, ClientSize.Width);
            }
        }
    }
}
