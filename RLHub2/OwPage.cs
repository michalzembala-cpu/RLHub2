using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using RLHub2.Controls;
using RLHub2.Helpers;
using RLHub2.Models;
using RLHub2.Services;

namespace RLHub2
{
    // Overwatch 2 dashboard. Reads a public career profile (OverFast) for the BattleTag saved in
    // this page, and shows identity, current rank per role and career aggregates. No match history
    // or "last match" — Blizzard does not publish that, so we deliberately do not pretend to.
    public class OwPage : Controls.ArenaControl
    {
        protected override string ArenaFile => "ow_bg.png"; // absent -> flat accent gradient

        private readonly SettingsStore _store = new();
        private readonly OverFastClient _client = new();

        private readonly Panel _content;
        private readonly Label _title;
        private readonly Label _status;
        private readonly PictureBox _avatar;
        private readonly Label _name;
        private readonly Label _sub;
        private readonly FlowLayoutPanel _rankRow;
        private readonly FlowLayoutPanel _statRow;
        private readonly Button _refresh;
        private readonly Button _editTag;

        // Inline BattleTag entry (shown when no tag is saved, or via "change").
        private readonly Panel _entry;
        private readonly TextBox _tagBox;
        private readonly SegmentedControl _platform;
        private readonly Button _go;

        public OwPage()
        {
            Padding = new Padding(28, 22, 28, 22);

            _content = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Transparent };

            _title = new Label
            {
                Text = "Overwatch 2",
                AutoSize = true,
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                Location = new Point(0, 0),
            };

            _avatar = new PictureBox
            {
                Size = new Size(76, 76),
                Location = new Point(0, 44),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
            };
            _name = new Label
            {
                AutoSize = true, Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary, Location = new Point(90, 50),
            };
            _sub = new Label
            {
                AutoSize = true, Font = new Font("Segoe UI", 10f),
                ForeColor = Theme.TextMuted, Location = new Point(92, 82),
            };

            _refresh = Flat(Localization.IsPolish ? "ODŚWIEŻ" : "REFRESH", Theme.Accent, Color.Black);
            _refresh.Click += async (s, e) => await LoadProfileAsync();
            _editTag = Flat(Localization.IsPolish ? "ZMIEŃ TAG" : "CHANGE TAG", Theme.Surface, Theme.TextPrimary);
            _editTag.Click += (s, e) => ShowEntry(true);

            _rankRow = Row(new Point(0, 140));
            _statRow = Row(new Point(0, 250));

            _status = new Label
            {
                AutoSize = true, Font = new Font("Segoe UI", 10.5f),
                ForeColor = Theme.TextMuted, Location = new Point(0, 140),
            };

            // ---- inline BattleTag entry ----
            _entry = new Panel { Location = new Point(0, 140), Size = new Size(560, 200), BackColor = Color.Transparent, Visible = false };
            var lblTag = new Label
            {
                Text = "BattleTag", AutoSize = true, ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(0, 0),
            };
            _tagBox = new TextBox
            {
                Location = new Point(0, 26), Size = new Size(260, 30),
                BackColor = Theme.Surface, ForeColor = Theme.TextPrimary, BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 11f), Text = "Nick#12345",
            };
            _go = Flat(Localization.IsPolish ? "POBIERZ" : "FETCH", Theme.Accent, Color.Black);
            _go.Location = new Point(276, 26);
            _go.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            _go.Click += async (s, e) => await SaveTagAndLoad();

