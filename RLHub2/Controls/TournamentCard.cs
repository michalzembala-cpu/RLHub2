using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using RLHub2.Helpers;

namespace RLHub2.Controls
{
    // Rounded tournament item: status pill, region/tier, name, dates + prize. Hover glow.
    public class TournamentCard : Control
    {
        private bool _hovered;

        public int CornerRadius { get; set; } = 16;
        public string StatusText { get; set; } = "";
        public string RegionName { get; set; } = "";
        public string Tier { get; set; } = "";
        public string DateText { get; set; } = "";
        public string Prize { get; set; } = "";
        public Color Accent { get; set; } = Color.FromArgb(150, 90, 255);

        public TournamentCard()
        {
            DoubleBuffered = true;
            Height = 110;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovered = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hovered = false; Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = RoundedRect(rect, CornerRadius);

            using (var bg = new LinearGradientBrush(rect, Theme.CardTop, Theme.CardBottom, 90f))
                g.FillPath(bg, path);
            using (var pen = new Pen(_hovered ? Accent : Color.FromArgb(60, Accent), _hovered ? 2f : 1f))
                g.DrawPath(pen, path);

            const int padX = 20;

            // Status pill.
            using (var pillFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            {
                string s = StatusText.ToUpperInvariant();
                var sz = g.MeasureString(s, pillFont);
                var pill = new Rectangle(padX, 16, (int)sz.Width + 22, 22);
                using (var pp = RoundedRect(pill, 11))
                using (var pb = new SolidBrush(Accent))
                    g.FillPath(pb, pp);
                using (var tb = new SolidBrush(Color.White))
                    g.DrawString(s, pillFont, tb, pill.X + 11, pill.Y + 4);

                // Region / tier to the right of the pill.
                string rt = string.Join("  •  ", new[] { RegionName, Tier == "" ? "" : "TIER " + Tier });
                rt = rt.Trim(' ', '•').Trim();
                if (!string.IsNullOrWhiteSpace(rt))
                {
                    using var rf = new Font("Segoe UI", 9f, FontStyle.Bold);
                    using var rb = new SolidBrush(Theme.TextMuted);
                    g.DrawString(rt.ToUpperInvariant(), rf, rb, pill.Right + 12, pill.Y + 3);
                }
            }

            // Name.
            using (var tf = new Font("Segoe UI", 13.5f, FontStyle.Bold))
            using (var tb = new SolidBrush(Theme.TextPrimary))
            {
                var fmt = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
                g.DrawString(Text, tf, tb, new RectangleF(padX, 46, Width - padX * 2, 24), fmt);
            }

            // Dates + prize.
            using (var f = new Font("Segoe UI", 10f))
            using (var b = new SolidBrush(Theme.TextMuted))
            {
                string line = DateText;
                if (!string.IsNullOrWhiteSpace(Prize))
                    line += string.IsNullOrWhiteSpace(line) ? Prize : $"     {Prize}";
                g.DrawString(line, f, b, padX, 76);
            }
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
            path.CloseAllFigures();
            return path;
        }
    }
}
