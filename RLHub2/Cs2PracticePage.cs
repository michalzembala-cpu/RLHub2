using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using RLHub2.Helpers;

namespace RLHub2
{
    // Practice console commands — the sv_cheats set everyone uses to drill nades, aim and
    // movement on their own server.
    //
    // Listed one per row with what it does, because copying a 16-command block when you wanted
    // a single command is useless; the section header still copies the whole set for the initial
    // server setup.
    //
    // These only do anything on a server you host (a private lobby / local practice map).
    // Official matchmaking refuses sv_cheats server-side, so this is a practice-config helper,
    // not something that affects real matches. The page says so, because a page full of
    // "cheat" commands with no context invites the wrong assumption.
    public class Cs2PracticePage : UserControl
    {
        private sealed class Cmd
        {
            public string Text = "";
            public string Desc = "";
            public Cmd(string text, string desc) { Text = text; Desc = desc; }
        }

        private sealed class Section
        {
            public string Name = "";
            public string Hint = "";
            public List<Cmd> Commands = new();
        }

        private static List<Section> Build()
        {
            bool pl = Localization.IsPolish;
            return new List<Section>
            {
                new()
                {
                    Name = pl ? "Serwer treningowy" : "Practice server",
                    Hint = pl ? "wklej raz na starcie" : "paste once at the start",
                    Commands = new()
                    {
                        new("sv_cheats 1",            pl ? "włącza komendy treningowe" : "enables practice commands"),
                        new("mp_limitteams 0",        pl ? "bez limitu graczy w drużynie" : "no team size limit"),
                        new("mp_autoteambalance 0",   pl ? "bez automatycznego balansu drużyn" : "no auto team balance"),
                        new("mp_roundtime 60",        pl ? "runda trwa 60 minut" : "60-minute rounds"),
                        new("mp_roundtime_defuse 60", pl ? "runda z bombą trwa 60 minut" : "60-minute defuse rounds"),
                        new("mp_freezetime 0",        pl ? "brak zamrożenia na starcie rundy" : "no freeze time"),
                        new("mp_maxmoney 60000",      pl ? "maksymalna kasa" : "max money cap"),
                        new("mp_startmoney 60000",    pl ? "kasa na start rundy" : "money at round start"),
                        new("mp_buytime 9999",        pl ? "kupowanie bez limitu czasu" : "unlimited buy time"),
                        new("mp_buy_anywhere 1",      pl ? "kupowanie w dowolnym miejscu mapy" : "buy anywhere on the map"),
                        new("mp_warmup_end",          pl ? "kończy rozgrzewkę" : "ends warmup"),
                        new("mp_restartgame 1",       pl ? "restartuje grę po sekundzie" : "restarts the game after 1s"),
                    },
                },
                new()
                {
                    Name = pl ? "Granaty" : "Grenades",
                    Hint = pl ? "lineupy i powtarzanie rzutu" : "lineups and rethrowing",
                    Commands = new()
                    {
                        new("sv_grenade_trajectory_prac_pipreview 1", pl ? "pokazuje tor lotu granatu" : "shows the grenade trajectory"),
                        new("sv_grenade_trajectory_time 8",           pl ? "jak długo widać tor lotu" : "how long the trajectory stays"),
                        new("sv_rethrow_last_grenade",                pl ? "powtarza ostatni rzut z tego samego miejsca" : "rethrows your last grenade"),
                        new("ammo_grenade_limit_total 5",             pl ? "pozwala nieść 5 granatów naraz" : "carry 5 grenades at once"),
                        new("sv_infinite_ammo 1",                     pl ? "nieskończone granaty i amunicja" : "infinite grenades and ammo"),
                    },
                },
                new()
                {
                    Name = pl ? "Aim / strzelanie" : "Aim",
                    Hint = pl ? "podgląd trafień i broń pod ręką" : "impacts and instant weapons",
                    Commands = new()
                    {
                        new("sv_showimpacts 1",      pl ? "pokazuje, gdzie trafiają pociski" : "shows where bullets land"),
                        new("sv_showimpacts_time 8", pl ? "jak długo widać trafienia" : "how long impacts stay"),
                        new("mp_free_armor 2",       pl ? "darmowa kamizelka i hełm" : "free armour and helmet"),
                        new("give weapon_ak47",      pl ? "daje AK-47" : "gives an AK-47"),
                        new("give weapon_awp",       pl ? "daje AWP" : "gives an AWP"),
                    },
                },
                new()
                {
                    Name = pl ? "Boty" : "Bots",
                    Hint = pl ? "do ćwiczenia celowania i pozycji" : "for aim and position drills",
                    Commands = new()
                    {
                        new("bot_kick",         pl ? "wyrzuca wszystkie boty" : "removes all bots"),
                        new("bot_add_t",        pl ? "dodaje bota do terrorystów" : "adds a bot to T"),
                        new("bot_add_ct",       pl ? "dodaje bota do antyterrorystów" : "adds a bot to CT"),
                        new("bot_stop 1",       pl ? "boty przestają walczyć" : "bots stop fighting"),
                        new("bot_dontmove 1",   pl ? "boty stoją w miejscu" : "bots stand still"),
                        new("bot_difficulty 3", pl ? "poziom trudności botów (0–3)" : "bot difficulty (0-3)"),
                        new("bot_place",        pl ? "stawia bota tam, gdzie patrzysz" : "places a bot where you look"),
                    },
                },
                new()
                {
                    Name = pl ? "Ruch" : "Movement",
                    Hint = pl ? "poruszanie się po mapie bez przeszkód" : "getting around the map",
                    Commands = new()
                    {
                        new("noclip",       pl ? "przenikanie przez ściany i latanie" : "fly through walls"),
                        new("god",          pl ? "nieśmiertelność" : "invulnerability"),
                        new("cl_showpos 1", pl ? "pokazuje pozycję i prędkość" : "shows position and speed"),
                    },
                },
                new()
                {
                    Name = pl ? "Bindy" : "Binds",
                    Hint = pl ? "jedno kliknięcie zamiast wpisywania" : "one key instead of typing",
                    Commands = new()
                    {
                        new("bind \"n\" \"noclip\"",                   pl ? "N — przenikanie" : "N - noclip"),
                        new("bind \"h\" \"sv_rethrow_last_grenade\"",  pl ? "H — powtórz rzut" : "H - rethrow grenade"),
                        new("bind \"j\" \"give weapon_flashbang\"",    pl ? "J — flash" : "J - flashbang"),
                        new("bind \"k\" \"give weapon_smokegrenade\"", pl ? "K — smoke" : "K - smoke"),
                        new("bind \"l\" \"give weapon_molotov\"",      pl ? "L — molotov" : "L - molotov"),
                        new("bind \"o\" \"mp_restartgame 1\"",         pl ? "O — restart rundy" : "O - restart round"),
                    },
                },
            };
        }

