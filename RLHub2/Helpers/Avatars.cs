using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace RLHub2.Helpers
{
    // Per-account avatar images. The user can drop any picture on a profile tile; we keep
    // a copy in %LocalAppData%\RLHub2\avatars\<name>.png. Accounts without one get a
    // generated tile (accent gradient + initials) so the picker never looks empty.
    public static class Avatars
    {
        private static readonly Dictionary<string, Image?> Cache = new(StringComparer.OrdinalIgnoreCase);

        public static string Dir { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RLHub2", "avatars");

        private static string PathFor(string account)
        {
            var safe = account;
            foreach (var c in Path.GetInvalidFileNameChars()) safe = safe.Replace(c, '_');
            return Path.Combine(Dir, safe + ".png");
        }

        public static Image? Load(string account)
        {
            if (string.IsNullOrWhiteSpace(account)) return null;
            if (Cache.TryGetValue(account, out var cached)) return cached;

            Image? img = null;
            try
            {
                var p = PathFor(account);
                if (File.Exists(p))
                {
                    // copy through a stream so the file isn't locked and can be replaced later
                    using var fs = File.OpenRead(p);
                    using var tmp = Image.FromStream(fs);
                    img = new Bitmap(tmp);
                }
            }
            catch { }

            Cache[account] = img;
            return img;
        }

        // Copy a picture the user picked into the avatars folder, square-cropped to 256px.
        public static bool Set(string account, string sourceFile)
        {
            try
            {
                Directory.CreateDirectory(Dir);
                using var src = Image.FromFile(sourceFile);

                const int size = 256;
                using var square = new Bitmap(size, size);
                using (var g = Graphics.FromImage(square))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    int side = Math.Min(src.Width, src.Height);
                    var crop = new Rectangle((src.Width - side) / 2, (src.Height - side) / 2, side, side);
                    g.DrawImage(src, new Rectangle(0, 0, size, size), crop, GraphicsUnit.Pixel);
                }

                if (Cache.TryGetValue(account, out var old)) old?.Dispose();
                Cache.Remove(account);
                square.Save(PathFor(account), System.Drawing.Imaging.ImageFormat.Png);
                return true;
            }
            catch { return false; }
        }

        // Deterministic color per account so the same profile always looks the same.
        public static Color ColorFor(string account)
        {
            int h = 0;
            foreach (var c in account ?? "") h = h * 31 + char.ToLowerInvariant(c);
            var palette = new[]
            {
                Color.FromArgb(120, 60, 255), Color.FromArgb(40, 150, 230),
                Color.FromArgb(30, 190, 160), Color.FromArgb(230, 120, 40),
                Color.FromArgb(225, 70, 130), Color.FromArgb(60, 190, 90),
            };
            return palette[Math.Abs(h) % palette.Length];
        }

        public static string Initials(string account)
        {
            account = (account ?? "").Trim();
            if (account.Length == 0) return "?";
            return account.Substring(0, 1).ToUpperInvariant();
        }
    }
}
