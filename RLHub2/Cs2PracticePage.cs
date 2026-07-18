using System;
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
    // These only do anything on a server you host (a private lobby / local practice map).
    // Official matchmaking refuses sv_cheats server-side, so this is a practice-config helper,
    // not something that affects real matches. The page says so, because a page full of
    // "cheat" commands with no context invites the wrong assumption.
    public class Cs2PracticePage : UserControl
    {
        private sealed class Recipe
        {
            public string Name = "";
            public string Category = "";
            public string Commands = "";
        }

        private static readonly Recipe[] Recipes =
        {
            new()
            {
                Name = "Serwer treningowy",
                Category = "pełny setup — wklej raz na starcie",
                Commands = string.Join("\n",
                    "sv_cheats 1",
                    "mp_limitteams 0",
                    "mp_autoteambalance 0",
                    "mp_roundtime 60",
                    "mp_roundtime_defuse 60",
                    "mp_maxmoney 60000",
                    "mp_startmoney 60000",
                    "mp_freezetime 0",
                    "mp_buytime 9999",
                    "mp_buy_anywhere 1",
                    "sv_infinite_ammo 1",
                    "ammo_grenade_limit_total 5",
                    "sv_showimpacts 1",
                    "bot_kick",
                    "mp_warmup_end",
                    "mp_restartgame 1"),
            },
            new()
            {
                Name = "Granaty",
                Category = "trajektoria i powtarzanie rzutu",
                Commands = string.Join("\n",
                    "sv_cheats 1",
                    "sv_grenade_trajectory_prac_pipreview 1",
                    "sv_grenade_trajectory_time 8",
                    "ammo_grenade_limit_total 5",
                    "sv_infinite_ammo 1",
                    "bind \"h\" \"sv_rethrow_last_grenade\"",
                    "bind \"n\" \"noclip\""),
            },
            new()
            {
                Name = "Aim / strzelanie",
                Category = "podgląd trafień i rozrzutu",
                Commands = string.Join("\n",
                    "sv_cheats 1",
                    "sv_showimpacts 1",
                    "sv_showimpacts_time 8",
                    "sv_infinite_ammo 1",
                    "mp_free_armor 2",
                    "mp_restartgame 1"),
            },
            new()
            {
                Name = "Boty",
                Category = "dodaj, zatrzymaj, wyrzuć",
                Commands = string.Join("\n",
                    "sv_cheats 1",
                    "bot_kick",
                    "bot_add_t",
                    "bot_add_ct",
                    "bot_stop 1",
                    "bot_dontmove 1",
                    "bot_difficulty 3",
                    "bot_place"),
            },
            new()
            {
                Name = "Ruch",
                Category = "noclip, nieśmiertelność, pozycja",
                Commands = string.Join("\n",
                    "sv_cheats 1",
                    "noclip",
                    "god",
                    "cl_showpos 1",
                    "bind \"n\" \"noclip\""),
            },
            new()
            {
                Name = "Bindy treningowe",
                Category = "jedno kliknięcie zamiast wpisywania",
                Commands = string.Join("\n",
                    "bind \"n\" \"noclip\"",
                    "bind \"h\" \"sv_rethrow_last_grenade\"",
                    "bind \"j\" \"give weapon_flashbang\"",
                    "bind \"k\" \"give weapon_smokegrenade\"",
                    "bind \"l\" \"give weapon_molotov\"",
                    "bind \"o\" \"mp_restartgame 1\""),
            },
            new()
            {
                Name = "Reset rundy",
                Category = "szybki restart i ekonomia",
                Commands = string.Join("\n",
                    "mp_warmup_end",
                    "mp_restartgame 1",
                    "mp_maxmoney 60000",
                    "mp_startmoney 60000"),
            },
            new()
            {
                Name = "Retake / egzekucja",
                Category = "ustaw bomby i pozycje",
                Commands = string.Join("\n",
                    "sv_cheats 1",
                    "mp_freezetime 0",
                    "mp_roundtime_defuse 60",
                    "give weapon_c4",
                    "bot_place",
                    "bot_stop 1"),
            },
        };

        public Cs2PracticePage()
        {
            BackColor = Theme.PageBg;
            Dock = DockStyle.Fill;

            var title = new Label
            {
                Text = Localization.IsPolish ? "TRENING" : "PRACTICE",
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(24, 8, 0, 0),
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
            };
            var hint = new Label
            {
                Text = Localization.IsPolish
                    ? "Komendy działają tylko na własnym serwerze (sv_cheats). Wklej w konsoli (~)."
                    : "These only work on your own server (sv_cheats). Paste into the console (~).",
                Dock = DockStyle.Top,
                Height = 26,
                Padding = new Padding(24, 0, 0, 0),
                ForeColor = Theme.TextMuted,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 8, 20, 20),
                AutoScroll = true,
                BackColor = Theme.PageBg,
            };
            foreach (var r in Recipes)
                flow.Controls.Add(new RecipeCard(r.Name, r.Category, r.Commands));

            Controls.Add(flow);
            Controls.Add(hint);
            Controls.Add(title);
        }

        private sealed class RecipeCard : Panel
        {
            private const int PreviewLines = 6;
            private readonly string _name, _category, _commands;

            public RecipeCard(string name, string category, string commands)
            {
                _name = name; _category = category; _commands = commands;
                Size = new Size(400, 208);
                Margin = new Padding(0, 0, 16, 16);
                DoubleBuffered = true;
                BackColor = Color.Transparent;

                var copy = new Button
                {
                    Text = Localization.IsPolish ? "KOPIUJ" : "COPY",
                    Size = new Size(96, 30),
                    Location = new Point(Width - 96 - 16, Height - 30 - 14),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Theme.Accent,
                    ForeColor = Color.Black,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                };
                copy.FlatAppearance.BorderSize = 0;
                copy.Click += (s, e) => Copy();
                Controls.Add(copy);
            }

            private void Copy()
            {
                try
                {
                    Clipboard.SetText(_commands);
                    Toast.Show(FindForm() is Control c ? c : this,
                        Localization.IsPolish ? $"Skopiowano: {_name}" : $"Copied: {_name}",
                        ToastKind.Success);
                }
                catch
                {
                    Toast.Show(this, Localization.IsPolish ? "Nie udało się skopiować" : "Copy failed", ToastKind.Info);
                }
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
                    using var pen = new Pen(Color.FromArgb(60, Theme.Accent), 1f);
                    g.DrawPath(pen, p);
                }

                using var nameFont = new Font("Segoe UI", 13.5f, FontStyle.Bold);
                using var catFont = new Font("Segoe UI", 8.5f);
                using var codeFont = new Font("Consolas", 8.5f);
                using var tp = new SolidBrush(Theme.TextPrimary);
                using var tm = new SolidBrush(Theme.TextMuted);
                using var ts = new SolidBrush(Theme.TextSecondary);

                g.DrawString(_name, nameFont, tp, 16, 14);
                g.DrawString(_category, catFont, tm, 17, 40);

                // command preview on a darker plate, trimmed to a few lines so cards stay even
                var code = new Rectangle(14, 60, Width - 28, 100);
                using (var cp = Round(code, 10))
                using (var cb = new SolidBrush(Color.FromArgb(120, 10, 12, 16)))
                    g.FillPath(cb, cp);

                var lines = _commands.Split('\n');
                int shown = Math.Min(PreviewLines, lines.Length);
                for (int i = 0; i < shown; i++)
                    g.DrawString(lines[i], codeFont, ts, 22, 66 + i * 14);
                if (lines.Length > shown)
                    g.DrawString($"… +{lines.Length - shown}", codeFont, tm, 22, 66 + shown * 14);

                using var cntFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                g.DrawString($"{lines.Length} {(Localization.IsPolish ? "komend" : "commands")}",
                    cntFont, tm, 16, Height - 30);
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
