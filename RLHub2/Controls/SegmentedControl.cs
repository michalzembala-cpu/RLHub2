using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using RLHub2.Helpers;

namespace RLHub2.Controls
{
    // Modern animated segmented toggle (pill with a sliding accent highlight).
    public class SegmentedControl : Control
    {
        private string[] _items = Array.Empty<string>();
        private int _selected = 0;
        private int _hover = -1;

        private float _animX;
        private float _targetX;
        private readonly System.Windows.Forms.Timer _anim;

        public event EventHandler? SelectedIndexChanged;

        public int SelectedIndex => _selected;

        public SegmentedControl()
        {
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            Height = 46;
            Width = 320;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;

            _anim = new System.Windows.Forms.Timer { Interval = 15 };
            _anim.Tick += (s, e) =>
            {
                float diff = _targetX - _animX;
                if (Math.Abs(diff) < 0.5f) { _animX = _targetX; _anim.Stop(); }
                else _animX += diff * 0.25f;
                Invalidate();
            };
        }

        public void SetOptions(string[] items)
        {
            _items = items ?? Array.Empty<string>();
            if (_selected >= _items.Length) _selected = 0;
            _targetX = _animX = SegLeft(_selected);
            Invalidate();
        }

        // Set selection without raising the event (initial state).
        public void SetSelectedSilent(int index)
        {
            if (_items.Length == 0) return;
            _selected = Math.Max(0, Math.Min(_items.Length - 1, index));
            _targetX = _animX = SegLeft(_selected);
            Invalidate();
        }

        private float SegW => _items.Length > 0 ? (Width - 8f) / _items.Length : Width;
        private float SegLeft(int i) => 4f + i * SegW;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int idx = HitTest(e.X);
            if (idx != _hover) { _hover = idx; Invalidate(); }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = -1;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            int idx = HitTest(e.X);
            if (idx >= 0 && idx != _selected)
            {
                _selected = idx;
                _targetX = SegLeft(idx);
                _anim.Start();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private int HitTest(int x)
        {
            if (_items.Length == 0) return -1;
            int idx = (int)((x - 4f) / SegW);
            return Math.Max(0, Math.Min(_items.Length - 1, idx));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            int radius = Height / 2;

            // track
            using (var path = Rounded(rect, radius))
            using (var b = new SolidBrush(Theme.SurfaceAlt))
                g.FillPath(b, path);

            if (_items.Length == 0) return;

            // sliding highlight
            var hi = new RectangleF(_animX, 4, SegW, Height - 8);
            using (var hpath = Rounded(Rectangle.Round(hi), (Height - 8) / 2))
            using (var hb = new SolidBrush(Theme.Accent))
                g.FillPath(hb, hpath);

            // labels
            using var font = new Font("Segoe UI", 11f, FontStyle.Bold);
            var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            for (int i = 0; i < _items.Length; i++)
            {
                var segRect = new RectangleF(SegLeft(i), 4, SegW, Height - 8);
                Color col = i == _selected ? Color.White
                    : (i == _hover ? Theme.TextPrimary : Theme.TextSecondary);
                using var br = new SolidBrush(col);
                g.DrawString(_items[i], font, br, segRect, fmt);
            }
        }

        private static GraphicsPath Rounded(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.Left, r.Top, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            path.CloseAllFigures();
            return path;
        }
    }
}
