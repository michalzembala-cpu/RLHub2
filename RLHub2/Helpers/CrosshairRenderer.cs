using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using RLHub2.Models;

namespace RLHub2.Helpers
{
    // Draws a crosshair into a bitmap. Not CS2's exact geometry — a faithful-enough preview so
    // you can tell two crosshairs apart at a glance, shared by the library cards and the editor.
    public static class CrosshairRenderer
    {
        public static readonly Color Backdrop = Color.FromArgb(14, 16, 20);

        public static Bitmap Render(CrosshairDef x, int box = 96)
        {
            var bmp = new Bitmap(box, box);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.None;
            g.Clear(Backdrop);
            Draw(g, x, box);
            return bmp;
        }

        public static void Draw(Graphics g, CrosshairDef x, int box)
        {
            // Everything scales off a 96px reference so a 200px editor preview looks like the card.
            float s = box / 96f;

            int cx = box / 2, cy = box / 2;
            int len = Math.Clamp((int)Math.Round(x.Size * 5 * s), (int)(4 * s), (int)(40 * s));
            int thk = Math.Clamp((int)Math.Round(x.Thickness * 3 * s) + 1, (int)(2 * s), (int)(7 * s));
            int gap = Math.Clamp((int)Math.Round((x.Gap + 4) * 2.5 * s), 0, (int)(34 * s));
            int ow = Math.Max(1, (int)Math.Round(s));
            var col = Color.FromArgb(Math.Clamp(x.Alpha, 0, 255), x.R, x.G, x.B);

            void Bar(int rx, int ry, int rw, int rh)
            {
                if (x.Outline > 0)
                    using (var ob = new SolidBrush(Color.FromArgb(230, 0, 0, 0)))
                        g.FillRectangle(ob, rx - ow, ry - ow, rw + ow * 2, rh + ow * 2);
                using var b = new SolidBrush(col);
                g.FillRectangle(b, rx, ry, rw, rh);
            }

            Bar(cx - gap - len, cy - thk / 2, len, thk);           // left
            Bar(cx + gap, cy - thk / 2, len, thk);                 // right
            Bar(cx - thk / 2, cy + gap, thk, len);                 // bottom
            if (!x.T) Bar(cx - thk / 2, cy - gap - len, thk, len); // top
            if (x.Dot) Bar(cx - thk / 2, cy - thk / 2, thk, thk);  // centre dot
        }
    }
}
