using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;
using RLHub2.Helpers;

namespace RLHub2
{
    // A library of crosshairs to copy into CS2. Each is copied as a block of console commands
    // (cl_crosshair*), which is exact and I can render an accurate preview from — unlike the
    // CSGO-xxxxx share code, whose encoder I can't verify against a real client, so I don't
    // ship possibly-wrong codes.
    //
    // The pro presets use those players' publicly shared settings; nudge them to taste.
    public class Cs2CrosshairPage : UserControl
    {
        private sealed class Xhair
        {
            public string Name = "";
            public string Author = "";
            public int Style = 4;      // 4 = classic static (what almost every pro uses)
            public float Size = 2;
            public float Thickness = 1;
            public int Gap = -3;
            public bool Dot;
            public bool T;
            public int Outline;         // 0 = off, else outline thickness
            public int R = 0, G = 255, B = 0;
            public int Alpha = 255;

            public string Commands()
            {
                var sb = new StringBuilder();
                sb.AppendLine($"cl_crosshairstyle {Style}");
                sb.AppendLine($"cl_crosshairsize {Size.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                sb.AppendLine($"cl_crosshairthickness {Thickness.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                sb.AppendLine($"cl_crosshairgap {Gap}");
                sb.AppendLine($"cl_crosshairdot {(Dot ? 1 : 0)}");
                sb.AppendLine($"cl_crosshair_t {(T ? 1 : 0)}");
                sb.AppendLine($"cl_crosshair_drawoutline {(Outline > 0 ? 1 : 0)}");
                sb.AppendLine($"cl_crosshair_outlinethickness {Math.Max(0, Outline)}");
                sb.AppendLine("cl_crosshaircolor 5");
                sb.AppendLine($"cl_crosshaircolor_r {R}");
                sb.AppendLine($"cl_crosshaircolor_g {G}");
                sb.AppendLine($"cl_crosshaircolor_b {B}");
                sb.AppendLine($"cl_crosshairalpha {Alpha}");
                sb.Append("cl_crosshairusealpha 1");
                return sb.ToString();
            }

            // The preview already shows dot / T / outline, so the line stays short and never clips.
            public string Summary()
                => $"size {Size:0.#}  •  gap {Gap}  •  thick {Thickness:0.#}";
        }

        private static readonly Xhair[] Library =
        {
            new() { Name = "s1mple",  Author = "Oleksandr Kostyljev", Size = 2, Thickness = 0, Gap = -3, Outline = 1, R = 0,   G = 255, B = 0 },
            new() { Name = "ZywOo",   Author = "Mathieu Herbaut",     Size = 2, Thickness = 1, Gap = -2, Outline = 1, R = 0,   G = 255, B = 255 },
            new() { Name = "NiKo",    Author = "Nikola Kovač",        Size = 1, Thickness = 1, Gap = -3, Dot = true,  Outline = 1, R = 0, G = 255, B = 0 },
            new() { Name = "m0NESY",  Author = "Ilya Osipov",         Size = 3, Thickness = 1, Gap = -3, Outline = 1, R = 0,   G = 255, B = 0 },
            new() { Name = "donk",    Author = "Danil Kryšković",     Size = 1, Thickness = 1, Gap = -1, Outline = 1, R = 0,   G = 255, B = 255 },
            new() { Name = "dev1ce",  Author = "Nicolai Reedtz",      Size = 3, Thickness = 1, Gap = -2, Outline = 0, R = 0,   G = 255, B = 0 },
            new() { Name = "Kropka",  Author = "styl minimalny",      Size = 1, Thickness = 1, Gap = -4, Dot = true, Outline = 1, R = 0, G = 255, B = 0 },
            new() { Name = "T-style", Author = "bez górnej linii",    Size = 4, Thickness = 1, Gap = 0,  T = true, Outline = 1, R = 255, G = 200, B = 0 },
            new() { Name = "Klasyk",  Author = "duży, biały",         Size = 5, Thickness = 1, Gap = 1,  Outline = 1, R = 255, G = 255, B = 255 },
        };

        private readonly FlowLayoutPanel _flow;

        public Cs2CrosshairPage()
        {
            BackColor = Theme.PageBg;
            Dock = DockStyle.Fill;

            var title = new Label
            {
                Text = Localization.IsPolish ? "CELOWNIKI" : "CROSSHAIRS",
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(24, 8, 0, 0),
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
            };
            var hint = new Label
            {
                Text = Localization.IsPolish
                    ? "Kliknij KOPIUJ, wklej w konsoli CS2 (~) i naciśnij Enter"
                    : "Click COPY, paste into the CS2 console (~) and hit Enter",
                Dock = DockStyle.Top,
                Height = 26,
                Padding = new Padding(24, 0, 0, 0),
                ForeColor = Theme.TextMuted,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            };

            _flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 8, 20, 20),
                AutoScroll = true,
                BackColor = Theme.PageBg,
            };
            foreach (var x in Library)
                _flow.Controls.Add(new CrosshairCard(x.Name, x.Author, x.Summary(), x.Commands(), Render(x)));

