using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using RLHub2.Helpers;

namespace RLHub2.Controls
{
    // Rounded, gradient dashboard tile.
    // Draws an uppercase title, a large value and an optional subtitle directly
    // in OnPaint (no child labels) so text always sits cleanly on the gradient.
    public class StatTile : Panel
    {
        private string _title = "TITLE";
        private string _value = "--";
        private string _subtitle = "";
        private Color _accent = Color.FromArgb(0, 140, 255);

        // Smoothly animated hover (0 = out, 1 = fully hovered).
        private float _hover;
        private bool _hoverTarget;
        private System.Windows.Forms.Timer? _hoverAnim;

        // Count-up animation for numeric values.
        private string _shownValue = "--";
        private System.Windows.Forms.Timer? _countAnim;
        private int _countFrom, _countTo;
        private float _countT;

        public int CornerRadius { get; set; } = 18;
        public float TitleFontSize { get; set; } = 9.5f;
        public float ValueFontSize { get; set; } = 18f;
        public float SubtitleFontSize { get; set; } = 9f;
        public Image? Icon { get; set; }

        public string Title
        {
            get => _title;
            set { _title = value ?? ""; Invalidate(); }
        }

        // Plain whole numbers count up from the previous value; anything else
        // (dashes, "5 – 3", "74d 18h", percentages…) is shown immediately.
        public string Value
        {
            get => _value;
            set
            {
                var v = value ?? "";
                if (v == _value) return;
                _value = v;

                if (int.TryParse(v, out int target))
                {
                    int from = int.TryParse(_shownValue, out int cur) ? cur : 0;
                    if (from != target) { StartCount(from, target); return; }
                }

                StopCount();
                _shownValue = v;
                Invalidate();
            }
        }

        private void StartCount(int from, int to)
        {
            _countFrom = from;
            _countTo = to;
            _countT = 0f;

            if (_countAnim == null)
            {
                _countAnim = new System.Windows.Forms.Timer { Interval = 16 };
                _countAnim.Tick += (s, e) =>
                {
                    _countT += 0.07f;
                    if (_countT >= 1f)
                    {
                        _countT = 1f;
                        _countAnim!.Stop();
                    }
                    float eased = 1f - (float)Math.Pow(1 - _countT, 3); // ease-out cubic
                    int val = (int)Math.Round(_countFrom + (_countTo - _countFrom) * eased);
                    _shownValue = val.ToString();
                    Invalidate();
                };
            }
            _countAnim.Start();
        }

        private void StopCount()
        {
            _countAnim?.Stop();
        }

        public string Subtitle
        {
            get => _subtitle;
            set { _subtitle = value ?? ""; Invalidate(); }
        }

        public Color Accent
        {
            get => _accent;
            set { _accent = value; Invalidate(); }
        }

        public StatTile()
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            Size = new Size(240, 92);
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        }

        private void EnsureHoverAnim()
        {
            if (_hoverAnim != null) return;
            _hoverAnim = new System.Windows.Forms.Timer { Interval = 15 };
            _hoverAnim.Tick += (s, e) =>
            {
                float target = _hoverTarget ? 1f : 0f;
                float d = target - _hover;
                if (Math.Abs(d) < 0.02f) { _hover = target; _hoverAnim!.Stop(); }
                else _hover += d * 0.28f; // ease-out
                Invalidate();
            };
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            EnsureHoverAnim();
            _hoverTarget = true;
            _hoverAnim!.Start();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            EnsureHoverAnim();
            _hoverTarget = false;
            _hoverAnim!.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _hoverAnim?.Stop(); _hoverAnim?.Dispose();
                _countAnim?.Stop(); _countAnim?.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = RoundedRect(rect, CornerRadius);

            // Background gradient: surface tinted by the accent at the top.
            var top = Theme.Mix(Theme.CardTop, _accent, 0.16f);
            var bottom = Theme.CardBottom;
            using (var brush = new LinearGradientBrush(rect, top, bottom, 90f))
                g.FillPath(brush, path);

            // Accent stripe down the left edge (clipped to the rounded shape).
            g.SetClip(path);
            using (var stripe = new SolidBrush(_accent))
                g.FillRectangle(stripe, 0, 0, 6, Height);
            g.ResetClip();

            // Border / hover glow — alpha and thickness ease in with the hover animation.
            int borderAlpha = (int)(70 + (255 - 70) * _hover);
            using (var pen = new Pen(Color.FromArgb(borderAlpha, _accent), 1f + _hover))
                g.DrawPath(pen, path);

            // Text block.
            using var titleFont = new Font("Segoe UI", TitleFontSize, FontStyle.Bold);
            using var subFont = new Font("Segoe UI", SubtitleFontSize, FontStyle.Regular);

            using var titleBrush = new SolidBrush(Theme.TextSecondary);
            using var valueBrush = new SolidBrush(Theme.TextPrimary);
            using var subBrush = new SolidBrush(Theme.TextMuted);

            int padX = 22;

            // Optional rank icon at the left. No white chip behind it — the rank art brings its
            // own dark background, so the chip only ringed every icon in bright white.
            if (Icon != null)
            {
                int bs = Math.Min(Height - 24, 56);
                var badge = new Rectangle(16, (Height - bs) / 2, bs, bs);
                using var bp = RoundedRect(badge, 12);
                g.SetClip(bp);
                var inner = Rectangle.Inflate(badge, -6, -6);
                float ar = Icon.Width / (float)Icon.Height;
                int w = inner.Width, h = inner.Height;
                if (ar > 1) h = (int)(w / ar); else w = (int)(h * ar);
                g.DrawImage(Icon, inner.X + (inner.Width - w) / 2, inner.Y + (inner.Height - h) / 2, w, h);
                g.ResetClip();
                padX = badge.Right + 14;
            }

            // Title pinned to the top, subtitle pinned to the BOTTOM, value centred in the
            // gap between them — and the value font shrinks if that gap is too small, so a
            // short tile can never clip the subtitle or overlap the value on top of it.
            int titleY = 14;
            g.DrawString(_title.ToUpperInvariant(), titleFont, titleBrush, padX, titleY);

            bool hasSub = !string.IsNullOrEmpty(_subtitle);
            float subH = hasSub ? subFont.Height : 0;
            float subY = Height - subH - 10;

            float valTop = titleY + titleFont.Height + 2;
            float valBottom = hasSub ? subY - 2 : Height - 8;
            float valArea = Math.Max(14f, valBottom - valTop);

            using var valueFont = FitFont(ValueFontSize, valArea);
            float valY = valTop + Math.Max(0, (valArea - valueFont.Height) / 2f);
            g.DrawString(_shownValue, valueFont, valueBrush, padX - 2, valY);

            if (hasSub)
                g.DrawString(_subtitle, subFont, subBrush, padX, subY);

            base.OnPaint(e);
        }

        // Largest bold "Segoe UI" at or below `size` whose line height fits `available`.
        private static Font FitFont(float size, float available)
        {
            var f = new Font("Segoe UI", size, FontStyle.Bold);
            while (f.Height > available && size > 9f)
            {
                f.Dispose();
                size -= 1f;
                f = new Font("Segoe UI", size, FontStyle.Bold);
            }
            return f;
        }

        private static Color Mix(Color a, Color b, float t)
        {
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
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
