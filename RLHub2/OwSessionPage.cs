using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using RLHub2.Controls;
using RLHub2.Helpers;
using RLHub2.Models;
using RLHub2.Services;

namespace RLHub2
{
    // Manual Overwatch 2 session tracker + screen-OCR rank read. OW2 has no live match feed, so
    // results are logged by hand and the rank is read from a screenshot the user confirms. Both are
    // always-available and independent of Blizzard's (often unavailable) public API.
    public class OwSessionPage : Controls.ArenaControl
    {
        protected override string ArenaFile => "ow_bg.png"; // absent -> flat accent gradient

        private readonly OwSessionStore _store = new();
        private readonly OwRankStore _rankStore = new();
        private readonly SettingsStore _settings = new();

        private readonly Panel _content;
        private readonly SegmentedControl _role;
        private readonly FlowLayoutPanel _statRow;
        private readonly FlowLayoutPanel _recent;
        private readonly Label _recentLabel;
        private readonly Label _rankLabel;
        private readonly Label _readStatus;
        private readonly Button _btnRead;
        private readonly Button _btnReadMatch;
        private readonly Button _btnOverlay;
        private readonly Label _chartHdr;
        private readonly OwTrendChart _chart;
        private readonly Button _btnUndo;
        private readonly Button _btnNew;

        public OwSessionPage()
        {
            Padding = new Padding(28, 22, 28, 22);
            _content = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Transparent };

            var title = new Label
            {
                Text = Localization.IsPolish ? "Sesja — Overwatch 2" : "Session — Overwatch 2",
                AutoSize = true, Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary, Location = new Point(0, 0),
            };
            var subtitle = new Label
            {
                AutoSize = true, MaximumSize = new Size(900, 0), ForeColor = Theme.TextMuted,
                Font = new Font("Segoe UI", 9.5f), Location = new Point(2, 38),
                Text = Localization.IsPolish
                    ? "Overwatch 2 nie udostępnia meczów na żywo — zapisuj wynik po każdym meczu, a policzę bilans i passę."
                    : "Overwatch 2 has no live match feed — log each result and the record and streak are tracked for you.",
            };

            // ---- rank (screen OCR, confirmed) ----
            var lblRankHdr = new Label
            {
                Text = Localization.IsPolish ? "RANGA" : "RANK", AutoSize = true,
                ForeColor = Theme.TextSecondary, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(0, 70),
            };
            _rankLabel = new Label
            {
                AutoSize = true, ForeColor = Theme.TextPrimary, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Location = new Point(0, 92),
            };
            _btnRead = Small(Localization.IsPolish ? "ODCZYT RANGI" : "READ RANK", Theme.Accent, Color.Black, new Point(0, 122));
            _btnRead.Size = new Size(150, 32);
            _btnRead.Click += async (s, e) => await ReadRankAsync();
            _btnReadMatch = Small(Localization.IsPolish ? "ODCZYT MECZU" : "READ MATCH", Theme.Surface, Theme.TextPrimary, new Point(160, 122));
            _btnReadMatch.Size = new Size(150, 32);
            _btnReadMatch.Click += async (s, e) => await ReadMatchAsync();
            _btnOverlay = Small(Localization.IsPolish ? "NAKŁADKA" : "OVERLAY", Theme.Surface, Theme.TextPrimary, new Point(320, 122));
            _btnOverlay.Size = new Size(130, 32);
            _btnOverlay.Click += (s, e) => OwOverlayWindow.Toggle();
            _readStatus = new Label
            {
                AutoSize = true, ForeColor = Theme.TextMuted, Font = new Font("Segoe UI", 9.5f),
                Location = new Point(462, 130),
            };

            // ---- log a result ----
            var lblRole = new Label
            {
                Text = Localization.IsPolish ? "Rola (opcjonalnie)" : "Role (optional)", AutoSize = true,
                ForeColor = Theme.TextSecondary, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(0, 172),
            };
            _role = new SegmentedControl { Location = new Point(0, 194), Size = new Size(360, 40) };
            _role.SetOptions(new[] { Localization.IsPolish ? "Dowolna" : "Any", "Tank", "DPS", "Support" });
            _role.SetSelectedSilent(0);

