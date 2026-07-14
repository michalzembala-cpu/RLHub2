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
        // Smoothly animated hover (0 = out, 1 = fully hovered).
        private float _hover;
        private bool _hoverTarget;
        private System.Windows.Forms.Timer? _hoverAnim;

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
        private void EnsureHoverAnim()
        {
            if (_hoverAnim != null) return;
            _hoverAnim = new System.Windows.Forms.Timer { Interval = 15 };
            _hoverAnim.Tick += (s, e) =>
            {
                float target = _hoverTarget ? 1f : 0f;
                float d = target - _hover;
                if (Math.Abs(d) < 0.02f) { _hover = target; _hoverAnim!.Stop(); }
                else _hover += d * 0.28f;
                Invalidate();
            };
        }

        protected override void OnMouseEnter(EventArgs e)
        { base.OnMouseEnter(e); EnsureHoverAnim(); _hoverTarget = true; _hoverAnim!.Start(); }

        protected override void OnMouseLeave(EventArgs e)
        { base.OnMouseLeave(e); EnsureHoverAnim(); _hoverTarget = false; _hoverAnim!.Start(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _hoverAnim?.Stop(); _hoverAnim?.Dispose(); }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = RoundedRect(rect, CornerRadius);

            using (var bg = new LinearGradientBrush(rect, Theme.CardTop, Theme.CardBottom, 90f))
                g.FillPath(bg, path);
            // Outer accent glow that fades in on hover.
            if (_hover > 0.01f)
                for (int i = 4; i >= 1; i--)
                    using (var glow = new Pen(Color.FromArgb((int)(16 * _hover), Accent), 1.5f + i * 2f))
                        g.DrawPath(glow, path);

            int borderAlpha = (int)(60 + (255 - 60) * _hover);
            using (var pen = new Pen(Color.FromArgb(borderAlpha, Accent), 1f + _hover))
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
