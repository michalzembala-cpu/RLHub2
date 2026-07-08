using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using RLHub2.Helpers;

namespace RLHub2.Controls
{
    // Rounded, gradient dashboard tile.
    // Draws an uppercase title, a large value and an optional subtitle directly
    // in OnPaint (no child labels) so text always sits cleanly on the gradient.
    public class StatTile : Panel
    {
        private string _title = "TITLE";
        private string _value = "--";
        private string _subtitle = "";
        private Color _accent = Color.FromArgb(0, 140, 255);
        private bool _hovered;

        public int CornerRadius { get; set; } = 18;
        public float TitleFontSize { get; set; } = 9.5f;
        public float ValueFontSize { get; set; } = 18f;
        public float SubtitleFontSize { get; set; } = 9f;
        public Image? Icon { get; set; }

        public string Title
        {
            get => _title;
            set { _title = value ?? ""; Invalidate(); }
        }

        public string Value
        {
            get => _value;
            set { _value = value ?? ""; Invalidate(); }
        }

        public string Subtitle
        {
            get => _subtitle;
            set { _subtitle = value ?? ""; Invalidate(); }
        }

        public Color Accent
        {
            get => _accent;
            set { _accent = value; Invalidate(); }
        }

        public StatTile()
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            Size = new Size(240, 92);
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hovered = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = RoundedRect(rect, CornerRadius);

            // Background gradient: surface tinted by the accent at the top.
            var top = Theme.Mix(Theme.CardTop, _accent, 0.16f);
            var bottom = Theme.CardBottom;
            using (var brush = new LinearGradientBrush(rect, top, bottom, 90f))
                g.FillPath(brush, path);

            // Accent stripe down the left edge (clipped to the rounded shape).
            g.SetClip(path);
            using (var stripe = new SolidBrush(_accent))
                g.FillRectangle(stripe, 0, 0, 6, Height);
            g.ResetClip();

            // Border / hover glow.
            using (var pen = new Pen(_hovered ? _accent : Color.FromArgb(70, _accent), _hovered ? 2f : 1f))
                g.DrawPath(pen, path);

            // Text block.
            using var titleFont = new Font("Segoe UI", TitleFontSize, FontStyle.Bold);
            using var valueFont = new Font("Segoe UI", ValueFontSize, FontStyle.Bold);
            using var subFont = new Font("Segoe UI", SubtitleFontSize, FontStyle.Regular);

            using var titleBrush = new SolidBrush(Theme.TextSecondary);
            using var valueBrush = new SolidBrush(Theme.TextPrimary);
            using var subBrush = new SolidBrush(Theme.TextMuted);

            int padX = 22;

            // Optional rank icon on a white chip at the left.
            if (Icon != null)
            {
                int bs = Math.Min(Height - 24, 56);
                var badge = new Rectangle(16, (Height - bs) / 2, bs, bs);
                using var bp = RoundedRect(badge, 12);
                using (var wb = new SolidBrush(Color.White))
                    g.FillPath(wb, bp);
                g.SetClip(bp);
                var inner = Rectangle.Inflate(badge, -6, -6);
                float ar = Icon.Width / (float)Icon.Height;
                int w = inner.Width, h = inner.Height;
                if (ar > 1) h = (int)(w / ar); else w = (int)(h * ar);
                g.DrawImage(Icon, inner.X + (inner.Width - w) / 2, inner.Y + (inner.Height - h) / 2, w, h);
                g.ResetClip();
                padX = badge.Right + 14;
            }

            int y = 16;
            g.DrawString(_title.ToUpperInvariant(), titleFont, titleBrush, padX, y);

            y += titleFont.Height + 4;
            g.DrawString(_value, valueFont, valueBrush, padX - 2, y);

            if (!string.IsNullOrEmpty(_subtitle))
            {
                y += valueFont.Height + 4;
                g.DrawString(_subtitle, subFont, subBrush, padX, y);
            }

            base.OnPaint(e);
        }

        private static Color Mix(Color a, Color b, float t)
        {
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
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
