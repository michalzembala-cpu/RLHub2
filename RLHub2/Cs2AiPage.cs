using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using RLHub2.Helpers;
using RLHub2.Services;

namespace RLHub2
{
    // Findings drawn from the CS2 matches we logged. Everything shown here is computed from
    // our own data — nothing is fetched, nothing is guessed.
    public class Cs2AiPage : Controls.ArenaControl
    {
        protected override string ArenaFile => "cs2_bg.png";

        private readonly SettingsStore _settings = new();
        private Cs2Report _report = new();

        private readonly Label _title = new();
        private readonly Label _headline = new();
        private readonly Panel _body = new();

        public Cs2AiPage()
        {
            BackColor = Theme.PageBg;
            Dock = DockStyle.Fill;
            Padding = new Padding(20);

            _title.Text = Localization.IsPolish ? "AI INSIGHTS" : "AI INSIGHTS";
            _title.Dock = DockStyle.Top;
            _title.Height = 46;
            _title.ForeColor = Theme.TextPrimary;
            _title.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
            _title.TextAlign = ContentAlignment.MiddleLeft;

            _headline.Dock = DockStyle.Top;
            _headline.Height = 30;
            _headline.ForeColor = Theme.TextSecondary;
            _headline.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            _headline.TextAlign = ContentAlignment.MiddleLeft;

            _body.Dock = DockStyle.Fill;
            _body.BackColor = Color.Transparent;
            _body.Paint += DrawBody;
            _body.Resize += (s, e) => _body.Invalidate();

            Controls.Add(_body);
            Controls.Add(_headline);
            Controls.Add(_title);

            Load += (s, e) => Refresh2();
        }

        private void Refresh2()
        {
            _report = Cs2Insights.Build(_settings.LoadCs2ModeFilter());
            _headline.Text = _report.Headline;
            _body.Invalidate();
        }

        private void DrawBody(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int W = _body.Width, H = _body.Height;
            if (W < 60 || H < 60) return;

            int y = 4;

            if (_report.HasData)
            {
                y = DrawSummary(g, W, y);
                y += 14;
            }

            foreach (var ins in _report.Insights)
            {
                if (y > H - 40) break;
                y = DrawCard(g, W, y, ins);
                y += 12;
            }
        }

        // The headline numbers for the mode being analysed.
        private int DrawSummary(Graphics g, int W, int y)
        {
            var rect = new Rectangle(0, y, W - 2, 92);
            using (var p = Round(rect, 16))
            {
                using var bg = new LinearGradientBrush(rect, Theme.CardTop, Theme.CardBottom, 90f);
                g.FillPath(bg, p);
                using var pen = new Pen(Color.FromArgb(55, Theme.Accent), 1f);
                g.DrawPath(pen, p);
            }

            var items = new (string Label, string Value)[]
            {
                (Localization.IsPolish ? "MECZE" : "MATCHES", _report.Matches.ToString()),
                (Localization.IsPolish ? "WYGRANE" : "WINS", _report.WinPct + "%"),
                ("K/D", _report.Kd.ToString("0.00")),
                ("ADR", _report.Adr > 0 ? _report.Adr.ToString("0") : "—"),
                ("HS%", _report.HsPct > 0 ? _report.HsPct.ToString("0") + "%" : "—"),
            };

            using var labelFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            using var valueFont = new Font("Segoe UI", 20f, FontStyle.Bold);
            using var labelBrush = new SolidBrush(Theme.TextMuted);
            using var valueBrush = new SolidBrush(Theme.TextPrimary);

            int cell = (W - 40) / items.Length;
            for (int i = 0; i < items.Length; i++)
            {
                int x = 20 + i * cell;
                g.DrawString(items[i].Label, labelFont, labelBrush, x, y + 18);
                g.DrawString(items[i].Value, valueFont, valueBrush, x - 2, y + 38);
            }

            return rect.Bottom;
        }

        private int DrawCard(Graphics g, int W, int y, Insight ins)
        {
            using var titleFont = new Font("Segoe UI", 11f, FontStyle.Bold);
            using var textFont = new Font("Segoe UI", 10.5f);

            var accent = ins.Good ? Color.FromArgb(46, 204, 113) : Color.FromArgb(222, 130, 40);

            // Height follows the wrapped text — an insight is one or three lines depending on
            // what it found, and a fixed height would either clip it or leave a hole.
            var textRect = new RectangleF(58, 0, W - 80, 1000);
            var textSize = g.MeasureString(ins.Text, textFont, (int)textRect.Width);
            int h = Math.Max(70, (int)textSize.Height + 46);

            var rect = new Rectangle(0, y, W - 2, h);
            using (var p = Round(rect, 16))
            {
                using var bg = new LinearGradientBrush(rect, Theme.CardTop, Theme.CardBottom, 90f);
                g.FillPath(bg, p);
                using var pen = new Pen(Color.FromArgb(55, accent), 1f);
                g.DrawPath(pen, p);
            }

            // accent stripe
            using (var stripe = new SolidBrush(accent))
                g.FillRectangle(stripe, rect.X + 1, rect.Y + 12, 4, rect.Height - 24);

            using (var b = new SolidBrush(accent))
                g.DrawString(ins.Title.ToUpperInvariant(), titleFont, b, 20, rect.Y + 14);
            using (var b = new SolidBrush(Theme.TextSecondary))
                g.DrawString(ins.Text, textFont, b, new RectangleF(20, rect.Y + 36, W - 44, rect.Height - 44));

            return rect.Bottom;
        }

        private static GraphicsPath Round(Rectangle r, int radius)
        {
            int d = radius * 2;
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseAllFigures();
            return p;
        }
    }
}
