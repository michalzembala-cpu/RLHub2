using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using RLHub2.Helpers;

namespace RLHub2.Controls
{
    // Rounded news item: category pill, title, wrapped description, date. Hover glow.
    public class NewsCard : Control
    {
        private bool _hovered;

        public int CornerRadius { get; set; } = 16;
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public string DateText { get; set; } = "";
        public Color Accent { get; set; } = Color.FromArgb(120, 60, 255);

        public NewsCard()
        {
            DoubleBuffered = true;
            Height = 118;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
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

            using (var bg = new LinearGradientBrush(rect, Theme.CardTop, Theme.CardBottom, 90f))
                g.FillPath(bg, path);

            using (var pen = new Pen(_hovered ? Accent : Color.FromArgb(60, Accent), _hovered ? 2f : 1f))
                g.DrawPath(pen, path);

            const int padX = 20;

            // Category pill.
            using (var pillFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            {
                string cat = Category.ToUpperInvariant();
                var sz = g.MeasureString(cat, pillFont);
                var pill = new Rectangle(padX, 16, (int)sz.Width + 22, 22);
                using (var pillPath = RoundedRect(pill, 11))
                using (var pb = new SolidBrush(Accent))
                    g.FillPath(pb, pillPath);
                using (var tb = new SolidBrush(Color.White))
                    g.DrawString(cat, pillFont, tb, pill.X + 11, pill.Y + 4);
            }

            // Date (top-right).
            if (!string.IsNullOrEmpty(DateText))
            {
                using var df = new Font("Segoe UI", 11f, FontStyle.Bold);
                using var db = new SolidBrush(Theme.TextMuted);
                var sz = g.MeasureString(DateText, df);
                g.DrawString(DateText, df, db, Width - padX - sz.Width, 16);
            }

            // Title.
            using (var tf = new Font("Segoe UI", 13.5f, FontStyle.Bold))
            using (var tb = new SolidBrush(Theme.TextPrimary))
            {
                var fmt = new StringFormat
                {
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                g.DrawString(Text, tf, tb, new RectangleF(padX, 46, Width - padX * 2, 24), fmt);
            }

            // Description (wrapped, up to 2 lines).
            if (!string.IsNullOrEmpty(Description))
            {
                using var f = new Font("Segoe UI", 10f);
                using var b = new SolidBrush(Theme.TextSecondary);
                var fmt = new StringFormat
                {
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.LineLimit
                };
                g.DrawString(Description, f, b, new RectangleF(padX, 74, Width - padX * 2, 38), fmt);
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