            var lblPlatform = new Label
            {
                Text = Localization.IsPolish ? "Platforma" : "Platform", AutoSize = true, ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold), Location = new Point(0, 70),
            };
            _platform = new SegmentedControl { Location = new Point(0, 94), Size = new Size(220, 42) };
            _platform.SetOptions(new[] { "PC", Localization.IsPolish ? "Konsola" : "Console" });
            _platform.SetSelectedSilent(0);

            var hint = new Label
            {
                AutoSize = true, MaximumSize = new Size(540, 0), ForeColor = Theme.TextMuted,
                Font = new Font("Segoe UI", 9f), Location = new Point(0, 148),
                Text = Localization.IsPolish
                    ? "Wpisz swój BattleTag (np. Nick#21837). Profil kariery w Overwatch 2 musi być ustawiony na PUBLICZNY, inaczej Blizzard nie udostępnia statystyk."
                    : "Enter your BattleTag (e.g. Nick#21837). Your Overwatch 2 career profile must be set to PUBLIC, otherwise Blizzard won't share the stats.",
            };
            _entry.Controls.AddRange(new Control[] { lblTag, _tagBox, _go, lblPlatform, _platform, hint });

            _content.Controls.AddRange(new Control[]
            {
                _title, _avatar, _name, _sub, _refresh, _editTag, _rankRow, _statRow, _status, _entry,
            });
            Controls.Add(_content);

            InitialLoad();
        }

        private async void InitialLoad() => await LoadProfileAsync();

        private void ShowEntry(bool on)
        {
            _entry.Visible = on;
            _avatar.Visible = !on;
            _name.Visible = !on;
            _sub.Visible = !on;
            _rankRow.Visible = !on;
            _statRow.Visible = !on;
            _refresh.Visible = !on;
            _editTag.Visible = !on;
            _status.Visible = !on;
            if (on)
            {
                var tag = _store.LoadOwBattleTag();
                if (!string.IsNullOrWhiteSpace(tag)) _tagBox.Text = tag;
                _platform.SetSelectedSilent(_store.LoadOwPlatform() == "console" ? 1 : 0);
            }
        }

        private async Task SaveTagAndLoad()
        {
            var tag = _tagBox.Text.Trim();
            if (!tag.Contains('#') && !tag.Contains('-'))
            {
                MessageBox.Show(this,
                    Localization.IsPolish ? "BattleTag ma format Nick#12345." : "BattleTag looks like Nick#12345.",
                    "NexPlay", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            _store.SaveOwBattleTag(tag);
            _store.SaveOwPlatform(_platform.SelectedIndex == 1 ? "console" : "pc");
            ShowEntry(false);
            await LoadProfileAsync();
        }

        private async Task LoadProfileAsync()
        {
            var tag = _store.LoadOwBattleTag();
            if (string.IsNullOrWhiteSpace(tag)) { ShowEntry(true); return; }

            PositionActionButtons();
            _rankRow.Controls.Clear();
            _statRow.Controls.Clear();
            _name.Text = "";
            _sub.Text = "";
            _avatar.Image = null;
            _status.Visible = true;
            _status.ForeColor = Theme.TextMuted;
            _status.Text = (Localization.IsPolish ? "Pobieram profil " : "Fetching profile ") + tag + "…";
            _refresh.Enabled = false;

            OwProfile p;
            try { p = await _client.FetchAsync(tag, _store.LoadOwPlatform()); }
            finally { _refresh.Enabled = true; }

            if (!p.Found || p.Error != null)
            {
                _status.ForeColor = Color.FromArgb(230, 90, 90);
                _status.Text = p.Error switch
                {
                    "not-found" => Localization.IsPolish
                        ? $"Nie znaleziono profilu „{tag}”. Albo BattleTag jest inny (sprawdź wielkość liter i numer), albo — częściej — profil kariery jest PRYWATNY. Ustaw go na publiczny: Opcje → Społeczność → Widoczność profilu kariery → Publiczny."
                        : $"Profile \"{tag}\" not found. Either the BattleTag differs (check case and number), or — more often — your career profile is PRIVATE. Set it public: Options → Social → Career Profile Visibility → Public.",
                    "indexing" => Localization.IsPolish
                        ? "Profil jest już publiczny, ale Blizzard/OverFast wciąż go indeksuje. Odczekaj ~10 min (pomaga rozegranie meczu) i kliknij ODŚWIEŻ."
                        : "Profile is public, but Blizzard/OverFast is still indexing it. Wait ~10 min (playing a match helps) and click REFRESH.",
                    "rate-limited" => Localization.IsPolish
                        ? "Za dużo zapytań do API — spróbuj za chwilę."
                        : "API rate limit hit — try again shortly.",
                    "no-tag" => Localization.IsPolish ? "Wpisz BattleTag." : "Enter a BattleTag.",
                    null => Localization.IsPolish ? "Nie udało się pobrać profilu." : "Couldn't load the profile.",
                    _ => (Localization.IsPolish ? "Błąd sieci: " : "Network error: ") + p.Error,
                };
                return;
            }

            if (p.Private)
            {
                _status.ForeColor = Color.FromArgb(222, 150, 40);
                _status.Text = Localization.IsPolish
                    ? "Profil istnieje, ale kariera jest PRYWATNA. Ustaw profil na publiczny w Overwatch 2 (Opcje → Społeczność → Widoczność profilu kariery: Wszyscy)."
                    : "Profile exists, but the career is PRIVATE. Set it to public in Overwatch 2 (Options → Social → Career Profile Visibility: Everyone).";
                return;
            }

            _status.Visible = false;
            _name.Text = string.IsNullOrEmpty(p.Username) ? tag : p.Username;
            var bits = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrEmpty(p.Title)) bits.Add(p.Title);
            if (p.Endorsement > 0) bits.Add((Localization.IsPolish ? "Endorsement " : "Endorsement ") + p.Endorsement);
            bits.Add(_store.LoadOwPlatform() == "console" ? "Konsola" : "PC");
            _sub.Text = string.Join("   •   ", bits);

            if (!string.IsNullOrEmpty(p.AvatarUrl))
            {
                var bytes = await _client.DownloadAvatarAsync(p.AvatarUrl);
                if (bytes != null && !IsDisposed)
                    try { _avatar.Image = Image.FromStream(new MemoryStream(bytes)); } catch { }
            }

            BuildRanks(p);
            BuildStats(p);
        }

        private void BuildRanks(OwProfile p)
        {
            _rankRow.Controls.Clear();
            (string role, string labelPl, string labelEn)[] roles =
            {
                ("tank", "TANK", "TANK"),
                ("damage", "DPS", "DPS"),
                ("support", "SUPPORT", "SUPPORT"),
            };
            foreach (var (role, pl, en) in roles)
            {
                var rank = p.Rank(role);
                var t = Tile(Localization.IsPolish ? pl : en,
                    rank?.Display ?? "—",
                    rank == null ? (Localization.IsPolish ? "brak rangi" : "unranked") : "");
                _rankRow.Controls.Add(t);
            }
        }

        private void BuildStats(OwProfile p)
        {
            _statRow.Controls.Clear();
            var g = p.General;
            if (g != null)
            {
                _statRow.Controls.Add(Tile(Localization.IsPolish ? "WINRATE" : "WIN RATE",
                    Math.Round(g.Winrate) + "%", $"{g.GamesWon}–{g.GamesLost}"));
                _statRow.Controls.Add(Tile(Localization.IsPolish ? "MECZE" : "GAMES",
                    g.GamesPlayed.ToString(), Localization.IsPolish ? "rankingowe" : "competitive"));
                _statRow.Controls.Add(Tile("KDA", g.Kda.ToString("0.00"), ""));
                _statRow.Controls.Add(Tile(Localization.IsPolish ? "CZAS GRY" : "TIME PLAYED",
                    g.TimePlayedText, ""));
            }
            var hero = p.MostPlayed;
            if (hero != null)
                _statRow.Controls.Add(Tile(Localization.IsPolish ? "NAJCZĘŚCIEJ GRANY" : "MOST PLAYED",
                    hero.Name, hero.TimePlayedText + $" · {Math.Round(hero.Winrate)}% WR"));
        }

        private StatTile Tile(string title, string value, string subtitle) => new()
        {
            Title = title,
            Value = value,
            Subtitle = subtitle,
            Accent = Theme.Accent,
            Size = new Size(200, 96),
            Margin = new Padding(0, 0, 14, 14),
        };

        private FlowLayoutPanel Row(Point at) => new()
        {
            Location = at, AutoSize = true, BackColor = Color.Transparent,
            FlowDirection = FlowDirection.LeftToRight, WrapContents = true, MaximumSize = new Size(980, 0),
        };

        private void PositionActionButtons()
        {
            _refresh.Location = new Point(_content.ClientSize.Width - 320, 4);
            _editTag.Location = new Point(_content.ClientSize.Width - 200, 4);
        }

        private static Button Flat(string text, Color back, Color fore)
        {
            var b = new Button
            {
                Text = text, Size = new Size(110, 30), FlatStyle = FlatStyle.Flat,
                BackColor = back, ForeColor = fore, Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand, Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
    }
}