            var btnWin = Big(Localization.IsPolish ? "WYGRANA" : "WIN", Color.FromArgb(46, 204, 113), new Point(0, 248));
            btnWin.Click += (s, e) => Log("W");
            var btnDraw = Big(Localization.IsPolish ? "REMIS" : "DRAW", Color.FromArgb(150, 150, 160), new Point(196, 248));
            btnDraw.Click += (s, e) => Log("D");
            var btnLoss = Big(Localization.IsPolish ? "PRZEGRANA" : "LOSS", Color.FromArgb(230, 90, 90), new Point(392, 248));
            btnLoss.Click += (s, e) => Log("L");

            _statRow = new FlowLayoutPanel
            {
                Location = new Point(0, 326), AutoSize = true, BackColor = Color.Transparent,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = true, MaximumSize = new Size(980, 0),
            };

            _recentLabel = new Label
            {
                Text = Localization.IsPolish ? "Ostatnie mecze" : "Recent matches", AutoSize = true,
                ForeColor = Theme.TextSecondary, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(0, 440),
            };
            _recent = new FlowLayoutPanel
            {
                Location = new Point(0, 464), AutoSize = true, BackColor = Color.Transparent,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = true, MaximumSize = new Size(980, 0),
            };

            _chartHdr = new Label
            {
                Text = Localization.IsPolish ? "Trend sesji (bilans W−L)" : "Session trend (net W−L)", AutoSize = true,
                ForeColor = Theme.TextSecondary, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(0, 326),
            };
            _chart = new OwTrendChart { Location = new Point(0, 350), Size = new Size(720, 150) };

