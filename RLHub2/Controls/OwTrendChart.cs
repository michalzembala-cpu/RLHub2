using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using RLHub2.Helpers;
using RLHub2.Models;

namespace RLHub2.Controls
{
    // Session momentum: the running net result (each win +1, loss −1, draw holds) plotted over the
    // matches of the current session. Above the zero line you are up on the session, below it down.
    // Built purely from the win/loss data the tracker already has.
    public class OwTrendChart : Control
    {
        private List<int> _net = new();   // cumulative net after each match

        public OwTrendChart()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        public void SetSession(List<OwMatch> session)
        {
            _net = new List<int>();
            int run = 0;
            foreach (var m in session.OrderBy(m => m.Time))
            {
                if (m.Won) run++;
                else if (m.Lost) run--;
                _net.Add(run);
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var plot = new Rectangle(6, 6, Width - 12, Height - 12);
            using (var frame = new GraphicsPath())
            {
                int r = 14, d = r * 2;
                frame.AddArc(plot.X, plot.Y, d, d, 180, 90);
                frame.AddArc(plot.Right - d, plot.Y, d, d, 270, 90);
                frame.AddArc(plot.Right - d, plot.Bottom - d, d, d, 0, 90);
                frame.AddArc(plot.X, plot.Bottom - d, d, d, 90, 90);
                frame.CloseAllFigures();
                using var bg = new SolidBrush(Color.FromArgb(150, 18, 16, 24));
                g.FillPath(bg, frame);
            }

            if (_net.Count < 2)
            {
                using var f = new Font("Segoe UI", 9.5f);
                using var b = new SolidBrush(Theme.TextMuted);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(Localization.IsPolish ? "Za mało meczów na wykres" : "Not enough matches yet", f, b, plot, sf);
                return;
            }

            int max = Math.Max(1, Math.Max(_net.Max(), -_net.Min()));
            float padX = 14, padY = 16;
            float x0 = plot.Left + padX, x1 = plot.Right - padX;
            float y0 = plot.Top + padY, y1 = plot.Bottom - padY;
            float zeroY = y0 + (y1 - y0) * (max / (float)(2 * max)); // symmetric range [-max, max]

            // zero baseline
            using (var pen = new Pen(Color.FromArgb(70, 200, 205, 225)) { DashStyle = DashStyle.Dash })
                g.DrawLine(pen, x0, zeroY, x1, zeroY);

            PointF P(int i)
            {
                float fx = _net.Count == 1 ? (x0 + x1) / 2 : x0 + (x1 - x0) * i / (_net.Count - 1);
                float fy = zeroY - (y1 - y0) / 2f * (_net[i] / (float)max);
                return new PointF(fx, fy);
            }

            var pts = Enumerable.Range(0, _net.Count).Select(P).ToArray();

            // area fill toward the baseline
            using (var area = new GraphicsPath())
            {
                area.AddLine(pts[0].X, zeroY, pts[0].X, pts[0].Y);
                area.AddLines(pts);
                area.AddLine(pts[^1].X, pts[^1].Y, pts[^1].X, zeroY);
                area.CloseFigure();
                var accent = Theme.Accent;
                using var fill = new LinearGradientBrush(plot, Color.FromArgb(90, accent), Color.FromArgb(10, accent), 90f);
                g.FillPath(fill, area);
            }

            using (var line = new Pen(Theme.Accent, 2.4f) { LineJoin = LineJoin.Round })
                g.DrawLines(line, pts);

            // end marker + current value
            var last = pts[^1];
            int net = _net[^1];
            var dot = net >= 0 ? Color.FromArgb(46, 204, 113) : Color.FromArgb(230, 90, 90);
            using (var db = new SolidBrush(dot))
                g.FillEllipse(db, last.X - 4, last.Y - 4, 8, 8);
            using (var f = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (var b = new SolidBrush(Theme.TextPrimary))
                g.DrawString((net > 0 ? "+" : "") + net, f, b, last.X + 8, last.Y - 10);
        }
    }
}
