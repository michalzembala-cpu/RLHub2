using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using RLHub2.Helpers;

namespace RLHub2.Controls
{
    // Dense "rank hero" card: rank gem badge + tier/MMR + progress bar + peak/week footer.
    public class RankHero : Panel
    {
        public int CornerRadius { get; set; } = 18;
        public string TierLabel { get; set; } = "—";
        public string MmrText { get; set; } = "";
        public string ProgressText { get; set; } = "";
        public string FootText { get; set; } = "";
        public float Fraction { get; set; }
        public Color Accent { get; set; } = Color.FromArgb(120, 60, 255);
        public Color GemColor { get; set; } = Color.FromArgb(120, 60, 255);
        public Image? Icon { get; set; }

        public RankHero()
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
            var top = Theme.Mix(Theme.CardTop, Accent, 0.18f);
            using (var bg = new LinearGradientBrush(rect, top, Theme.CardBottom, 90f))
                g.FillPath(bg, path);
            using (var pen = new Pen(Color.FromArgb(80, Accent), 1f))
                g.DrawPath(pen, path);

            // ===== rank badge (real icon on a white chip, or drawn gem fallback) =====
            int badgeSize = 84;
            var badge = new Rectangle(14, 16, badgeSize, badgeSize);
            if (Icon != null) DrawIconBadge(g, badge, Icon);
            else DrawGem(g, badge.X + badgeSize / 2, badge.Y + badgeSize / 2, 40, GemColor);

            // ===== tier + mmr =====
            int tx = badge.Right + 16;
            using (var tf = new Font("Segoe UI", 21f, FontStyle.Bold))
            using (var tb = new SolidBrush(Theme.TextPrimary))
                g.DrawString(TierLabel, tf, tb, tx, 22);
            using (var mf = new Font("Segoe UI", 13f, FontStyle.Bold))
            using (var mb = new SolidBrush(Theme.TextSecondary))
                g.DrawString(MmrText, mf, mb, tx, 56);

            // ===== progress =====
            int barX = 20, barW = Width - 40, barY = Height - 46, barH = 10;
            if (!string.IsNullOrEmpty(ProgressText))
                using (var pf = new Font("Segoe UI", 9.5f, FontStyle.Bold))
                using (var pb = new SolidBrush(Theme.TextMuted))
                    g.DrawString(ProgressText, pf, pb, barX, barY - 22);

            using (var trackPath = Rounded(new Rectangle(barX, barY, barW, barH), barH / 2))
            using (var tb2 = new SolidBrush(Theme.SurfaceAlt))
                g.FillPath(tb2, trackPath);
            int fillW = (int)(barW * Math.Max(0f, Math.Min(1f, Fraction)));
            if (fillW > barH)
                using (var fillPath = Rounded(new Rectangle(barX, barY, fillW, barH), barH / 2))
                using (var fb = new LinearGradientBrush(new Rectangle(barX, barY, barW, barH), Accent, Theme.Mix(Accent, Color.White, 0.4f), 0f))
                    g.FillPath(fb, fillPath);

            // ===== footer =====
            if (!string.IsNullOrEmpty(FootText))
                using (var ff = new Font("Segoe UI", 9.5f))
                using (var fb = new SolidBrush(Theme.TextMuted))
                    g.DrawString(FootText, ff, fb, barX, Height - 22);

            base.OnPaint(e);
        }

        private static void DrawIconBadge(Graphics g, Rectangle badge, Image icon)
        {
            using var path = Rounded(badge, 16);
            using (var wb = new SolidBrush(Color.White))
                g.FillPath(wb, path);

            g.SetClip(path);
            var inner = Rectangle.Inflate(badge, -8, -8);
            float ar = icon.Width / (float)icon.Height;
            int w = inner.Width, h = inner.Height;
            if (ar > 1) h = (int)(w / ar); else w = (int)(h * ar);
            int fx = inner.X + (inner.Width - w) / 2;
            int fy = inner.Y + (inner.Height - h) / 2;
            g.DrawImage(icon, fx, fy, w, h);
            g.ResetClip();

            using (var pen = new Pen(Color.FromArgb(60, 0, 0, 0), 1f))
                g.DrawPath(pen, path);
        }

        private static void DrawGem(Graphics g, int cx, int cy, int r, Color accent)
        {
            var pts = new[]
            {
                new Point(cx, cy - r),
                new Point(cx + r, cy),
                new Point(cx, cy + r),
                new Point(cx - r, cy),
            };
            using (var gp = new GraphicsPath())
            {
                gp.AddPolygon(pts);
                using var b = new LinearGradientBrush(new Rectangle(cx - r, cy - r, r * 2, r * 2),
                    Theme.Mix(accent, Color.White, 0.25f), Theme.Mix(accent, Color.Black, 0.25f), 90f);
                g.FillPath(b, gp);
                using var pen = new Pen(Color.FromArgb(200, 255, 255, 255), 2f);
                g.DrawPath(pen, gp);
            }
            // inner facet
            var inner = new[]
            {
                new Point(cx, cy - r / 2),
                new Point(cx + r / 2, cy),
                new Point(cx, cy + r / 2),
                new Point(cx - r / 2, cy),
            };
            using (var ib = new SolidBrush(Color.FromArgb(70, 255, 255, 255)))
                g.FillPolygon(ib, inner);
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
