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
            Invalidate(true);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            ArenaBackground.Paint(e.Graphics, Width, Height,
                ArenaBackground.Load(ArenaFile), Theme.IsDark);
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
            if (disposing) Theme.ThemeChanged -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }
}
