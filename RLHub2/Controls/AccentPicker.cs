using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using RLHub2.Helpers;

namespace RLHub2.Controls
{
    // Row of accent-color swatches; click to pick. Selected one gets a ring.
    public class AccentPicker : Control
    {
        private int _hover = -1;
        public event EventHandler? AccentSelected;

        private const int Cell = 46;
        private const int R = 15;

        public AccentPicker()
        {
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            Height = 46;
            Width = Theme.Accents.Length * Cell;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

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
            if (idx >= 0)
            {
                Theme.SetAccent(Theme.Accents[idx]);
                AccentSelected?.Invoke(this, EventArgs.Empty);
            }
        }

        private int HitTest(int x)
        {
            int idx = x / Cell;
            return (idx >= 0 && idx < Theme.Accents.Length) ? idx : -1;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int currentArgb = Theme.Accent.ToArgb();
            for (int i = 0; i < Theme.Accents.Length; i++)
            {
                int cx = i * Cell + Cell / 2;
                int cy = Height / 2;
                int r = i == _hover ? R + 2 : R;

                using (var b = new SolidBrush(Theme.Accents[i]))
                    g.FillEllipse(b, cx - r, cy - r, r * 2, r * 2);

                if (Theme.Accents[i].ToArgb() == currentArgb)
                    using (var pen = new Pen(Theme.TextPrimary, 3f))
                        g.DrawEllipse(pen, cx - r - 4, cy - r - 4, (r + 4) * 2, (r + 4) * 2);
            }
        }
    }
}
