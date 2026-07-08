using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Windows.Forms;

namespace RLHub2
{
    // Borderless splash shown briefly at startup, displaying the app logo + a loading bar.
    public class SplashForm : Form
    {
        private Image? _img;
        private int _elapsed;
        private const int Duration = 1800;
        private readonly System.Windows.Forms.Timer _timer;

        public SplashForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(440, 440);
            BackColor = Color.FromArgb(10, 10, 22);
            DoubleBuffered = true;
            ShowInTaskbar = false;
            TopMost = true;

            try
            {
                using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("RLHub2.splash.png");
                if (s != null) _img = Image.FromStream(s);
            }
            catch { /* ignore */ }

            _timer = new System.Windows.Forms.Timer { Interval = 30 };
            _timer.Tick += (sender, e) =>
            {
                _elapsed += 30;
                if (_elapsed >= Duration) { _timer.Stop(); Close(); return; }

                // Fade out slowly over the last 700 ms.
                const int fadeMs = 700;
                int fadeStart = Duration - fadeMs;
                if (_elapsed >= fadeStart)
                    Opacity = System.Math.Max(0.0, 1.0 - (_elapsed - fadeStart) / (double)fadeMs);
            };
            _timer.Start();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            using var path = new GraphicsPath();
            int d = 28 * 2;
            var r = ClientRectangle;
            path.AddArc(r.Left, r.Top, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            path.CloseAllFigures();
            Region = new Region(path);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            if (_img != null)
            {
                int imgSize = (int)(Width * 0.90);
                int x = (Width - imgSize) / 2;
                int y = (int)(Width * 0.02); // small top margin; larger bottom margin balances the cropped frame
                g.DrawImage(_img, x, y, imgSize, imgSize);
            }
            else
            {
                using var f = new Font("Segoe UI", 28f, FontStyle.Bold);
                using var b = new SolidBrush(Color.White);
                g.DrawString("RL HUB 2", f, b, 90, Height / 2 - 24);
            }
        }
    }
}
