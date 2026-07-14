using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using RLHub2.Helpers;

namespace RLHub2.Controls
{
    // Small rotating-arc loading indicator. Spins only while visible.
    public class Spinner : Control
    {
        private readonly System.Windows.Forms.Timer _timer;
        private float _angle;

        public Color Accent { get; set; } = Color.FromArgb(120, 60, 255);
        public float Thickness { get; set; } = 3f;

        public Spinner()
        {
            DoubleBuffered = true;
            TabStop = false;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(32, 32);

            _timer = new System.Windows.Forms.Timer { Interval = 16 };
            _timer.Tick += (s, e) => { _angle = (_angle + 7f) % 360f; Invalidate(); };
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible) _timer.Start(); else _timer.Stop();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float pad = Thickness + 1;
            int m = Math.Min(Width, Height);
            var r = new RectangleF((Width - m) / 2f + pad, (Height - m) / 2f + pad, m - pad * 2, m - pad * 2);
            if (r.Width <= 1 || r.Height <= 1) return;

            using (var track = new Pen(Color.FromArgb(45, Accent), Thickness))
                g.DrawEllipse(track, r);

            using var pen = new Pen(Accent, Thickness)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            g.DrawArc(pen, r, _angle, 100f);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _timer?.Stop(); _timer?.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
