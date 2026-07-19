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
        protected virtual string ArenaFile => "rl_bg.png";

        // The arena, scaled and dimmed once per size. Rebuilt only when the size actually
        // changes; every paint after that is a plain blit.
        private Bitmap? _bgCache;

        public ArenaControl()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);
            Theme.ThemeChanged += OnThemeChanged;
        }

        private void OnThemeChanged()
        {
            if (IsDisposed) return;
            Transparentify(this);
            _bgCache?.Dispose();
            _bgCache = null;
            Invalidate(true);
        }

        // No Invalidate() here: this is called from inside OnPaintBackground, and asking for
        // another paint from within a paint would loop.
        private void RebuildCache()
        {
            if (Width < 2 || Height < 2) return;
            var bmp = new Bitmap(Width, Height);
            using (var g = Graphics.FromImage(bmp))
                ArenaBackground.Paint(g, bmp.Width, bmp.Height, ArenaBackground.Load(ArenaFile), Theme.IsDark);
            var old = _bgCache;
            _bgCache = bmp;
            old?.Dispose();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (Width < 2 || Height < 2) return;

            // This runs once per TRANSPARENT CHILD, not once per frame: WinForms implements
            // transparency by having the parent repaint the child's slice of background. With a
            // handful of transparent layout panels on a page that is many calls per frame, so
            // scaling the whole arena here (~16 ms a go) cost hundreds of ms per frame and the
            // animations crawled. Scale once into a cache, then blit only the strip asked for.
            if (_bgCache == null || _bgCache.Width != Width || _bgCache.Height != Height)
                RebuildCache();
            if (_bgCache == null) return;

            var clip = Rectangle.Round(e.Graphics.VisibleClipBounds);
            clip.Intersect(new Rectangle(0, 0, _bgCache.Width, _bgCache.Height));
            if (clip.Width <= 0 || clip.Height <= 0) return;

            e.Graphics.DrawImage(_bgCache, clip, clip, GraphicsUnit.Pixel);
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
                _bgCache?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
