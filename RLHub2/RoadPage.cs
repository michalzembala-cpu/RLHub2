using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using RLHub2.Helpers;
using RLHub2.Models;
using RLHub2.Services;

namespace RLHub2
{
    public partial class RoadPage : Controls.ArenaControl
    {
        protected override string ArenaFile => "rl_bg.png";

        private readonly MmrStore _store = new();
        private List<MmrEntry> _entries = new();

        private static readonly (string Name, int Mmr)[] Tiers =
        {
            ("Bronze", 0), ("Silver", 400), ("Gold", 550), ("Platinum", 700),
            ("Diamond", 850), ("Champion", 1000), ("Grand Champion", 1200), ("Supersonic Legend", 1400)
        };

        private bool _hasData;
        private int _peak;
        private int _current;
        private int _curIdx;

        public RoadPage()
        {
            InitializeComponent();
            ApplyLanguage();

            modeSeg.SetOptions(new[] { "1v1", "2v2", "3v3" });
            modeSeg.SetSelectedSilent(1);
            modeSeg.SelectedIndexChanged += (s, e) => Recompute();

            ladderPanel.Paint += DrawLadder;
            ladderPanel.Resize += (s, e) => ladderPanel.Invalidate();

            Load += (s, e) => { _entries = _store.LoadForActive(); Recompute(); };
        }

        private void ApplyLanguage()
        {
            lblTitle.Text = Localization.T("road_title");
        }

        private string CurrentMode() => modeSeg.SelectedIndex switch { 0 => "1v1", 2 => "3v3", _ => "2v2" };

        private void Recompute()
        {
            var list = _entries.Where(e => e.Mode == CurrentMode()).OrderBy(e => e.Timestamp).ToList();
            _hasData = list.Count > 0;
            _current = _hasData ? list[^1].Value : 0;
            _peak = _hasData ? list.Max(e => e.Value) : 0;

            _curIdx = 0;
            for (int i = 0; i < Tiers.Length; i++)
                if (_current >= Tiers[i].Mmr) _curIdx = i;

            var next = Tiers.FirstOrDefault(t => t.Mmr > _current);

            if (!_hasData)
            {
                lblStats.Text = Localization.T("road_empty");
            }
            else if (next.Name == null)
            {
                lblStats.Text = $"{Localization.T("road_maxed")}  ({_current})";
            }
            else
            {
                var wk = list.Where(e => e.Timestamp >= DateTime.Now.AddDays(-7)).ToList();
                int pace = wk.Count > 0 ? _current - wk[0].Value : 0;
                int toNext = next.Mmr - _current;
                string eta = pace > 0 ? $"~{(int)Math.Ceiling(toNext / (double)pace)} {Localization.T("road_wk")}" : "—";
                lblStats.Text =
                    $"{Localization.T("road_current")} {_current}    " +
                    $"{Localization.T("road_tonext")} {toNext}    " +
                    $"{Localization.T("road_pace")} {(pace > 0 ? "+" : "")}{pace}/{Localization.T("road_wk")}    " +
                    $"{Localization.T("road_eta")} {eta}";
            }

            ladderPanel.Invalidate();
        }

        private static GraphicsPath RoundRect(Rectangle r, int radius)
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

        private void DrawLadder(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int n = Tiers.Length;
            int W = ladderPanel.Width, H = ladderPanel.Height;
            int pad = 60;
            if (W - 2 * pad < 100 || H < 120) return;

            // translucent "glass" panel so the arena backdrop shows through (matches the cards)
            using (var panelPath = RoundRect(new Rectangle(0, 0, W - 1, H - 1), 20))
            {
                using (var glass = new LinearGradientBrush(new Rectangle(0, 0, W, H), Theme.CardTop, Theme.CardBottom, 90f))
                    g.FillPath(glass, panelPath);
                using (var border = new Pen(Color.FromArgb(60, Theme.Accent), 1f))
                    g.DrawPath(border, panelPath);
            }

            float step = (W - 2 * pad) / (float)(n - 1);
            int bs = (int)Math.Max(56, Math.Min(112, step * 0.72f)); // badge diameter
            int cy = H / 2;                                          // vertical center of the row

            using var nameFont = new Font("Segoe UI", 13.5f, FontStyle.Bold);
            using var mmrFont = new Font("Segoe UI", 10.5f);
            using var centerTop = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near };
            using var centerBottom = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Far };

            var accent = Theme.Accent;
            var gray = Theme.IsDark ? Color.FromArgb(70, 74, 100) : Color.FromArgb(200, 205, 215);

            // connectors first (behind badges): Bronze(left) -> SSL(right)
            for (int i = 0; i < n - 1; i++)
            {
                float x1 = pad + i * step, x2 = pad + (i + 1) * step;
                bool lineDone = _peak >= Tiers[i + 1].Mmr;
                using var lp = new Pen(lineDone ? accent : gray, 4f);
                g.DrawLine(lp, x1 + bs / 2f, cy, x2 - bs / 2f, cy);
            }

            for (int i = 0; i < n; i++)
            {
                float cx = pad + i * step;
                bool done = _peak >= Tiers[i].Mmr;
                bool cur = i == _curIdx;

                var badge = new RectangleF(cx - bs / 2f, cy - bs / 2f, bs, bs);
                var icon = RankIcons.Get(Tiers[i].Name);

                // soft glow behind current tier
                if (cur)
                    using (var glow = new SolidBrush(Color.FromArgb(60, accent)))
                    using (var gp = RoundedSquare(RectangleF.Inflate(badge, 10, 10), 26))
                        g.FillPath(glow, gp);

                // Rounded square, not a circle: the wider emblems (Grand Champion, Supersonic
                // Legend) have wings that a circular clip sliced off.
                using (var bp = RoundedSquare(badge, 20))
                {
                    // No white disc behind the icon: the rank art carries its own dark
                    // background, so the disc only framed each badge in a bright ring.
                    if (icon != null)
                    {
                        g.SetClip(bp);
                        var inner = RectangleF.Inflate(badge, -5, -5);
                        float ar = icon.Width / (float)icon.Height;
                        float w = inner.Width, h = inner.Height;
                        if (ar > 1) h = w / ar; else w = h * ar;
                        g.DrawImage(icon, inner.X + (inner.Width - w) / 2, inner.Y + (inner.Height - h) / 2, w, h);
                        g.ResetClip();
                    }
                    if (!done)
                        using (var ov = new SolidBrush(Color.FromArgb(165, Theme.Surface)))
                            g.FillPath(ov, bp);
                    using (var pen = new Pen(cur ? accent : Color.FromArgb(120, gray), cur ? 4f : 2f))
                        g.DrawPath(pen, bp);
                }

                // name above, MMR below — centered under the column
                var nameRect = new RectangleF(cx - step / 2, cy - bs / 2f - 52, step, 46);
                using (var nb = new SolidBrush(done ? Theme.TextPrimary : Theme.TextMuted))
                    g.DrawString(Tiers[i].Name, nameFont, nb, nameRect, centerBottom);

                var mmrRect = new RectangleF(cx - step / 2, cy + bs / 2f + 8, step, 26);
                using (var mb = new SolidBrush(cur ? accent : Theme.TextMuted))
                    g.DrawString($"≥ {Tiers[i].Mmr}", mmrFont, mb, mmrRect, centerTop);
            }
        }

        private static GraphicsPath RoundedSquare(RectangleF r, float radius)
        {
            float d = radius * 2;
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