        private readonly List<Section> _sections = Build();

        private Label _title = null!, _hint = null!;
        private FlowLayoutPanel _tiles = null!;   // category grid (first level)
        private Panel _detail = null!;            // one category's commands (second level)
        private FlowLayoutPanel _rows = null!;
        private Label _detailTitle = null!;
        private Section? _current;

        public Cs2PracticePage()
        {
            BackColor = Theme.PageBg;
            Dock = DockStyle.Fill;

            _title = new Label
            {
                Text = Localization.IsPolish ? "TRENING" : "PRACTICE",
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(24, 8, 0, 0),
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
            };
            _hint = new Label
            {
                Dock = DockStyle.Top,
                Height = 26,
                Padding = new Padding(24, 0, 0, 0),
                ForeColor = Theme.TextMuted,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            };

            // ===== level 1: category tiles =====
            _tiles = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 8, 20, 20),
                AutoScroll = true,
                BackColor = Theme.PageBg,
            };
            foreach (var sec in _sections)
            {
                var s = sec;
                var tile = new SectionTile(s.Name, s.Hint, s.Commands.Count);
                tile.Click += (o, e) => ShowSection(s);
                _tiles.Controls.Add(tile);
            }

            // ===== level 2: the chosen category's commands =====
            _rows = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 8, 20, 20),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Theme.PageBg,
            };
            _rows.Resize += (s, e) => FitRows();

            var bar = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Theme.PageBg };
            var back = new Button
            {
                Text = Localization.IsPolish ? "←  WSTECZ" : "←  BACK",
                Size = new Size(112, 28),
                Location = new Point(20, 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
            };
            back.FlatAppearance.BorderSize = 1;
            back.FlatAppearance.BorderColor = Color.FromArgb(90, Theme.Accent);
            back.Click += (s, e) => ShowTiles();

            _detailTitle = new Label
            {
                AutoSize = true,
                Location = new Point(146, 12),
                ForeColor = Theme.AccentSoft,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            };

            var copyAll = new Button
            {
                Text = Localization.IsPolish ? "KOPIUJ CAŁOŚĆ" : "COPY ALL",
                Size = new Size(130, 28),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
            };
            copyAll.FlatAppearance.BorderSize = 1;
            copyAll.FlatAppearance.BorderColor = Color.FromArgb(90, Theme.Accent);
            copyAll.Click += (s, e) =>
            {
                if (_current == null) return;
                CopyToClipboard(this, string.Join("\n", _current.Commands.Select(c => c.Text)), _current.Name);
            };
            bar.Resize += (s, e) => copyAll.Location = new Point(bar.Width - copyAll.Width - 20, 8);

            bar.Controls.Add(back);
            bar.Controls.Add(_detailTitle);
            bar.Controls.Add(copyAll);

            _detail = new Panel { Dock = DockStyle.Fill, BackColor = Theme.PageBg, Visible = false };
            _detail.Controls.Add(_rows);
            _detail.Controls.Add(bar);

            Controls.Add(_detail);
            Controls.Add(_tiles);
            Controls.Add(_hint);
            Controls.Add(_title);

            ShowTiles();
        }

        private void ShowTiles()
        {
            _current = null;
            _detail.Visible = false;
            _tiles.Visible = true;
            _title.Text = Localization.IsPolish ? "TRENING" : "PRACTICE";
            _hint.Text = Localization.IsPolish
                ? "Działa tylko na własnym serwerze (sv_cheats). Wybierz kategorię."
                : "Only works on your own server (sv_cheats). Pick a category.";
        }

        private void ShowSection(Section sec)
        {
            _current = sec;
            _detailTitle.Text = sec.Name;

            _rows.SuspendLayout();
            foreach (Control c in _rows.Controls) c.Dispose();
            _rows.Controls.Clear();
            foreach (var c in sec.Commands)
                _rows.Controls.Add(new CommandRow(c.Text, c.Desc));
            _rows.ResumeLayout();
            FitRows();

            _tiles.Visible = false;
            _detail.Visible = true;
            _title.Text = Localization.IsPolish ? "TRENING" : "PRACTICE";
            _hint.Text = Localization.IsPolish
                ? "Kliknij komendę, by ją skopiować, potem wklej w konsoli (~)."
                : "Click a command to copy it, then paste into the console (~).";
        }

        // Rows span the page; the scrollbar appearing changes the usable width, so this runs
        // on every resize rather than being set once.
        private void FitRows()
        {
            int w = _rows.ClientSize.Width - _rows.Padding.Horizontal;
            if (w < 200) return;
            foreach (Control c in _rows.Controls)
                c.Width = w;
        }

        private static void CopyToClipboard(Control owner, string text, string what)
        {
            try
            {
                Clipboard.SetText(text);
                Toast.Show(owner.FindForm() is Control f ? f : owner,
                    (Localization.IsPolish ? "Skopiowano: " : "Copied: ") + what, ToastKind.Success);
            }
            catch
            {
                Toast.Show(owner, Localization.IsPolish ? "Nie udało się skopiować" : "Copy failed", ToastKind.Info);
            }
        }

        // ===== category tile (first level) =====
        private sealed class SectionTile : Panel
        {
            private readonly string _name, _hint;
            private readonly int _count;
            private bool _hover;

            public SectionTile(string name, string hint, int count)
            {
                _name = name; _hint = hint; _count = count;
                Size = new Size(300, 124);
                Margin = new Padding(0, 0, 16, 16);
                DoubleBuffered = true;
                BackColor = Color.Transparent;
                Cursor = Cursors.Hand;

                MouseEnter += (s, e) => { _hover = true; Invalidate(); };
                MouseLeave += (s, e) => { _hover = false; Invalidate(); };
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var rect = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var p = Round(rect, 16))
                {
                    using var bg = new LinearGradientBrush(rect, Theme.CardTop, Theme.CardBottom, 90f);
                    g.FillPath(bg, p);
                    using var pen = new Pen(Color.FromArgb(_hover ? 150 : 60, Theme.Accent), _hover ? 2f : 1f);
                    g.DrawPath(pen, p);
                }

                using var nameFont = new Font("Segoe UI", 15f, FontStyle.Bold);
                using var hintFont = new Font("Segoe UI", 9f);
                using var cntFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                using var chevFont = new Font("Segoe UI", 16f, FontStyle.Bold);
                using var nb = new SolidBrush(Theme.TextPrimary);
                using var hb = new SolidBrush(Theme.TextMuted);
                using var ab = new SolidBrush(Theme.Accent);

                g.DrawString(_name, nameFont, nb, 18, 20);
                g.DrawString(_hint, hintFont, hb, 19, 50);
                g.DrawString($"{_count} {(Localization.IsPolish ? "komend" : "commands")}", cntFont, ab, 19, 88);
                g.DrawString("›", chevFont, hb, Width - 32, Height / 2 - 18);
            }

            // The tile is the button — clicking the label area must count as clicking it.
            protected override void OnMouseClick(MouseEventArgs e)
            {
                base.OnMouseClick(e);
                OnClick(e);
            }

            private static GraphicsPath Round(Rectangle r, int radius)
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

        // ===== one command =====
        private sealed class CommandRow : Panel
        {
            private readonly string _cmd, _desc;
            private bool _hover;

            public CommandRow(string cmd, string desc)
            {
                _cmd = cmd; _desc = desc;
                Height = 52;
                Margin = new Padding(0, 0, 0, 6);
                DoubleBuffered = true;
                BackColor = Color.Transparent;
                Cursor = Cursors.Hand;

                var copy = new Button
                {
                    Text = Localization.IsPolish ? "KOPIUJ" : "COPY",
                    Size = new Size(88, 28),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Theme.Accent,
                    ForeColor = Color.Black,
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                };
                copy.FlatAppearance.BorderSize = 0;
                copy.Click += (s, e) => Copy();
                Controls.Add(copy);
                Resize += (s, e) => copy.Location = new Point(Width - copy.Width - 12, 12);

                // clicking the row itself copies too — the button is just the obvious target
                Click += (s, e) => Copy();
                MouseEnter += (s, e) => { _hover = true; Invalidate(); };
                MouseLeave += (s, e) => { _hover = false; Invalidate(); };
            }

            private void Copy() => CopyToClipboard(this, _cmd, _cmd);

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var rect = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var p = Round(rect, 12))
                {
                    using var bg = new LinearGradientBrush(rect, Theme.CardTop, Theme.CardBottom, 90f);
                    g.FillPath(bg, p);
                    using var pen = new Pen(Color.FromArgb(_hover ? 130 : 55, Theme.Accent), 1f);
                    g.DrawPath(pen, p);
                }

                using var codeFont = new Font("Consolas", 10.5f, FontStyle.Bold);
                using var descFont = new Font("Segoe UI", 8.5f);
                using var cb = new SolidBrush(Theme.TextPrimary);
                using var db = new SolidBrush(Theme.TextMuted);

                g.DrawString(_cmd, codeFont, cb, 16, 8);
                g.DrawString(_desc, descFont, db, 17, 30);
            }

            private static GraphicsPath Round(Rectangle r, int radius)
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
}