            Controls.Add(_flow);
            Controls.Add(hint);
            Controls.Add(title);
        }

        // Render the crosshair to a small bitmap for the card. Not CS2's exact geometry — a
        // faithful-enough preview so you can tell them apart at a glance.
        private static Bitmap Render(Xhair x)
        {
            int box = 96;
            var bmp = new Bitmap(box, box);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.None;
            g.Clear(Color.FromArgb(14, 16, 20));

            int cx = box / 2, cy = box / 2;
            int len = Math.Clamp((int)Math.Round(x.Size * 5), 4, 40);
            int thk = Math.Clamp((int)Math.Round(x.Thickness * 3) + 1, 2, 7);
            int gap = Math.Clamp((int)Math.Round((x.Gap + 4) * 2.5), 0, 34);
            var col = Color.FromArgb(x.Alpha, x.R, x.G, x.B);

            void Bar(int rx, int ry, int rw, int rh)
            {
                if (x.Outline > 0)
                    using (var ob = new SolidBrush(Color.FromArgb(230, 0, 0, 0)))
                        g.FillRectangle(ob, rx - 1, ry - 1, rw + 2, rh + 2);
                using var b = new SolidBrush(col);
                g.FillRectangle(b, rx, ry, rw, rh);
            }

            Bar(cx - gap - len, cy - thk / 2, len, thk);           // left
            Bar(cx + gap, cy - thk / 2, len, thk);                 // right
            Bar(cx - thk / 2, cy + gap, thk, len);                 // bottom
            if (!x.T) Bar(cx - thk / 2, cy - gap - len, thk, len); // top
            if (x.Dot) Bar(cx - thk / 2, cy - thk / 2, thk, thk);  // center dot

            return bmp;
        }

        // ===== one crosshair card =====
        private sealed class CrosshairCard : Panel
        {
            private readonly Bitmap _preview;
            private readonly string _name, _author, _summary, _commands;

            public CrosshairCard(string name, string author, string summary, string commands, Bitmap preview)
            {
                _name = name; _author = author; _summary = summary; _commands = commands; _preview = preview;
                Size = new Size(330, 128);
                Margin = new Padding(0, 0, 16, 16);
                DoubleBuffered = true;
                BackColor = Color.Transparent;

                var copy = new Button
                {
                    Text = Localization.IsPolish ? "KOPIUJ" : "COPY",
                    Size = new Size(96, 30),
                    Location = new Point(Width - 96 - 16, Height - 30 - 16),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Theme.Accent,
                    ForeColor = Color.Black,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                };
                copy.FlatAppearance.BorderSize = 0;
                copy.Click += (s, e) => CopyCommands();
                Controls.Add(copy);
            }

            private void CopyCommands()
            {
                try
                {
                    Clipboard.SetText(_commands);
                    Toast.Show(FindForm() is Control c ? c : this,
                        Localization.IsPolish ? $"Skopiowano celownik: {_name}" : $"Copied {_name}'s crosshair",
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

                // preview box
                var pv = new Rectangle(16, 16, 96, 96);
                using (var pp = Round(pv, 10))
                {
                    using var pb = new SolidBrush(Color.FromArgb(14, 16, 20));
                    g.FillPath(pb, pp);
                }
                g.DrawImage(_preview, pv);

                using var nameFont = new Font("Segoe UI", 13f, FontStyle.Bold);
                using var authFont = new Font("Segoe UI", 8.5f);
                using var sumFont = new Font("Segoe UI", 8.5f);
                using var tp = new SolidBrush(Theme.TextPrimary);
                using var tm = new SolidBrush(Theme.TextMuted);
                using var ts = new SolidBrush(Theme.TextSecondary);

                int tx = pv.Right + 16;
                g.DrawString(_name, nameFont, tp, tx, 18);
                g.DrawString(_author, authFont, tm, tx, 44);
                g.DrawString(_summary, sumFont, ts, tx, 64);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) _preview.Dispose();
                base.Dispose(disposing);
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
