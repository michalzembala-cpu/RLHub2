using System;
using System.Drawing;
using System.Windows.Forms;
using RLHub2.Helpers;

namespace RLHub2.Controls
{
    // Base class for full-page UserControls. Paints a dimmed Rocket League arena
    // behind the page, and makes page-background containers transparent so the
    // arena shows through in the margins and gaps (cards stay solid).
    public class ArenaControl : UserControl
    {
        // Override in a page to pick which arena shows behind it.
        protected virtual string ArenaFile => "stadion1.jpg";

        // Cached, pre-dimmed arena bitmap. During a resize we draw this stretched (cheap)
        // and rebuild the crisp version once the size settles — keeps resize animations smooth.
        private Bitmap? _bgCache;
        private readonly System.Windows.Forms.Timer _rebuildTimer;

        public ArenaControl()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);
            Theme.ThemeChanged += OnThemeChanged;

            _rebuildTimer = new System.Windows.Forms.Timer { Interval = 60 };
            _rebuildTimer.Tick += (s, e) =>
            {
                _rebuildTimer.Stop();
                if (_bgCache == null || _bgCache.Width != Width || _bgCache.Height != Height)
                    RebuildCache();
            };
        }

        private void OnThemeChanged()
        {
            if (IsDisposed) return;
            Transparentify(this);
            _bgCache?.Dispose();
            _bgCache = null;
            Invalidate(true);
        }

        private void RebuildCache()
        {
            if (Width < 2 || Height < 2) return;
            var bmp = new Bitmap(Width, Height);
            using (var g = Graphics.FromImage(bmp))
                ArenaBackground.Paint(g, bmp.Width, bmp.Height, ArenaBackground.Load(ArenaFile), Theme.IsDark);
            var old = _bgCache;
            _bgCache = bmp;
            old?.Dispose();
            Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (Width < 2 || Height < 2) return;
            var g = e.Graphics;

            if (_bgCache == null) RebuildCache();
            if (_bgCache == null) return;

            if (_bgCache.Width == Width && _bgCache.Height == Height)
            {
                g.DrawImageUnscaled(_bgCache, 0, 0);
            }
            else
            {
                // in-between frame during a resize: stretch the cached bitmap (cheap),
                // and debounce a crisp rebuild for when the size stops changing
                g.DrawImage(_bgCache, new Rectangle(0, 0, Width, Height));
                _rebuildTimer.Stop();
                _rebuildTimer.Start();
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Transparentify(this);
        }

        // Turn only the page-background-colored layout containers transparent.
        // Cards/tiles (Surface color or custom paint) and grids are left untouched.
        private static void Transparentify(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                bool isContainer = c is TableLayoutPanel || c is FlowLayoutPanel ||
                                   (c is Panel && c.GetType() == typeof(Panel));
                if (isContainer && c.BackColor == Theme.PageBg)
                    c.BackColor = Color.Transparent;

                if (c.HasChildren)
                    Transparentify(c);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Theme.ThemeChanged -= OnThemeChanged;
                _rebuildTimer?.Stop();
                _rebuildTimer?.Dispose();
                _bgCache?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
