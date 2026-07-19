using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;
using RLHub2.Helpers;

namespace RLHub2.Controls
{
    // Small "living" card: title + current value + a sparkline of recent MMR.
    public class SparkCard : Panel
    {
        private int[] _values = Array.Empty<int>();

        public int CornerRadius { get; set; } = 18;
        public string Title { get; set; } = "";
        public Color Accent { get; set; } = Color.FromArgb(0, 140, 255);

        public SparkCard()
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        }

        public void SetValues(IEnumerable<int> values)
        {
            _values = (values ?? Enumerable.Empty<int>()).ToArray();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = Rounded(rect, CornerRadius);
            using (var bg = new LinearGradientBrush(rect, Theme.Mix(Theme.CardTop, Accent, 0.14f), Theme.CardBottom, 90f))
                g.FillPath(bg, path);
            using (var pen = new Pen(Color.FromArgb(70, Accent), 1f))
                g.DrawPath(pen, path);

            const int padX = 20;
            using (var tf = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (var tb = new SolidBrush(Theme.TextSecondary))
                g.DrawString(Title.ToUpperInvariant(), tf, tb, padX, 14);

            int cur = _values.Length > 0 ? _values[^1] : 0;
            int delta = _values.Length >= 2 ? _values[^1] - _values[0] : 0;
            using (var vf = new Font("Segoe UI", 22f, FontStyle.Bold))
            using (var vb = new SolidBrush(Theme.TextPrimary))
                g.DrawString(_values.Length > 0 ? cur.ToString() : "—", vf, vb, padX - 2, 34);
            if (_values.Length >= 2)
            {
                string d = delta > 0 ? $"▲ +{delta}" : delta < 0 ? $"▼ {delta}" : "0";
                Color dc = delta > 0 ? Color.FromArgb(46, 204, 113) : delta < 0 ? Color.FromArgb(225, 70, 80) : Theme.TextMuted;
                using var df = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                using var db = new SolidBrush(dc);
                g.DrawString(d, df, db, padX + 62, 44);
            }

            // sparkline area (lower part)
            if (_values.Length >= 2)
            {
                var plot = new Rectangle(padX, Height - 56, Width - padX * 2, 42);

                // Same fixed scale as the full chart, so the sparkline and the MMR page agree
                // rather than the sparkline silently zooming to whatever it happens to hold.
                int min = 0, max = 2000;
                float range = max - min;
                int n = _values.Length;

                PointF P(int i) => new PointF(
                    plot.Left + (n == 1 ? 0 : (float)i / (n - 1) * plot.Width),
                    plot.Bottom - (_values[i] - min) / range * plot.Height);

                var pts = Enumerable.Range(0, n).Select(P).ToArray();

                // gradient fill under the line
                using (var fillPath = new GraphicsPath())
                {
                    fillPath.AddLines(pts);
                    fillPath.AddLine(pts[^1].X, pts[^1].Y, plot.Right, plot.Bottom);
                    fillPath.AddLine(plot.Right, plot.Bottom, plot.Left, plot.Bottom);
                    fillPath.CloseFigure();
                    using var fb = new LinearGradientBrush(plot, Color.FromArgb(90, Accent), Color.FromArgb(0, Accent), 90f);
                    g.FillPath(fb, fillPath);
                }
                using (var lp = new Pen(Accent, 2.4f) { LineJoin = LineJoin.Round })
                    g.DrawLines(lp, pts);
                using (var dot = new SolidBrush(Accent))
                    g.FillEllipse(dot, pts[^1].X - 4, pts[^1].Y - 4, 8, 8);
                using (var dotc = new SolidBrush(Color.White))
                    g.FillEllipse(dotc, pts[^1].X - 2, pts[^1].Y - 2, 4, 4);
            }

            base.OnPaint(e);
        }

        private static GraphicsPath Rounded(Rectangle b, int r)
        {
            var p = new GraphicsPath();
            int d = r * 2;
            p.AddArc(b.Left, b.Top, d, d, 180, 90);
            p.AddArc(b.Right - d, b.Top, d, d, 270, 90);
            p.AddArc(b.Right - d, b.Bottom - d, d, d, 0, 90);
            p.AddArc(b.Left, b.Bottom - d, d, d, 90, 90);
            p.CloseAllFigures();
            return p;
        }
    }
}
