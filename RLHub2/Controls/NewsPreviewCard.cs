using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using RLHub2.Helpers;

namespace RLHub2.Controls
{
    // Rounded card that previews the latest news (title + category), drawn in OnPaint.
    // Fonts and spacing scale with the card size so the text fills the whole tile.
    // The "OPEN NEWS" button is a sibling control placed by the page, not part of the card.
    public class NewsPreviewCard : Panel
    {
        private IReadOnlyList<(string Title, string Category)> _items = Array.Empty<(string, string)>();

        public int CornerRadius { get; set; } = 18;
        public Color Accent { get; set; } = Color.FromArgb(150, 90, 255);
        public string HeaderText { get; set; } = "LATEST NEWS";

        public NewsPreviewCard()
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            Size = new Size(296, 300);
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        }

        public void SetItems(IReadOnlyList<(string Title, string Category)> items)
        {
            _items = items ?? Array.Empty<(string, string)>();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = RoundedRect(rect, CornerRadius);

            using (var brush = new LinearGradientBrush(rect, Theme.CardTop, Theme.CardBottom, 90f))
                g.FillPath(brush, path);

            using (var pen = new Pen(Color.FromArgb(70, Accent), 1f))
                g.DrawPath(pen, path);

            int padX = 24;

            // Header scales with the card height.
            float headerSize = Clamp(Height * 0.058f, 12f, 18f);
            using var headerFont = new Font("Segoe UI", headerSize, FontStyle.Bold);
            using var headerBrush = new SolidBrush(Accent);
            g.DrawString(HeaderText, headerFont, headerBrush, padX, 18);

            int contentTop = 18 + headerFont.Height + 10;
            int contentBottom = Height - 16;

            if (_items.Count == 0)
            {
                using var emptyFont = new Font("Segoe UI", 11f, FontStyle.Italic);
                using var emptyBrush = new SolidBrush(Theme.TextMuted);
                g.DrawString("Loading news...", emptyFont, emptyBrush, padX, contentTop);
                base.OnPaint(e);
                return;
            }

            // Fit the items to the card: shrink the titles to one line and, if still too
            // tight, drop items — otherwise a short card made the blocks overlap.
            int count = Math.Min(3, _items.Count);
            int titleLines = 2;
            int rowH;
            Font titleFont, catFont;

            while (true)
            {
                rowH = Math.Max(1, (contentBottom - contentTop) / count);
                titleFont = new Font("Segoe UI", Clamp(rowH * 0.26f, 13f, 24f), FontStyle.Bold);
                catFont = new Font("Segoe UI", Clamp(rowH * 0.16f, 9f, 14f), FontStyle.Regular);

                int blockH = titleFont.Height * titleLines + 2 + catFont.Height;
                if (blockH <= rowH) break;

                titleFont.Dispose();
                catFont.Dispose();

                if (titleLines == 2) { titleLines = 1; continue; }
                if (count > 1) { count--; titleLines = 2; continue; }

                // single item, single line — accept whatever fits
                titleFont = new Font("Segoe UI", 13f, FontStyle.Bold);
                catFont = new Font("Segoe UI", 9f, FontStyle.Regular);
                break;
            }
            using var titleBrush = new SolidBrush(Theme.TextPrimary);
            using var catBrush = new SolidBrush(Theme.TextMuted);
            using var bulletBrush = new SolidBrush(Accent);

            var titleFormat = new StringFormat
            {
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.LineLimit
            };

            int bullet = Math.Max(8, rowH / 10);
            int textLeft = padX + bullet + 12;
            int textWidth = Width - textLeft - 18;

            for (int i = 0; i < count; i++)
            {
                int rowTop = contentTop + i * rowH;

                int titleH = titleFont.Height * titleLines;
                int catH = catFont.Height;
                int blockH = titleH + 2 + catH;
                int startY = rowTop + Math.Max(0, (rowH - blockH) / 2);

                g.FillEllipse(bulletBrush, padX, startY + titleFont.Height / 2 - bullet / 2, bullet, bullet);

                g.DrawString(_items[i].Title, titleFont, titleBrush,
                    new RectangleF(textLeft, startY, textWidth, titleH), titleFormat);

                if (!string.IsNullOrEmpty(_items[i].Category))
                    g.DrawString(_items[i].Category.ToUpperInvariant(), catFont, catBrush,
                        textLeft, startY + titleH + 2);
            }

            titleFont.Dispose();
            catFont.Dispose();

            base.OnPaint(e);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
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
