using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;
using RLHub2.Helpers;
using RLHub2.Models;

namespace RLHub2.Controls
{
    // Rounded dark candlestick chart for a single playlist.
    // Open = previous entry value, Close = current entry value.
    // Y axis is fixed via YMin / YMax (set per mode by the page).
    public class MmrChartControl : Panel
    {
        private List<MmrEntry> _entries = new();

        public int CornerRadius { get; set; } = 18;
        public int YMin { get; set; } = 0;
        public int YMax { get; set; } = 2000;
        public Color Accent { get; set; } = Color.FromArgb(120, 60, 255);
        public string EmptyTitle { get; set; } = "No data yet";
        public string EmptySub { get; set; } = "Add your first MMR entry below";

        private static readonly Color UpColor = Color.FromArgb(0, 200, 120);
        private static readonly Color DownColor = Color.FromArgb(230, 70, 80);

        private int _hover = -1;

        public MmrChartControl()
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        }

        private Rectangle PlotRect() => new Rectangle(56, 22, Width - 56 - 22, Height - 22 - 44);

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_entries.Count == 0) { if (_hover != -1) { _hover = -1; Invalidate(); } return; }

            var plot = PlotRect();
            var (startX, candleW) = CandleLayout(plot);
            int idx = candleW <= 0 ? -1 : (int)((e.X - startX) / candleW);
            if (idx < 0 || idx >= _entries.Count) idx = -1;
            if (idx != _hover) { _hover = idx; Invalidate(); }
        }

        // Candles have a capped width and touch edge-to-edge (no gaps), packed from the left. With
        // few entries that's a compact run on the left rather than a couple of giant blocks; with
        // many, the width shrinks to fill the plot. Returns the left edge of the first candle and
        // the per-candle width so painting, hit-testing and the hover guide all line up.
        private (float startX, float candleW) CandleLayout(Rectangle plot)
        {
            int n = _entries.Count;
            if (n == 0) return (plot.Left, 0f);
            float w = Math.Min(plot.Width / (float)n, 48f);
            return (plot.Left, w);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hover != -1) { _hover = -1; Invalidate(); }
        }

        public void Configure(int yMin, int yMax, Color accent)
        {
            YMin = yMin;
            YMax = yMax;
            Accent = accent;
            Invalidate();
        }

        public void SetEntries(IEnumerable<MmrEntry> entries)
        {
            _entries = (entries ?? Enumerable.Empty<MmrEntry>())
                .OrderBy(e => e.Timestamp)
                .ToList();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var outer = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = RoundedRect(outer, CornerRadius))
            using (var bg = new LinearGradientBrush(outer, Theme.CardTop, Theme.CardBottom, 90f))
            {
                g.FillPath(bg, path);
                using var border = new Pen(Color.FromArgb(70, Accent), 1f);
                g.DrawPath(border, path);
            }

            int padL = 56, padR = 22, padT = 22, padB = 44;
            var plot = new Rectangle(padL, padT, Width - padL - padR, Height - padT - padB);
            if (plot.Width <= 10 || plot.Height <= 10)
                return;

            float YOf(int v)
            {
                int c = Math.Max(YMin, Math.Min(YMax, v));
                return plot.Bottom - (c - YMin) / (float)(YMax - YMin) * plot.Height;
            }

            // Horizontal grid + Y labels. The step follows the range — a fixed 100 would draw
            // twenty-odd lines across a 0-2000 axis and bury the curve.
            int span = Math.Max(1, YMax - YMin);
            int gridStep = span <= 400 ? 50 : span <= 1000 ? 100 : span <= 2500 ? 250 : 500;

            using (var grid = new Pen(Theme.GridLines) { DashStyle = DashStyle.Dash })
            using (var axisFont = new Font("Segoe UI", 8.5f))
            using (var axisBrush = new SolidBrush(Theme.TextMuted))
            {
                for (int v = YMin; v <= YMax; v += gridStep)
                {
                    float yy = YOf(v);
                    g.DrawLine(grid, plot.Left, yy, plot.Right, yy);
                    g.DrawString(v.ToString(), axisFont, axisBrush, 8, yy - 8);
                }
            }

            if (_entries.Count == 0)
            {
                float cx = plot.Left + plot.Width / 2f;
                float cy = plot.Top + plot.Height / 2f;
                var center = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                using (var gf = new Font("Segoe UI Emoji", 30f))
                using (var gb = new SolidBrush(Color.FromArgb(130, Accent)))
                    g.DrawString("\U0001F4C8", gf, gb, cx, cy - 34, center);
                using (var tf = new Font("Segoe UI", 14f, FontStyle.Bold))
                using (var tb = new SolidBrush(Theme.TextPrimary))
                    g.DrawString(EmptyTitle, tf, tb, cx, cy + 14, center);
                using (var sf = new Font("Segoe UI", 10f))
                using (var sb = new SolidBrush(Theme.TextMuted))
                    g.DrawString(EmptySub, sf, sb, cx, cy + 38, center);
                return;
            }

            int n = _entries.Count;
            var (startX, candleW) = CandleLayout(plot);

            // X labels. When every entry is from the same day (the DAY view) the date would just
            // repeat, so show the time instead. Labels are drawn left to right and any that would
            // overlap the previous one are skipped — so they never run into each other.
            bool sameDay = _entries[0].Timestamp.Date == _entries[n - 1].Timestamp.Date;
            string fmt = sameDay ? "HH:mm" : "dd.MM";
            using (var axisFont = new Font("Segoe UI", 10f, FontStyle.Bold))
            using (var axisBrush = new SolidBrush(Theme.TextMuted))
            {
                float lastRight = float.MinValue;
                for (int i = 0; i < n; i++)
                {
                    float xCenter = startX + candleW * (i + 0.5f);
                    string s = _entries[i].Timestamp.ToString(fmt);
                    var sz = g.MeasureString(s, axisFont);
                    float lx = xCenter - sz.Width / 2f;
                    if (lx <= lastRight + 8) continue;   // would touch the previous label — skip
                    g.DrawString(s, axisFont, axisBrush, lx, plot.Bottom + 6);
                    lastRight = lx + sz.Width;
                }
            }

            // Candles.
            for (int i = 0; i < n; i++)
            {
                int close = _entries[i].Value;
                int open = i > 0 ? _entries[i - 1].Value : close;

                bool up = close >= open;
                Color col = up ? UpColor : DownColor;

                float left = startX + candleW * i;   // candles touch edge-to-edge

                float yHigh = YOf(Math.Max(open, close));
                float yLow = YOf(Math.Min(open, close));
                float bodyTop = yHigh;
                float bodyH = Math.Max(3f, yLow - yHigh);

                // Bodies touch edge-to-edge; thin separators keep them readable.
                using (var b = new SolidBrush(col))
                    g.FillRectangle(b, left, bodyTop, candleW, bodyH);
                using (var p = new Pen(Color.FromArgb(90, 12, 14, 24), 1f))
                    g.DrawRectangle(p, left, bodyTop, candleW, bodyH);
            }

            // Current value + delta (top-left).
            int latest = _entries[n - 1].Value;
            int delta = n >= 2 ? latest - _entries[n - 2].Value : 0;
            using (var vf = new Font("Segoe UI", 14f, FontStyle.Bold))
            using (var vb = new SolidBrush(Theme.TextPrimary))
                g.DrawString(latest.ToString(), vf, vb, plot.Left + 4, plot.Top - 2);

            string deltaTxt = delta > 0 ? $"▲ +{delta}" : delta < 0 ? $"▼ {delta}" : "0";
            Color deltaCol = delta > 0 ? UpColor : delta < 0 ? DownColor : Color.LightGray;
            using (var df = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (var db = new SolidBrush(deltaCol))
                g.DrawString(deltaTxt, df, db, plot.Left + 52, plot.Top + 4);

            // Hover tooltip (date + value) with a vertical guide line.
            if (_hover >= 0 && _hover < n)
            {
                var en = _entries[_hover];
                var (hStartX, hCandleW) = CandleLayout(plot);
                float xc = hStartX + hCandleW * (_hover + 0.5f);

                using (var guide = new Pen(Color.FromArgb(130, Accent), 1f) { DashStyle = DashStyle.Dash })
                    g.DrawLine(guide, xc, plot.Top, xc, plot.Bottom);

                string txt = $"{en.Timestamp:dd.MM.yyyy}    {en.Value}";
                using var tf = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                var sz = g.MeasureString(txt, tf);
                float bw = sz.Width + 18, bh = sz.Height + 10;
                float bx = Math.Min(Math.Max(plot.Left, xc - bw / 2), plot.Right - bw);
                float by = plot.Top + 4;

                var box = new Rectangle((int)bx, (int)by, (int)bw, (int)bh);
                using (var path = RoundedRect(box, 8))
                {
                    using (var bg = new SolidBrush(Theme.IsDark ? Color.FromArgb(240, 22, 24, 44) : Color.FromArgb(245, 255, 255, 255)))
                        g.FillPath(bg, path);
                    using (var pen = new Pen(Accent, 1.4f))
                        g.DrawPath(pen, path);
                }
                using (var tb = new SolidBrush(Theme.TextPrimary))
                    g.DrawString(txt, tf, tb, bx + 9, by + 5);
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
