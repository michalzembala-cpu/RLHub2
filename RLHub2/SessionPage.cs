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
    public partial class SessionPage : Controls.ArenaControl
    {
        protected override string ArenaFile => "rl_bg.png";

        private readonly SessionStore _store = new();
        private readonly StatsApiClient _client = StatsApiClient.Instance;
        private List<SessionMatch> _session = new();
        private Button btnMode = null!;

        public SessionPage()
        {
            InitializeComponent();
            ApplyLanguage();

            recentPanel.Paint += DrawRecent;
            recentPanel.Resize += (s, e) => recentPanel.Invalidate();
            recentPanel.MouseClick += RecentClicked;

            btnMode = new Button
            {
                Size = new Size(150, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
            };
            btnMode.FlatAppearance.BorderSize = 1;
            btnMode.FlatAppearance.BorderColor = Color.FromArgb(90, 90, 120);
            btnMode.Click += (s, e) => ShowOverrideMenu();
            recentBar.Controls.Add(btnMode);
            recentBar.Resize += (s, e) =>
                btnMode.Location = new Point(btnOverlay.Left - btnMode.Width - 8, 2);
            UpdateModeButton();
            btnReset.Click += (s, e) => ResetSession();
            btnOverlay.Click += (s, e) => OverlayWindow.Toggle();

            _client.ConnectionChanged += OnConnectionChanged;
            _client.MatchLogged += OnMatchLogged;
            HandleDestroyed += (s, e) =>
            {
                _client.ConnectionChanged -= OnConnectionChanged;
                _client.MatchLogged -= OnMatchLogged;
            };

            Load += (s, e) =>
            {
                _client.Start(); // idempotent — also started at app launch (UI thread → captures sync context)
                UpdateStatus(_client.IsConnected);
                RefreshStats();
            };
        }

        private void ApplyLanguage()
        {
            lblTitle.Text = Localization.T("session_title");
            lblRecent.Text = Localization.T("session_recent");
            btnReset.Text = Localization.T("session_reset");
            tileRecord.Title = Localization.T("session_record");
            tileWinRate.Title = Localization.T("session_winrate");
            tileStreak.Title = Localization.T("session_streak");
            tileMatches.Title = Localization.T("session_matches");
            tileGoals.Title = Localization.T("session_avg_goals");
            tileSaves.Title = Localization.T("session_avg_saves");
            tileAssists.Title = Localization.T("session_avg_assists");
        }

        private void OnConnectionChanged(bool connected) => UpdateStatus(connected);

        private void OnMatchLogged(SessionMatch m)
        {
            RefreshStats();
            Toast.Show(this,
                $"{Localization.T(m.Won ? "session_win" : "session_loss")}  {m.TeamGoals}:{m.OppGoals}",
                m.Won ? ToastKind.Success : ToastKind.Info, 3500);
        }

        private void UpdateStatus(bool connected)
        {
            lblStatus.Text = connected
                ? "🟢  " + Localization.T("session_status_on")
                : "⚪  " + Localization.T("session_status_off");
            lblStatus.ForeColor = connected ? Color.FromArgb(46, 204, 113) : Theme.TextMuted;
        }

        private void ResetSession()
        {
            _store.Clear();
            RefreshStats();
        }

        private void RefreshStats()
        {
            _session = _store.LoadForActive()
                .Where(m => m.Time >= _client.StartedAt)
                .OrderBy(m => m.Time)
                .ToList();

            int wins = _session.Count(m => m.Won);
            int losses = _session.Count - wins;
            int n = _session.Count;

            tileRecord.Value = $"{wins} – {losses}";
            tileRecord.Subtitle = n > 0 ? $"{n} {Localization.T("session_matches")}" : "";

            tileWinRate.Value = n > 0 ? $"{(int)Math.Round(100.0 * wins / n)}%" : "—";
            tileWinRate.Subtitle = "";

            tileStreak.Value = StreakText();
            tileStreak.Subtitle = "";

            tileMatches.Value = n.ToString();
            tileMatches.Subtitle = "";

            tileGoals.Value = Avg(m => m.Goals);
            tileSaves.Value = Avg(m => m.Saves);
            tileAssists.Value = Avg(m => m.Assists);

            recentPanel.Invalidate();
        }

        private string Avg(Func<SessionMatch, int> sel)
            => _session.Count > 0 ? _session.Average(sel).ToString("0.0") : "—";

        private string StreakText()
        {
            if (_session.Count == 0) return "—";
            bool last = _session[^1].Won;
            int c = 0;
            for (int i = _session.Count - 1; i >= 0 && _session[i].Won == last; i--) c++;
            return $"{c}{(last ? "W" : "L")}";
        }

        private void DrawRecent(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int W = recentPanel.Width, H = recentPanel.Height;
            if (W < 40 || H < 40) return;

            // glass panel backdrop (matches the cards)
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
                g.DrawString(Localization.T("session_empty_short"), ef, eb, new RectangleF(0, 0, W, H), esf);
                return;
            }

            using var resFont = new Font("Segoe UI", 11f, FontStyle.Bold);
            using var statFont = new Font("Segoe UI", 10f);
            using var timeFont = new Font("Segoe UI", 9f);
            var win = Color.FromArgb(46, 204, 113);
            var loss = Color.FromArgb(230, 80, 90);

            int pad = 18, rowH = 44, y = 14;
            _rows.Clear();
            // newest first
            foreach (var m in Enumerable.Reverse(_session).Take((H - 20) / rowH))
            {
                var rowRect = new Rectangle(pad, y, W - pad * 2, rowH - 8);

                // W/L badge
                var badge = new Rectangle(rowRect.X, rowRect.Y + 4, 52, rowRect.Height - 8);
                using (var bp = RoundRect(badge, 8))
                {
                    using (var bb = new SolidBrush(Color.FromArgb(m.Won ? 42 : 40, m.Won ? win : loss)))
                        g.FillPath(bb, bp);
                    using (var bpen = new Pen(m.Won ? win : loss, 1.4f))
                        g.DrawPath(bpen, bp);
                }
                using (var tb = new SolidBrush(m.Won ? win : loss))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(m.Won ? "W" : "L", resFont, tb, badge, sf);
                }

                // score + stats
                using (var sb = new SolidBrush(Theme.TextPrimary))
                    g.DrawString($"{m.TeamGoals} : {m.OppGoals}", resFont, sb, badge.Right + 12, rowRect.Y + 3);
                using (var mb = new SolidBrush(Theme.TextSecondary))
                    g.DrawString($"G {m.Goals}   S {m.Saves}   A {m.Assists}   •   {m.Score} pts",
                        statFont, mb, badge.Right + 12, rowRect.Y + 22);

                // playlist, right of the score — clicking the row corrects it
                using (var pb = new SolidBrush(Theme.AccentSoft))
                {
                    string mode = string.IsNullOrEmpty(m.Mode) ? "—" : m.Mode;
                    g.DrawString(mode, statFont, pb, badge.Right + 120, rowRect.Y + 4);
                }

                // time (right aligned)
                using (var tmb = new SolidBrush(Theme.TextMuted))
                {
                    string t = m.Time.ToString("HH:mm");
                    var sz = g.MeasureString(t, timeFont);
                    g.DrawString(t, timeFont, tmb, rowRect.Right - sz.Width, rowRect.Y + 10);
                }

                _rows.Add((rowRect, m));
                y += rowH;
                if (y + rowH > H) break;
            }
        }

        // Playlists you can pick by hand. Everything except Rumble is detected automatically —
        // Rumble is here because nothing in the feed identifies it.
        private static readonly string[] PickableModes =
            { "1v1", "2v2", "3v3", "Rumble", "Hoops", "Dropshot", "Snow Day" };

        private readonly List<(Rectangle Rect, SessionMatch Match)> _rows = new();

        // Click a match to correct the playlist it was filed under; the MMR point moves with it.
        private void RecentClicked(object? sender, MouseEventArgs e)
        {
            var hit = _rows.FirstOrDefault(r => r.Rect.Contains(e.Location));
            if (hit.Match == null) return;

            var menu = new ContextMenuStrip();
            foreach (var mode in PickableModes)
            {
                var item = new ToolStripMenuItem(mode) { Checked = hit.Match.Mode == mode };
                item.Click += (s, _) => Retag(hit.Match, mode);
                menu.Items.Add(item);
            }
            menu.Show(recentPanel, e.Location);
        }

        private void Retag(SessionMatch match, string newMode)
        {
            string oldMode = match.Mode;
            if (oldMode == newMode) return;

            var all = _store.Load();
            var stored = all.FirstOrDefault(m => Math.Abs((m.Time - match.Time).TotalSeconds) < 2
                                                 && m.Account == match.Account);
            if (stored == null) return;

            stored.Mode = newMode;
            _store.Save(all);
            MmrTracker.Retag(match.Account, match.Time, oldMode, newMode);

            RefreshStats();
            Toast.Show(this,
                Localization.IsPolish ? $"Zmieniono tryb na {newMode}" : $"Mode changed to {newMode}",
                ToastKind.Success);
        }

        // Forces the playlist for matches the feed can't identify, until switched back.
        private void ShowOverrideMenu()
        {
            var store = new SettingsStore();
            string current = store.LoadRlModeOverride();

            var menu = new ContextMenuStrip();
            var auto = new ToolStripMenuItem(Localization.IsPolish ? "Automatycznie" : "Automatic")
            { Checked = current.Length == 0 };
            auto.Click += (s, _) => { store.SaveRlModeOverride(""); UpdateModeButton(); };
            menu.Items.Add(auto);
            menu.Items.Add(new ToolStripSeparator());

            foreach (var mode in new[] { "Rumble", "Hoops", "Dropshot", "Snow Day" })
            {
                var item = new ToolStripMenuItem(mode) { Checked = current == mode };
                item.Click += (s, _) => { store.SaveRlModeOverride(mode); UpdateModeButton(); };
                menu.Items.Add(item);
            }
            menu.Show(btnMode, new Point(0, btnMode.Height));
        }

        private void UpdateModeButton()
        {
            string m = new SettingsStore().LoadRlModeOverride();
            btnMode.Text = m.Length == 0
                ? (Localization.IsPolish ? "TRYB: AUTO" : "MODE: AUTO")
                : "TRYB: " + m.ToUpperInvariant();
            btnMode.ForeColor = m.Length == 0 ? Theme.TextSecondary : Theme.Accent;
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