            _btnUndo = Small(Localization.IsPolish ? "COFNIJ" : "UNDO", Theme.Surface, Theme.TextPrimary, new Point(0, 530));
            _btnUndo.Click += (s, e) => { _store.RemoveLast(); Refresh2(); };
            _btnNew = Small(Localization.IsPolish ? "NOWA SESJA" : "NEW SESSION", Theme.Surface, Theme.TextPrimary, new Point(120, 530));
            _btnNew.Click += (s, e) =>
            {
                var msg = Localization.IsPolish
                    ? "Zacząć nową sesję? Historia zostaje, licznik sesji rusza od zera."
                    : "Start a new session? History is kept; the session counter resets.";
                if (MessageBox.Show(this, msg, "NexPlay", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
                { _store.NewSession(); Refresh2(); }
            };

            _content.Controls.AddRange(new Control[]
            {
                title, subtitle, lblRankHdr, _rankLabel, _btnRead, _btnReadMatch, _btnOverlay, _readStatus,
                lblRole, _role, btnWin, btnDraw, btnLoss, _statRow,
                _chartHdr, _chart, _recentLabel, _recent, _btnUndo, _btnNew,
            });
            Controls.Add(_content);

            RefreshRank();
            Refresh2();
        }

        private async Task ReadRankAsync()
        {
            bool pl = Localization.IsPolish;
            _btnRead.Enabled = false;
            try
            {
                for (int s = 4; s >= 1; s--)
                {
                    _readStatus.ForeColor = Theme.TextMuted;
                    _readStatus.Text = (pl ? "Przełącz na ekran rangi w OW… " : "Switch to the OW rank screen… ") + s;
                    await Task.Delay(1000);
                }
                _readStatus.Text = pl ? "Czytam ekran…" : "Reading screen…";

                Dictionary<string, string> ocr;
                try { ocr = await ScreenMmr.ReadOwRankAsync(); }
                catch { ocr = new(); }

                _readStatus.Text = "";
                using var dlg = new OwRankDialog(ocr);
                if (dlg.ShowDialog(FindForm()) == DialogResult.OK && dlg.Result.Any)
                {
                    _rankStore.Append(dlg.Result);
                    RefreshRank();
                }
            }
            finally { _btnRead.Enabled = true; }
        }

        private async Task ReadMatchAsync()
        {
            bool pl = Localization.IsPolish;
            var name = _settings.LoadOwBattleTag().Split('#', '-')[0].Trim();
            _btnRead.Enabled = false; _btnReadMatch.Enabled = false;
            try
            {
                for (int s = 4; s >= 1; s--)
                {
                    _readStatus.ForeColor = Theme.TextMuted;
                    _readStatus.Text = (pl ? "Przełącz na tablicę wyników… " : "Switch to the scoreboard… ") + s;
                    await Task.Delay(1000);
                }
                _readStatus.Text = pl ? "Czytam ekran…" : "Reading screen…";

                List<int> nums;
                try { nums = await ScreenMmr.ReadOwScoreboardAsync(name); }
                catch { nums = new List<int>(); }

                _readStatus.Text = "";
                if (nums.Count == 0 && !string.IsNullOrEmpty(name))
                    _readStatus.Text = pl
                        ? "Nie znalazłem Twojego wiersza — wpisz liczby ręcznie."
                        : "Couldn't find your row — enter the numbers by hand.";

                using var dlg = new OwMatchDialog(nums);
                if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
                {
                    _store.Append(dlg.Result);
                    Refresh2();
                }
            }
            finally { _btnRead.Enabled = true; _btnReadMatch.Enabled = true; }
        }

        private void RefreshRank()
        {
            var s = _rankStore.Latest();
            if (s == null || !s.Any)
            {
                _rankLabel.ForeColor = Theme.TextMuted;
                _rankLabel.Text = Localization.IsPolish ? "brak — kliknij „Odczyt rangi”" : "none — click “Read rank”";
                return;
            }
            var parts = new List<string>();
            if (s.Tank.Length > 0) parts.Add("Tank: " + s.Tank);
            if (s.Damage.Length > 0) parts.Add("DPS: " + s.Damage);
            if (s.Support.Length > 0) parts.Add("Support: " + s.Support);
            _rankLabel.ForeColor = Theme.TextPrimary;
            _rankLabel.Text = string.Join("    •    ", parts);
        }

        private void Log(string result)
        {
            string role = _role.SelectedIndex switch { 1 => "tank", 2 => "damage", 3 => "support", _ => "" };
            _store.Append(new OwMatch { Time = DateTime.Now, Result = result, Role = role });
            Refresh2();
        }

        private void Refresh2()
        {
            var data = _store.Load();
            var session = data.Matches.Where(m => m.Time >= data.SessionStart).ToList();

            int w = session.Count(m => m.Won);
            int l = session.Count(m => m.Lost);
            int d = session.Count(m => m.Draw);
            int decided = w + l;

            _statRow.Controls.Clear();
            _statRow.Controls.Add(Tile(Localization.IsPolish ? "BILANS" : "RECORD",
                d > 0 ? $"{w}–{l}–{d}" : $"{w}–{l}", Localization.IsPolish ? "sesja" : "session"));
            _statRow.Controls.Add(Tile("WINRATE",
                decided > 0 ? Math.Round(100.0 * w / decided) + "%" : "—", $"{session.Count} " + (Localization.IsPolish ? "meczów" : "games")));
            _statRow.Controls.Add(Tile(Localization.IsPolish ? "PASSA" : "STREAK", StreakText(session), ""));
            _statRow.Controls.Add(Tile(Localization.IsPolish ? "DZIŚ" : "TODAY",
                data.Matches.Count(m => m.Time.Date == DateTime.Now.Date).ToString(),
                Localization.IsPolish ? "meczów" : "games"));

            // Averages only appear once at least one match carries scoreboard stats.
            var withStats = session.Where(m => m.HasStats).ToList();
            if (withStats.Count > 0)
            {
                double dSum = withStats.Sum(m => m.Deaths);
                double kd = dSum > 0 ? withStats.Sum(m => m.Eliminations) / dSum : withStats.Sum(m => m.Eliminations);
                _statRow.Controls.Add(Tile("K/D", kd.ToString("0.0"),
                    $"{withStats.Count} " + (Localization.IsPolish ? "z ekranu" : "with stats")));
                _statRow.Controls.Add(Tile(Localization.IsPolish ? "ŚR. DMG" : "AVG DMG",
                    Math.Round(withStats.Average(m => m.Damage)).ToString("#,0"), Localization.IsPolish ? "na mecz" : "per game"));
                if (withStats.Any(m => m.Healing > 0))
                    _statRow.Controls.Add(Tile(Localization.IsPolish ? "ŚR. HEAL" : "AVG HEAL",
                        Math.Round(withStats.Average(m => m.Healing)).ToString("#,0"), Localization.IsPolish ? "na mecz" : "per game"));
            }

            _recent.Controls.Clear();
            foreach (var m in Enumerable.Reverse(session).Take(16))
                _recent.Controls.Add(Badge(m));
            _recentLabel.Visible = session.Count > 0;

            _chart.SetSession(session);
            LayoutLower();
        }

        // Stack the chart, recent list and buttons below the stat tiles — done in code because the
        // tile row's height varies (extra tiles appear once matches carry scoreboard stats), so a
        // fixed Y would collide.
        private void LayoutLower()
        {
            _statRow.PerformLayout();
            int y = _statRow.Bottom + 18;
            _chartHdr.Location = new Point(0, y); y += 24;
            _chart.Width = Math.Min(720, Math.Max(320, _content.ClientSize.Width - 40));
            _chart.Location = new Point(0, y); y += _chart.Height + 18;

            _recentLabel.Location = new Point(0, y); y += 24;
            _recent.Location = new Point(0, y);
            _recent.PerformLayout();
            int rb = _recent.Controls.Count > 0 ? _recent.Bottom : y;

            _btnUndo.Location = new Point(0, rb + 16);
            _btnNew.Location = new Point(120, rb + 16);
        }

        private static string StreakText(List<OwMatch> session)
        {
            var decided = session.Where(m => !m.Draw).ToList();
            if (decided.Count == 0) return "—";
            bool won = decided[^1].Won;
            int n = 0;
            for (int i = decided.Count - 1; i >= 0 && decided[i].Won == won; i--) n++;
            return (won ? "W" : (Localization.IsPolish ? "P" : "L")) + n;
        }

        private Control Badge(OwMatch m)
        {
            Color c = m.Won ? Color.FromArgb(46, 204, 113) : m.Lost ? Color.FromArgb(230, 90, 90) : Color.FromArgb(150, 150, 160);
            string letter = m.Won ? "W" : m.Lost ? (Localization.IsPolish ? "P" : "L") : (Localization.IsPolish ? "R" : "D");
            var lbl = new Label
            {
                Text = letter, Size = new Size(40, 40), Margin = new Padding(0, 0, 8, 8),
                TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.White, BackColor = c,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            };
            var tip = new ToolTip();
            string text = m.Time.ToString("HH:mm") + (string.IsNullOrEmpty(m.Role) ? "" : "  ·  " + m.Role);
            if (m.HasStats)
                text += $"\n{m.Eliminations}/{m.Assists}/{m.Deaths}  ·  {m.Damage} dmg  ·  {m.Healing} heal";
            tip.SetToolTip(lbl, text);
            return lbl;
        }

        private StatTile Tile(string title, string value, string subtitle) => new()
        {
            Title = title, Value = value, Subtitle = subtitle, Accent = Theme.Accent,
            Size = new Size(200, 96), Margin = new Padding(0, 0, 14, 14),
        };

        private static Button Big(string text, Color back, Point at)
        {
            var b = new Button
            {
                Text = text, Location = at, Size = new Size(180, 58), FlatStyle = FlatStyle.Flat,
                BackColor = back, ForeColor = Color.White, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Cursor = Cursors.Hand,
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private static Button Small(string text, Color back, Color fore, Point at)
        {
            var b = new Button
            {
                Text = text, Location = at, Size = new Size(112, 30), FlatStyle = FlatStyle.Flat,
                BackColor = back, ForeColor = fore, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
    }
}
