using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using RLHub2.Helpers;
using RLHub2.Models;
using RLHub2.Services;

namespace RLHub2
{
    // Always-on-top mini panel for Overwatch. OW2 has no live match feed, so instead of auto-scored
    // points it carries WIN / DRAW / LOSS buttons — one click logs the result to the session while
    // you play, and the record/win-rate/streak update in place. Drag to move, right-click to close.
    public class OwOverlayWindow : Form
    {
        private static OwOverlayWindow? _instance;
        public static bool IsOpen => _instance != null && !_instance.IsDisposed;

        public static void Toggle()
        {
            if (IsOpen) { _instance!.Close(); _instance = null; }
            else { _instance = new OwOverlayWindow(); _instance.Show(); }
        }

        private readonly OwSessionStore _store = new();
        private readonly Label _record;
        private readonly Label _sub;
        private readonly System.Windows.Forms.Timer _topTimer;

        private bool _dragging;
        private Point _dragOffset;

        public OwOverlayWindow()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            DoubleBuffered = true;
            BackColor = Color.FromArgb(12, 10, 16);
            Opacity = 0.93;
            Size = new Size(300, 150);

            var wa = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
            Location = new Point(wa.Right - Width - 40, wa.Top + 40);

            var accent = Games.Accent(GameId.Overwatch);

            var title = new Label
            {
                Text = "OVERWATCH", Location = new Point(16, 12), AutoSize = true,
                ForeColor = accent, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), BackColor = Color.Transparent,
            };
            _record = new Label
            {
                Location = new Point(14, 28), Size = new Size(272, 40), TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White, Font = new Font("Segoe UI", 22f, FontStyle.Bold), BackColor = Color.Transparent,
            };
            _sub = new Label
            {
                Location = new Point(14, 70), Size = new Size(272, 20), TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(170, 175, 195), Font = new Font("Segoe UI", 9.5f), BackColor = Color.Transparent,
            };

            var win = Btn("WYGRANA", Localization.IsPolish ? "WYGRANA" : "WIN", Color.FromArgb(46, 204, 113), new Point(14, 98), 96);
            win.Click += (s, e) => Log("W");
            var draw = Btn("REMIS", Localization.IsPolish ? "REMIS" : "DRAW", Color.FromArgb(150, 150, 160), new Point(116, 98), 68);
            draw.Click += (s, e) => Log("D");
            var loss = Btn("PRZEGRANA", Localization.IsPolish ? "PRZEGRANA" : "LOSS", Color.FromArgb(230, 90, 90), new Point(190, 98), 96);
            loss.Click += (s, e) => Log("L");

            Controls.AddRange(new Control[] { title, _record, _sub, win, draw, loss });

            // Dragging: the labels sit on top of the form, so listen on them too.
            foreach (Control c in new Control[] { this, title, _record, _sub })
            {
                c.MouseDown += OnDragDown;
                c.MouseMove += OnDragMove;
                c.MouseUp += OnDragUp;
            }

            _topTimer = new System.Windows.Forms.Timer { Interval = 1500 };
            _topTimer.Tick += (s, e) => ReassertTopmost();
            _topTimer.Start();

            Refresh2();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            using var p = Rounded(new Rectangle(0, 0, Width, Height), 16);
            Region = new Region(p);
        }

        protected override void OnShown(EventArgs e) { base.OnShown(e); ReassertTopmost(); }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = Rounded(rect, 16);
            using var bg = new LinearGradientBrush(rect, Color.FromArgb(24, 20, 30), Color.FromArgb(10, 8, 14), 90f);
            g.FillPath(bg, path);
            using var pen = new Pen(Color.FromArgb(60, Games.Accent(GameId.Overwatch)), 1.5f);
            g.DrawPath(pen, path);
        }

        private void Log(string result)
        {
            _store.Append(new OwMatch { Time = DateTime.Now, Result = result, Role = "" });
            Refresh2();
        }

        private void Refresh2()
        {
            var data = _store.Load();
            var session = data.Matches.Where(m => m.Time >= data.SessionStart).ToList();
            int w = session.Count(m => m.Won);
            int l = session.Count(m => m.Lost);
            int d = session.Count(m => m.Draw);
            int decided = w + l;

            _record.Text = d > 0 ? $"{w} – {l} – {d}" : $"{w} – {l}";

            string wr = decided > 0 ? Math.Round(100.0 * w / decided) + "%" : "—";
            _sub.Text = (Localization.IsPolish ? "Winrate " : "Win rate ") + wr + "   ·   " +
                        (Localization.IsPolish ? "Passa " : "Streak ") + StreakText(session);
        }

        private static string StreakText(System.Collections.Generic.List<OwMatch> session)
        {
            var decided = session.Where(m => !m.Draw).ToList();
            if (decided.Count == 0) return "—";
            bool won = decided[^1].Won;
            int n = 0;
            for (int i = decided.Count - 1; i >= 0 && decided[i].Won == won; i--) n++;
            return (won ? "W" : (Localization.IsPolish ? "P" : "L")) + n;
        }

        private static Button Btn(string name, string text, Color back, Point at, int w)
        {
            var b = new Button
            {
                Name = name, Text = text, Location = at, Size = new Size(w, 38), FlatStyle = FlatStyle.Flat,
                BackColor = back, ForeColor = Color.White, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand, TabStop = false,
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private void ReassertTopmost()
        {
            if (IsHandleCreated && !IsDisposed)
                SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
        }

        private void OnDragDown(object? s, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { _dragging = true; _dragOffset = PointToClient(Cursor.Position); }
            else if (e.Button == MouseButtons.Right) Close();
        }
        private void OnDragMove(object? s, MouseEventArgs e)
        {
            if (_dragging) Location = new Point(Cursor.Position.X - _dragOffset.X, Cursor.Position.Y - _dragOffset.Y);
        }
        private void OnDragUp(object? s, MouseEventArgs e) => _dragging = false;

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _topTimer?.Stop(); _topTimer?.Dispose(); }
            base.Dispose(disposing);
        }

        private static GraphicsPath Rounded(Rectangle r, int radius)
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

        private static readonly IntPtr HWND_TOPMOST = new(-1);
        private const uint SWP_NOSIZE = 0x0001, SWP_NOMOVE = 0x0002, SWP_NOACTIVATE = 0x0010;
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    }
}
