using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace RLHub2.Services
{
    // Reads text off the screen with the OCR engine built into Windows (no external library).
    // Stage 1 is calibration only: capture the screen, OCR it, and dump every token it found with
    // its position, so the real "which number is which playlist's MMR" logic can be built on real
    // data from the user's own resolution.
    public static class ScreenMmr
    {
        public sealed class Token
        {
            public string Text = "";
            public double X, Y, W, H;   // bounding box in screen pixels
        }

        public static string DebugDir { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RLHub2", "ocr");

        // What one screen read produced.
        public sealed class Reading
        {
            // "1v1" / "2v2" / "3v3" -> MMR, read from the top row of Rocket League's Play menu.
            public Dictionary<string, int> Standard = new();
            // Every MMR-plausible number found (all playlists incl. extra modes) — for diagnostics.
            public List<int> AllTiles = new();
        }

        // Capture the primary screen, OCR it, and pull out the standard-playlist MMRs. Also writes
        // ocr_capture.png + ocr_debug.txt so a missed read can be diagnosed.
        public static async Task<Reading> ReadAsync()
        {
            Directory.CreateDirectory(DebugDir);

            using var full = CapturePrimaryScreen();
            try { full.Save(Path.Combine(DebugDir, "ocr_capture.png"), ImageFormat.Png); } catch { }

            // The tile MMRs are white numbers on colourful art, so OCR often misses them at native
            // size (contrast). Crop the Play menu's top-row band and upscale it 3x so they read
            // reliably, then take the numbers left to right: 3v3, 2v2, 1v1 (RL's fixed order).
            int top = (int)(full.Height * 0.235);
            int h = (int)(full.Height * 0.055);
            using var band = CropUpscale(full, new Rectangle(0, top, full.Width, h), 3);

            var tokens = await OcrAsync(band);
            DumpTokens(band, tokens);
            return ParseRow(tokens);
        }

        private static Bitmap CropUpscale(Bitmap src, Rectangle r, int scale)
        {
            r.Intersect(new Rectangle(0, 0, src.Width, src.Height));
            var b = new Bitmap(Math.Max(1, r.Width * scale), Math.Max(1, r.Height * scale), PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(b);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(src, new Rectangle(0, 0, b.Width, b.Height), r, GraphicsUnit.Pixel);
            return b;
        }

        // The band we OCR is just the Play menu's top row, so the MMR-plausible numbers are already
        // 3v3, 2v2, 1v1 left to right (RL's fixed order), with extra modes / an FPS overlay falling
        // to their right. Read them by X and take the first three.
        private static Reading ParseRow(List<Token> tokens)
        {
            var cand = new List<(int val, double x)>();
            foreach (var t in tokens)
            {
                string d = OnlyDigits(t.Text);
                if (d.Length < 3 || d.Length > 4) continue;      // MMR is 3–4 digits
                if (!int.TryParse(d, out int v)) continue;
                if (v < 100 || v > 2500) continue;               // excludes players-online, level, labels
                cand.Add((v, t.X));
            }

            var r = new Reading { AllTiles = cand.Select(c => c.val).ToList() };
            var ordered = cand.OrderBy(c => c.x).ToList();
            string[] order = { "3v3", "2v2", "1v1" };
            for (int i = 0; i < order.Length && i < ordered.Count; i++)
                r.Standard[order[i]] = ordered[i].val;
            return r;
        }

        // For the automatic post-match read: the end-of-match scoreboard shows the player's MMR
        // right next to their name. Capture, find the account's name token, and take the MMR number
        // on the same row just to its left. Returns null if the scoreboard isn't up (e.g. casual,
        // or the player alt-tabbed away) — never guesses.
        public static async Task<int?> ReadScoreboardMmrAsync(string accountName)
        {
            if (string.IsNullOrWhiteSpace(accountName)) return null;
            Directory.CreateDirectory(DebugDir);

            using var bmp = CapturePrimaryScreen();
            try { bmp.Save(Path.Combine(DebugDir, "ocr_scoreboard.png"), ImageFormat.Png); } catch { }

            var tokens = await OcrAsync(bmp);
            DumpTokens(bmp, tokens);
            return ParseScoreboard(tokens, accountName);
        }

        private static int? ParseScoreboard(List<Token> tokens, string account)
        {
            // Locate the player's own row by matching the name (aliases included).
            Token? name = null;
            foreach (var t in tokens)
            {
                var acc = Helpers.Accounts.MatchByName(t.Text);
                if (acc != null && string.Equals(acc.Name, account, StringComparison.OrdinalIgnoreCase))
                { name = t; break; }
            }
            if (name == null) return null;

            double rowTol = Math.Max(24, name.H * 2.5);
            int? best = null;
            double bestX = double.MinValue;
            foreach (var t in tokens)
            {
                if (Math.Abs(t.Y - name.Y) > rowTol) continue;   // same row as the name
                if (t.X >= name.X) continue;                     // MMR sits to the left of the name
                string d = OnlyDigits(t.Text);
                if (d.Length < 3 || d.Length > 4) continue;
                if (!int.TryParse(d, out int v) || v < 100 || v > 2500) continue;
                if (t.X > bestX) { bestX = t.X; best = v; }       // closest number to the left of the name
            }
            return best;
        }

        private static void DumpTokens(Bitmap bmp, List<Token> tokens)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"screen: {bmp.Width}x{bmp.Height}    tokens: {tokens.Count}");
                sb.AppendLine("text\tx\ty\tw\th");
                foreach (var t in tokens)
                    sb.AppendLine($"{t.Text}\t{t.X:0}\t{t.Y:0}\t{t.W:0}\t{t.H:0}");
                File.WriteAllText(Path.Combine(DebugDir, "ocr_debug.txt"), sb.ToString(), new UTF8Encoding(false));
            }
            catch { }
        }

        private static Bitmap CapturePrimaryScreen()
        {
            var b = System.Windows.Forms.Screen.PrimaryScreen!.Bounds;
            var bmp = new Bitmap(b.Width, b.Height, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            // Works for windowed / borderless games (what most people run). Exclusive fullscreen
            // can come back black — then the game must be switched to borderless.
            g.CopyFromScreen(b.Location, System.Drawing.Point.Empty, b.Size);
            return bmp;
        }

        private static async Task<List<Token>> OcrAsync(Bitmap bmp)
        {
            var result = new List<Token>();

            var engine = OcrEngine.TryCreateFromUserProfileLanguages()
                         ?? OcrEngine.TryCreateFromLanguage(new Language("en"));
            if (engine == null) return result;   // no OCR language pack installed

            // The engine caps input size; downscale if the screen is larger and remember the scale
            // so reported boxes map back to real screen pixels.
            double scale = 1.0;
            int max = (int)OcrEngine.MaxImageDimension;
            using Bitmap use = (bmp.Width > max || bmp.Height > max)
                ? Downscale(bmp, max, out scale)
                : bmp;

            using var software = ToSoftwareBitmap(use);
            var ocr = await engine.RecognizeAsync(software);

            foreach (var line in ocr.Lines)
                foreach (var w in line.Words)
                {
                    var r = w.BoundingRect;
                    result.Add(new Token
                    {
                        Text = w.Text,
                        X = r.X / scale, Y = r.Y / scale, W = r.Width / scale, H = r.Height / scale,
                    });
                }

            return result;
        }

        private static Bitmap Downscale(Bitmap src, int max, out double scale)
        {
            scale = Math.Min((double)max / src.Width, (double)max / src.Height);
            int w = (int)(src.Width * scale), h = (int)(src.Height * scale);
            var dst = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(dst);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(src, 0, 0, w, h);
            return dst;
        }

        // Copy the pixels straight into a SoftwareBitmap. Encoding to BMP in memory and re-decoding
        // (the obvious route) throws "Parameter is not valid" on 32-bit captures; this doesn't.
        // Format32bppArgb in GDI is already BGRA byte order, matching Bgra8, with no row padding.
        private static SoftwareBitmap ToSoftwareBitmap(Bitmap bmp)
        {
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                int len = data.Stride * bmp.Height;
                var buffer = new byte[len];
                Marshal.Copy(data.Scan0, buffer, 0, len);
                return SoftwareBitmap.CreateCopyFromBuffer(
                    buffer.AsBuffer(), BitmapPixelFormat.Bgra8, bmp.Width, bmp.Height, BitmapAlphaMode.Premultiplied);
            }
            finally { bmp.UnlockBits(data); }
        }

        private static string OnlyDigits(string s)
        {
            var sb = new StringBuilder();
            foreach (char c in s) if (char.IsDigit(c)) sb.Append(c);
            return sb.ToString();
        }
    }
}
