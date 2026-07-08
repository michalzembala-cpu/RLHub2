using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace RLHub2.Helpers
{
    // Loads arena background images from Resources\ (copied to output) and paints
    // them "cover"-fitted and dimmed toward the page background, as a subtle backdrop.
    public static class ArenaBackground
    {
        private static readonly Dictionary<string, Image?> Cache = new();

        public static Image? Load(string file)
        {
            if (string.IsNullOrEmpty(file)) return null;
            if (Cache.TryGetValue(file, out var cached)) return cached;

            Image? result = null;
            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, "Resources", file);
                if (File.Exists(path))
                    result = Image.FromFile(path);
            }
            catch { }
            Cache[file] = result;
            return result;
        }

        public static void Paint(Graphics g, int width, int height, Image? img, bool isDark)
        {
            g.Clear(Theme.PageBg);
            if (img == null || width < 2 || height < 2) return;

            // "cover" fit — fill the whole control, cropping overflow
            float ir = img.Width / (float)img.Height;
            float cr = width / (float)height;
            Rectangle dst;
            if (ir > cr) { int h = height; int w = (int)(h * ir); dst = new Rectangle((width - w) / 2, 0, w, h); }
            else { int w = width; int h = (int)(w / ir); dst = new Rectangle(0, (height - h) / 2, w, h); }
            g.DrawImage(img, dst);

            // dim toward the page background so it stays a readable backdrop
            using var ov = new SolidBrush(Color.FromArgb(isDark ? 165 : 205, Theme.PageBg));
            g.FillRectangle(ov, 0, 0, width, height);
        }
    }
}
