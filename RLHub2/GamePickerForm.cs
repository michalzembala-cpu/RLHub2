using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using RLHub2.Helpers;
using RLHub2.Services;

namespace RLHub2
{
    // "What are you playing?" — the same Big Picture treatment as the profile picker, one step
    // earlier. Picking a game decides which pages the app shows; Rocket League then asks which
    // profile, CS2 does not (its identity is whoever is signed into Steam).
    public class GamePickerForm : Form
    {
        private const int Tile = 190;
        private const int Gap = 28;

        private readonly List<GameId> _games = new() { GameId.RocketLeague, GameId.Cs2 };
        private readonly List<Rectangle> _hits = new();
        private int _hover = -1;
        private Image? _blurred;

        public GameId? Selected { get; private set; }

        public GamePickerForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1000, 660);
            BackColor = Color.FromArgb(10, 12, 24);
            DoubleBuffered = true;
            KeyPreview = true;
            SetStyle(ControlStyles.ResizeRedraw, true);

            MouseMove += OnMove;
            MouseLeave += (s, e) => { _hover = -1; Invalidate(); };
            MouseClick += OnClick;
            KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RoundCorners(20);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (IsHandleCreated) RoundCorners(20);
        }

        private void RoundCorners(int radius)
        {
            if (ClientSize.Width < 4 || ClientSize.Height < 4) return;
            using var path = new GraphicsPath();
            int d = radius * 2;
            var r = ClientRectangle;
            path.AddArc(r.Left, r.Top, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            path.CloseAllFigures();
            Region = new Region(path);
        }

        // Same trick as the profile picker: a heavily downscaled image blown back up is a blur.
        private Image? Blurred()
        {
            if (_blurred != null) return _blurred;
            var src = ArenaBackground.Load("stadion1.jpg");
            if (src == null) return null;

            const int small = 40;
            int h = Math.Max(1, small * src.Height / src.Width);
            using var tiny = new Bitmap(small, h);
            using (var g = Graphics.FromImage(tiny))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                g.DrawImage(src, 0, 0, small, h);
            }
            _blurred = new Bitmap(tiny, tiny.Width * 4, tiny.Height * 4);
            return _blurred;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBilinear;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            g.Clear(Color.FromArgb(10, 12, 24));

            var bg = Blurred();
            if (bg != null)
            {
                float ir = bg.Width / (float)bg.Height, cr = Width / (float)Height;
                Rectangle dst = ir > cr
                    ? new Rectangle((Width - (int)(Height * ir)) / 2, 0, (int)(Height * ir), Height)
                    : new Rectangle(0, (Height - (int)(Width / ir)) / 2, Width, (int)(Width / ir));
                g.DrawImage(bg, dst);
            }
            using (var dim = new SolidBrush(Color.FromArgb(185, 8, 10, 20)))
                g.FillRectangle(dim, 0, 0, Width, Height);

            using var titleFont = new Font("Segoe UI", 26f, FontStyle.Bold);
            using var subFont = new Font("Segoe UI", 11.5f);
            using var white = new SolidBrush(Color.White);
            using var muted = new SolidBrush(Color.FromArgb(190, 200, 215));

            string title = Localization.IsPolish ? "W co grasz?" : "What are you playing?";
            string sub = Localization.IsPolish ? "Wybierz grę" : "Pick a game";

            var ts = g.MeasureString(title, titleFont);
            var ss = g.MeasureString(sub, subFont);
            float titleY = Height * 0.30f;
            g.DrawString(title, titleFont, white, (Width - ts.Width) / 2, titleY);
            g.DrawString(sub, subFont, muted, (Width - ss.Width) / 2, titleY + ts.Height + 6);

            LayoutTiles(titleY + ts.Height + ss.Height + 34);

            // Highlight what's hovered, not what's stored — this screen is about choosing.
            var active = _hover >= 0 ? _games[_hover] : Games.Active;
            for (int i = 0; i < _games.Count; i++)
                DrawGame(g, _hits[i], _games[i], i == _hover, _games[i] == active);

            using var hintFont = new Font("Segoe UI", 9f);
            using var hintBrush = new SolidBrush(Color.FromArgb(150, 255, 255, 255));
            string hint = Localization.IsPolish ? "Esc — anuluj" : "Esc — cancel";
            var hs = g.MeasureString(hint, hintFont);
            g.DrawString(hint, hintFont, hintBrush, (Width - hs.Width) / 2, Height - hs.Height - 18);
        }

        private void LayoutTiles(float top)
        {
            _hits.Clear();
            int count = _games.Count;
            int totalW = count * Tile + (count - 1) * Gap;
            int x = (Width - totalW) / 2;
            int y = (int)top;
            for (int i = 0; i < count; i++)
                _hits.Add(new Rectangle(x + i * (Tile + Gap), y, Tile, Tile));
        }

        private static void DrawGame(Graphics g, Rectangle r, GameId game, bool hover, bool active)
        {
            var c = Games.Accent(game);
            using (var grad = new LinearGradientBrush(r, ControlPaint.Light(c, 0.1f), ControlPaint.Dark(c, 0.45f), 60f))
            using (var path = Round(r, 16))
                g.FillPath(grad, path);

            // Short name big in the middle — no logos to ship, and initials would be useless
            // when both games' art is missing.
            using (var f = new Font("Segoe UI", 46f, FontStyle.Bold))
            using (var b = new SolidBrush(Color.FromArgb(240, 255, 255, 255)))
            {
                var sz = g.MeasureString(Games.ShortName(game), f);
                g.DrawString(Games.ShortName(game), f, b,
                    r.X + (r.Width - sz.Width) / 2, r.Y + (r.Height - sz.Height) / 2);
            }

            if (hover || active)
            {
                using var pen = new Pen(hover ? Color.White : Color.FromArgb(150, 255, 255, 255), 3f);
                using var path = Round(Rectangle.Inflate(r, 3, 3), 18);
                g.DrawPath(pen, path);
            }

            bool lit = hover || active;
            using (var f = new Font("Segoe UI", 11.5f, lit ? FontStyle.Bold : FontStyle.Regular))
            using (var b = new SolidBrush(lit ? Color.White : Color.FromArgb(150, 165, 185)))
            {
                var name = Games.Name(game);
                var sz = g.MeasureString(name, f);
                g.DrawString(name, f, b, r.X + (r.Width - sz.Width) / 2, r.Bottom + 14);
            }
        }

        private static GraphicsPath Round(Rectangle r, int radius)
        {
            int d = radius * 2;
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseAllFigures();
            return p;
        }

        private void OnMove(object? sender, MouseEventArgs e)
        {
            int h = _hits.FindIndex(r => r.Contains(e.Location));
            if (h == _hover) return;
            _hover = h;
            Cursor = h >= 0 ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }

        private void OnClick(object? sender, MouseEventArgs e)
        {
            int i = _hits.FindIndex(r => r.Contains(e.Location));
            if (i < 0 || i >= _games.Count) return;

            Selected = _games[i];
            Games.SetActive(_games[i]);
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _blurred?.Dispose();
            base.Dispose(disposing);
        }
    }
}
