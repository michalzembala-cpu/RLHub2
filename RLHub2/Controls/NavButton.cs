using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using RLHub2.Helpers;

namespace RLHub2.Controls
{
    // Sidebar navigation item: rounded hover/active background, accent bar when active,
    // emoji glyph + label. When Collapsed only the centered glyph is shown.
    public class NavButton : Control
    {
        private bool _hovered;
        private bool _active;
        private bool _collapsed;

        public string Glyph { get; set; } = "";
        public Color Accent { get; set; } = Color.FromArgb(120, 60, 255);
        public Color BaseColor { get; set; } = Color.FromArgb(18, 18, 38);

        public bool Active
        {
            get => _active;
            set { _active = value; Invalidate(); }
        }

        public bool Collapsed
        {
            get => _collapsed;
            set { _collapsed = value; Invalidate(); }
        }

        public NavButton()
        {
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            Height = 48;
            TabStop = false;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
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

            var rect = new Rectangle(8, 4, Width - 16, Height - 8);
            using var path = RoundedRect(rect, 12);

            var accent = Theme.Accent;

            // Background: filled when active, lighter on hover, otherwise nothing.
            if (_active)
            {
                using var b = new SolidBrush(Theme.IsDark ? Theme.Mix(Theme.Sidebar, accent, 0.38f) : accent);
                g.FillPath(b, path);
            }
            else if (_hovered)
            {
                using var b = new SolidBrush(Theme.Mix(Theme.Sidebar, accent, 0.18f));
                g.FillPath(b, path);
            }

            // Accent bar on the left edge when active.
            if (_active)
            {
                g.SetClip(path);
                using var bar = new SolidBrush(Theme.IsDark ? accent : Color.White);
                g.FillRectangle(bar, rect.Left, rect.Top, 5, rect.Height);
                g.ResetClip();
            }

            // Glyph.
            if (!string.IsNullOrEmpty(Glyph))
            {
                using var glyphFont = new Font("Segoe UI Emoji", 14f);
                using var glyphBrush = new SolidBrush(_active ? Color.White : Theme.TextSecondary);
                var size = g.MeasureString(Glyph, glyphFont);
                float gx = _collapsed ? (Width - size.Width) / 2f : 24f;
                float gy = (Height - size.Height) / 2f;
                g.DrawString(Glyph, glyphFont, glyphBrush, gx, gy);
            }

            // Label (hidden when collapsed).
            if (!_collapsed && !string.IsNullOrEmpty(Text))
            {
                using var font = new Font("Segoe UI", 11f, _active ? FontStyle.Bold : FontStyle.Regular);
                using var brush = new SolidBrush(_active ? Color.White : Theme.TextSecondary);
                var size = g.MeasureString(Text, font);
                float ty = (Height - size.Height) / 2f;
                g.DrawString(Text, font, brush, 56f, ty);
            }
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
