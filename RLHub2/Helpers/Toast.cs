using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;

namespace RLHub2.Helpers
{
    public enum ToastKind { Info, Success, Error }

    public static class Toast
    {
        public static void Show(Control host, string message, ToastKind kind = ToastKind.Info, int durationMs = 2800)
        {
            var form = host?.FindForm();
            if (form == null) return;

            foreach (var old in form.Controls.OfType<ToastPanel>().ToList())
            {
                form.Controls.Remove(old);
                old.Dispose();
            }

            var toast = new ToastPanel(message, kind);
            form.Controls.Add(toast);
            toast.BringToFront();
            toast.Launch(durationMs);
        }
    }

    internal sealed class ToastPanel : Panel
    {
        private readonly string _message;
        private readonly ToastKind _kind;
        private readonly System.Windows.Forms.Timer _slide = new() { Interval = 15 };
        private readonly System.Windows.Forms.Timer _life = new();
        private int _targetX;
        private bool _closing;

        public ToastPanel(string message, ToastKind kind)
        {
            _message = message ?? "";
            _kind = kind;
            DoubleBuffered = true;

            using var f = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            int textW = TextRenderer.MeasureText(_message, f).Width;
            Width = Math.Min(380, Math.Max(180, textW + 70));
            Height = 50;
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        }

        public void Launch(int durationMs)
        {
            var form = FindForm();
            if (form == null) return;

            _targetX = form.ClientSize.Width - Width - 18;
            int startX = form.ClientSize.Width + 10;
            Location = new Point(startX, form.ClientSize.Height - Height - 18);

            _slide.Tick += (s, e) =>
            {
                int dest = _closing ? form.ClientSize.Width + 10 : _targetX;
                int diff = dest - Left;
                if (Math.Abs(diff) < 2)
                {
                    Left = dest;
                    _slide.Stop();
                    if (_closing) { Parent?.Controls.Remove(this); Dispose(); }
                }
                else Left += (int)(diff * 0.28f);
            };
            _slide.Start();

            _life.Interval = durationMs;
            _life.Tick += (s, e) => { _life.Stop(); _closing = true; _slide.Start(); };
            _life.Start();
        }

        private Color Accent => _kind switch
        {
            ToastKind.Success => Color.FromArgb(46, 204, 113),
            ToastKind.Error => Color.FromArgb(225, 70, 80),
            _ => Theme.Accent
        };

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = Rounded(rect, 12);

            using (var bg = new SolidBrush(Theme.IsDark ? Color.FromArgb(30, 30, 56) : Color.FromArgb(250, 250, 255)))
                g.FillPath(bg, path);
            using (var pen = new Pen(Color.FromArgb(90, Accent), 1f))
                g.DrawPath(pen, path);

            g.SetClip(path);
            using (var bar = new SolidBrush(Accent))
                g.FillRectangle(bar, 0, 0, 6, Height);
            g.ResetClip();

            string glyph = _kind switch { ToastKind.Success => "✓", ToastKind.Error => "✕", _ => "ℹ" };
            using (var gf = new Font("Segoe UI", 12f, FontStyle.Bold))
            using (var gb = new SolidBrush(Accent))
                g.DrawString(glyph, gf, gb, 16, Height / 2 - 11);

            using var f = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            using var tb = new SolidBrush(Theme.TextPrimary);
            var fmt = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
            g.DrawString(_message, f, tb, new RectangleF(40, 0, Width - 50, Height), fmt);
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
