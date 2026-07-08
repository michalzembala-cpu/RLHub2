using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using RLHub2.Helpers;

namespace RLHub2.Controls
{
    // Rounded card with a title, big percent value, subtitle and a progress bar.
    public class ProgressTile : Panel
    {
        private string _title = "";
        private string _subtitle = "";
        private float _fraction;
        private Color _accent = Color.FromArgb(120, 60, 255);

        public int CornerRadius { get; set; } = 16;

        public string Title { get => _title; set { _title = value ?? ""; Invalidate(); } }
        public string Subtitle { get => _subtitle; set { _subtitle = value ?? ""; Invalidate(); } }
        public float Fraction { get => _fraction; set { _fraction = Math.Max(0f, Math.Min(1f, value)); Invalidate(); } }
        public Color Accent { get => _accent; set { _accent = value; Invalidate(); } }

        public ProgressTile()
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = Rounded(rect, CornerRadius);
            var top = Theme.Mix(Theme.CardTop, _accent, 0.16f);
            using (var bg = new LinearGradientBrush(rect, top, Theme.CardBottom, 90f))
                g.FillPath(bg, path);
            using (var pen = new Pen(Color.FromArgb(70, _accent), 1f))
                g.DrawPath(pen, path);

            g.SetClip(path);
            using (var stripe = new SolidBrush(_accent)) g.FillRectangle(stripe, 0, 0, 6, Height);
            g.ResetClip();

            const int padX = 20;
            using (var tf = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (var tb = new SolidBrush(Theme.TextSecondary))
                g.DrawString(_title.ToUpperInvariant(), tf, tb, padX, 14);

            using (var vf = new Font("Segoe UI", 22f, FontStyle.Bold))
            using (var vb = new SolidBrush(Theme.TextPrimary))
                g.DrawString($"{(int)Math.Round(_fraction * 100)}%", vf, vb, padX - 2, 34);

            if (!string.IsNullOrEmpty(_subtitle))
                using (var sf = new Font("Segoe UI", 9f))
                using (var sb = new SolidBrush(Theme.TextMuted))
                    g.DrawString(_subtitle, sf, sb, padX, Height - 40);

            // progress bar
            int barY = Height - 18, barH = 7, barX = padX, barW = Width - padX * 2;
            using (var trackPath = Rounded(new Rectangle(barX, barY, barW, barH), barH / 2))
            using (var tb2 = new SolidBrush(Theme.SurfaceAlt))
                g.FillPath(tb2, trackPath);
            int fillW = (int)(barW * _fraction);
            if (fillW > barH)
                using (var fillPath = Rounded(new Rectangle(barX, barY, fillW, barH), barH / 2))
                using (var fb = new SolidBrush(_accent))
                    g.FillPath(fb, fillPath);

            base.OnPaint(e);
        }

        private static GraphicsPath Rounded(Rectangle b, int r)
        {
            var p = new GraphicsPath();
            int d = r * 2;
            if (d <= 0) { p.AddRectangle(b); return p; }
            p.AddArc(b.Left, b.Top, d, d, 180, 90);
            p.AddArc(b.Right - d, b.Top, d, d, 270, 90);
            p.AddArc(b.Right - d, b.Bottom - d, d, d, 0, 90);
            p.AddArc(b.Left, b.Bottom - d, d, d, 90, 90);
            p.CloseAllFigures();
            return p;
        }
    }
}
