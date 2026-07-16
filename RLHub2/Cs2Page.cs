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
    // Counter-Strike 2 session tracker, fed by Game State Integration.
    //
    // Same deal as the Rocket League tracker and for the same reason: the live feed carries
    // match stats but no rank and no CS Rating, so this counts what happened this session
    // rather than pretending to track a rating.
    public partial class Cs2Page : Controls.ArenaControl
    {
        private readonly Cs2SessionStore _store = new();
        private readonly SettingsStore _settings = new();
        private readonly Cs2GsiClient _client = Cs2GsiClient.Instance;
        private List<Cs2Match> _session = new();

        // "" = every mode. GSI reports the mode per match, so the filter is just a view over
        // what we already store — no data is thrown away by choosing one.
        private static readonly string[] ModeKeys = { "", "premier", "competitive", "casual" };
        private string _mode = "";

        public Cs2Page()
        {
            InitializeComponent();
            ApplyLanguage();

            _mode = _settings.LoadCs2ModeFilter();
            int idx = Array.IndexOf(ModeKeys, _mode);
            segMode.SetSelectedSilent(idx < 0 ? 0 : idx);
            segMode.SelectedIndexChanged += (s, e) =>
            {
                _mode = ModeKeys[Math.Clamp(segMode.SelectedIndex, 0, ModeKeys.Length - 1)];
                _settings.SaveCs2ModeFilter(_mode);
                RefreshStats();
            };

            recentPanel.Paint += DrawRecent;
            recentPanel.Resize += (s, e) => recentPanel.Invalidate();
            btnReset.Click += (s, e) => { _store.Clear(); RefreshStats(); };

            _client.ConnectionChanged += OnConnectionChanged;
            _client.MatchLogged += OnMatchLogged;
            HandleDestroyed += (s, e) =>
            {
                _client.ConnectionChanged -= OnConnectionChanged;
                _client.MatchLogged -= OnMatchLogged;
            };

            Load += (s, e) =>
            {
                _client.Start();
                UpdateStatus();
                RefreshStats();
            };
        }

        private void ApplyLanguage()
        {
            lblTitle.Text = Localization.IsPolish ? "TRACKER CS2" : "CS2 TRACKER";
            lblRecent.Text = Localization.IsPolish ? "OSTATNIE MECZE" : "RECENT MATCHES";
            btnReset.Text = Localization.IsPolish ? "RESET" : "RESET";
            tileRecord.Title = Localization.IsPolish ? "W – P" : "W – L";
            tileWinRate.Title = Localization.IsPolish ? "WYGRANE %" : "WIN RATE";
            tileKd.Title = "K/D";
            tileMatches.Title = Localization.IsPolish ? "MECZE" : "MATCHES";
            tileKills.Title = Localization.IsPolish ? "ZABÓJSTWA/MECZ" : "KILLS/GAME";
            tileDeaths.Title = Localization.IsPolish ? "ŚMIERCI/MECZ" : "DEATHS/GAME";
            tileMvp.Title = "MVP";

            segMode.SetOptions(new[]
            {
                Localization.IsPolish ? "Wszystkie" : "All",
                "Premier", "Competitive", "Casual",
            });
        }

        private void OnConnectionChanged(bool connected) => UpdateStatus();

        private void OnMatchLogged(Cs2Match m)
        {
            RefreshStats();
            string res = m.Draw ? (Localization.IsPolish ? "Remis" : "Draw")
                       : m.Won ? (Localization.IsPolish ? "Wygrana" : "Win")
                               : (Localization.IsPolish ? "Przegrana" : "Loss");
            Toast.Show(this, $"{res}  {m.RoundsWon}:{m.RoundsLost}  •  {PrettyMap(m.Map)}",
                m.Won ? ToastKind.Success : ToastKind.Info, 3500);
        }

        // Three things can be wrong before any data arrives, and they need different fixes —
        // so say which one it is instead of a generic "waiting".
        private void UpdateStatus()
        {
            if (!Cs2Install.IsInstalled)
            {
                lblStatus.Text = "⚪  " + (Localization.IsPolish
                    ? "Nie znaleziono CS2 na tym komputerze"
                    : "CS2 not found on this PC");
                lblStatus.ForeColor = Theme.TextMuted;
                return;
            }

            if (_client.IsConnected)
            {
                string where = string.IsNullOrEmpty(_client.CurrentMap)
                    ? (Localization.IsPolish ? "w menu" : "in menu")
                    : $"{PrettyMap(_client.CurrentMap)}  {_client.RoundsWon}:{_client.RoundsLost}";
                lblStatus.Text = $"🟢  CS2  •  {where}";
                lblStatus.ForeColor = Color.FromArgb(46, 204, 113);
                return;
            }

            lblStatus.Text = "⚪  " + (Localization.IsPolish
                ? "Czekam na CS2 — jeśli gra jest włączona, zrestartuj ją"
                : "Waiting for CS2 — if it's running, restart it");
            lblStatus.ForeColor = Theme.TextMuted;
        }

        private void RefreshStats()
        {
            _session = _store.Load()
                .Where(m => m.Time >= _client.StartedAt)
                .Where(m => _mode.Length == 0 || string.Equals(m.Mode, _mode, StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => m.Time)
                .ToList();

            int wins = _session.Count(m => m.Won);
            int losses = _session.Count(m => !m.Won && !m.Draw);
            int n = _session.Count;

            tileRecord.Value = $"{wins} – {losses}";
            tileRecord.Subtitle = n > 0 ? $"{n} {(Localization.IsPolish ? "meczów" : "matches")}" : "";

            tileWinRate.Value = n > 0 ? $"{(int)Math.Round(100.0 * wins / n)}%" : "—";

            int k = _session.Sum(m => m.Kills), d = _session.Sum(m => m.Deaths);
            tileKd.Value = n == 0 ? "—" : (d == 0 ? k.ToString("0.00") : (k / (float)d).ToString("0.00"));
            tileKd.Subtitle = n > 0 ? $"{k} / {d}" : "";

            tileMatches.Value = n.ToString();

            tileKills.Value = Avg(m => m.Kills);
            tileDeaths.Value = Avg(m => m.Deaths);
            tileMvp.Value = n > 0 ? _session.Sum(m => m.Mvps).ToString() : "—";

            UpdateStatus();
            recentPanel.Invalidate();
        }

        private string Avg(Func<Cs2Match, int> sel)
            => _session.Count > 0 ? _session.Average(sel).ToString("0.0") : "—";

        private void DrawRecent(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int W = recentPanel.Width, H = recentPanel.Height;
            if (W < 40 || H < 40) return;

            using (var path = RoundRect(new Rectangle(0, 0, W - 1, H - 1), 18))
            {
                using (var bg = new LinearGradientBrush(new Rectangle(0, 0, W, H), Theme.CardTop, Theme.CardBottom, 90f))
                    g.FillPath(bg, path);
                using (var border = new Pen(Color.FromArgb(55, Theme.Accent), 1f))
                    g.DrawPath(border, path);
            }

            if (_session.Count == 0)
            {
                using var ef = new Font("Segoe UI", 11f);
                using var eb = new SolidBrush(Theme.TextMuted);
                using var esf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                // "No matches" while a filter is on would read as "nothing was tracked" — say
                // which it is.
                bool filtered = _mode.Length > 0;
                string msg = filtered
                    ? (Localization.IsPolish
                        ? $"Brak meczów w tej sesji w trybie {Cap(_mode)}"
                        : $"No {Cap(_mode)} matches this session")
                    : (Localization.IsPolish ? "Brak meczów w tej sesji" : "No matches this session");
                g.DrawString(msg, ef, eb, new RectangleF(0, 0, W, H), esf);
                return;
            }

            using var resFont = new Font("Segoe UI", 11f, FontStyle.Bold);
            using var statFont = new Font("Segoe UI", 10f);
            using var timeFont = new Font("Segoe UI", 9f);
            var win = Color.FromArgb(46, 204, 113);
            var loss = Color.FromArgb(230, 80, 90);
            var draw = Color.FromArgb(170, 170, 190);

            int pad = 18, rowH = 44, y = 14;
            foreach (var m in Enumerable.Reverse(_session).Take((H - 20) / rowH))
            {
                var rowRect = new Rectangle(pad, y, W - pad * 2, rowH - 8);
                var col = m.Draw ? draw : m.Won ? win : loss;
                string tag = m.Draw ? "D" : m.Won ? "W" : "L";

                var badge = new Rectangle(rowRect.X, rowRect.Y + 4, 52, rowRect.Height - 8);
                using (var bp = RoundRect(badge, 8))
                {
                    using (var bb = new SolidBrush(Color.FromArgb(42, col)))
                        g.FillPath(bb, bp);
                    using (var bpen = new Pen(col, 1.4f))
                        g.DrawPath(bpen, bp);
                }
                using (var tb = new SolidBrush(col))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(tag, resFont, tb, badge, sf);
                }

                using (var sb = new SolidBrush(Theme.TextPrimary))
                    g.DrawString($"{m.RoundsWon} : {m.RoundsLost}   {PrettyMap(m.Map)}", resFont, sb, badge.Right + 12, rowRect.Y + 3);
                using (var mb = new SolidBrush(Theme.TextSecondary))
                    g.DrawString($"K {m.Kills}   D {m.Deaths}   A {m.Assists}   •   MVP {m.Mvps}",
                        statFont, mb, badge.Right + 12, rowRect.Y + 22);

                using (var tmb = new SolidBrush(Theme.TextMuted))
                {
                    string t = m.Time.ToString("HH:mm");
                    var sz = g.MeasureString(t, timeFont);
                    g.DrawString(t, timeFont, tmb, rowRect.Right - sz.Width, rowRect.Y + 10);
                }

                y += rowH;
                if (y + rowH > H) break;
            }
        }

        private static string Cap(string s)
            => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

        // "de_dust2" -> "Dust2"
        private static string PrettyMap(string map)
        {
            if (string.IsNullOrEmpty(map)) return "";
            var s = map.StartsWith("de_") || map.StartsWith("cs_") ? map.Substring(3) : map;
            return s.Length == 0 ? map : char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

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
