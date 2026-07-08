using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RLHub2.Controls
{
    // Simple rounded panel with entrance fade+slide animation and hover glow.
    public class RoundedPanel : Panel
    {
        public int CornerRadius { get; set; } = 18;
        public Color StartColor { get; set; } = Color.FromArgb(24, 34, 58);
        public Color EndColor { get; set; } = Color.FromArgb(15, 33, 56);
        public Color GlowColor { get; set; } = Color.FromArgb(46, 196, 255);

        System.Windows.Forms.Timer animTimer;
        DateTime animStart;
        int animDuration = 320;
        float progress = 1f; // 0..1
        int slideFrom = 24;
        bool hovered = false;

        public RoundedPanel()
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            animTimer = new System.Windows.Forms.Timer() { Interval = 16 };
            animTimer.Tick += (s, e) =>
            {
                var elapsed = (float)(DateTime.UtcNow - animStart).TotalMilliseconds;
                var t = Math.Min(1f, elapsed / animDuration);
                // cubic ease-out
                var eased = 1f - (float)Math.Pow(1 - t, 3);
                progress = eased;
                Invalidate();
                if (t >= 1f) animTimer.Stop();
            };
        }

        public void StartEntranceAnimation(int fromOffset = 24, int durationMs = 320)
        {
            slideFrom = fromOffset;
            animDuration = Math.Max(80, durationMs);
            progress = 0f;
            animStart = DateTime.UtcNow;
            animTimer.Start();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            hovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hovered = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = Width;
            int h = Height;

            // compute alpha from progress
            int alpha = (int)(255 * progress);

            // fill with gradient
            using (var path = RoundedRect(new Rectangle(0, 0, w - 1, h - 1), CornerRadius))
            using (var brush = new LinearGradientBrush(new Rectangle(0, 0, w, h), StartColor, EndColor, 90f))
            {
                var sc = Color.FromArgb(alpha, StartColor);
                var ec = Color.FromArgb(alpha, EndColor);
                brush.InterpolationColors = new ColorBlend()
                {
                    Colors = new[] { sc, ec },
                    Positions = new[] { 0f, 1f }
                };

                // draw shadow (simple)
                using (var shadowBrush = new SolidBrush(Color.FromArgb((int)(24 * progress), 0, 0, 0)))
                {
                    g.FillPath(shadowBrush, RoundedRect(new Rectangle(4, 6, w - 1, h - 1), CornerRadius));
                }

                // apply slide offset by translating the graphics when drawing content
                var tx = (int)((1f - progress) * slideFrom);
                g.TranslateTransform(-tx, 0);
                g.FillPath(brush, path);

                // hover glow border
                if (hovered)
                {
                    using (var pen = new Pen(Color.FromArgb((int)(120 * progress), GlowColor), 2f))
                    {
                        g.DrawPath(pen, path);
                    }
                }

                g.ResetTransform();
            }

            base.OnPaint(e);
        }

        static GraphicsPath RoundedRect(Rectangle bounds, int radius)
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
