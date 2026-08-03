using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using RLHub2.Helpers;

namespace RLHub2
{
    // Confirmation for MMR read off the screen — shows each playlist's value with its rank badge,
    // in the app's dark style, instead of a plain system message box. Result via DialogResult.OK.
    public class ScreenMmrDialog : Form
    {
        private readonly (string Mode, int Mmr)[] _rows;

        public ScreenMmrDialog(Dictionary<string, int> standard)
        {
            bool pl = Localization.IsPolish;

            // Only MMR is shown — not a rank. Rank thresholds in RL differ per playlist and per
            // season, so deriving a rank from a single MMR number is unreliable, and the menu we
            // read doesn't display the rank name anyway.
            var rows = new List<(string, int)>();
            foreach (var m in new[] { "1v1", "2v2", "3v3" })
                if (standard.TryGetValue(m, out int v))
                    rows.Add((m, v));
            _rows = rows.ToArray();

            Text = pl ? "Odczyt MMR z ekranu" : "MMR read from screen";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            BackColor = Theme.PageBg;
            ForeColor = Theme.TextPrimary;
            Font = new Font("Segoe UI", 9.5F);

            const int top = 92, rowH = 52, gap = 10;
            int listH = _rows.Length * rowH + (_rows.Length - 1) * gap;
            ClientSize = new Size(400, top + listH + 84);

            var cancel = Flat(pl ? "ANULUJ" : "CANCEL", ClientSize.Width - 220, ClientSize.Height - 56, 100, Theme.Surface, Theme.TextPrimary);
            cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancel);

            var save = Flat(pl ? "ZAPISZ" : "SAVE", ClientSize.Width - 110, ClientSize.Height - 56, 100, Theme.Accent, Color.White);
            save.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            Controls.Add(save);

            AcceptButton = save;
            CancelButton = cancel;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            bool pl = Localization.IsPolish;

            using (var tf = new Font("Segoe UI", 15F, FontStyle.Bold))
            using (var tb = new SolidBrush(Theme.TextPrimary))
                g.DrawString(pl ? "Odczytano z ekranu" : "Read from screen", tf, tb, 24, 22);
            using (var sf = new Font("Segoe UI", 9.5F))
            using (var sbr = new SolidBrush(Theme.TextMuted))
                g.DrawString(pl ? "Zapisać te wartości do wykresu?" : "Save these values to the chart?", sf, sbr, 24, 54);

            const int top = 92, rowH = 52, gap = 10;
            int y = top;
            foreach (var r in _rows)
            {
                var rect = new Rectangle(24, y, ClientSize.Width - 48, rowH);
                using (var path = Rounded(rect, 12))
                {
                    using var bg = new LinearGradientBrush(rect, Theme.CardTop, Theme.CardBottom, 90f);
                    g.FillPath(bg, path);
                    using var pen = new Pen(Color.FromArgb(60, Theme.Accent), 1f);
                    g.DrawPath(pen, path);
                }

                using (var mf = new Font("Segoe UI", 14F, FontStyle.Bold))
                using (var mb = new SolidBrush(Theme.TextPrimary))
                    g.DrawString(r.Mode.ToUpperInvariant(), mf, mb, rect.X + 18, rect.Y + (rowH - mf.Height) / 2);

                using (var vf = new Font("Segoe UI", 20F, FontStyle.Bold))
                using (var vb = new SolidBrush(Theme.Accent))
                {
                    string val = r.Mmr.ToString();
                    var sz = g.MeasureString(val, vf);
                    g.DrawString(val, vf, vb, rect.Right - sz.Width - 16, rect.Y + (rowH - sz.Height) / 2);
                }

                y += rowH + gap;
            }
        }

        private static Button Flat(string text, int x, int y, int w, Color back, Color fore)
        {
            var b = new Button
            {
                Text = text, Location = new Point(x, y), Size = new Size(w, 34),
                FlatStyle = FlatStyle.Flat, BackColor = back, ForeColor = fore,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand,
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
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
    }
}
