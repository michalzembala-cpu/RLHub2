using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using RLHub2.Helpers;
using RLHub2.Services;

namespace RLHub2
{
    // Always-on-top mini scoreboard that floats over other apps / the game.
    // If Resources\overlay_bg.png exists it is used as a (cropped) semi-transparent
    // background and only the live numbers are drawn on top; otherwise a stylised vector
    // version is drawn. Live W/L/streak from the Stats API tracker; MMR is approximate
    // from the latest ranked ballchasing match.
    public class OverlayWindow : Form
    {
        private static OverlayWindow? _instance;
        public static bool IsOpen => _instance != null && !_instance.IsDisposed;

        public static void Toggle()
        {
            if (IsOpen) { _instance!.Close(); _instance = null; }
            else { _instance = new OverlayWindow(); _instance.Show(); }
        }

        private static bool _bgTried;
        private static Image? _bg;
        private static Image? Bg
        {
            get
            {
                if (!_bgTried)
                {
                    _bgTried = true;
                    foreach (var f in new[] { "overlay_bg.png", "overlay_bg.jpg" })
                    {
                        var p = Path.Combine(AppContext.BaseDirectory, "Resources", f);
                        if (File.Exists(p)) { try { _bg = Image.FromFile(p); } catch { } break; }
                    }
                }
                return _bg;
            }
        }
        private bool HasImage => Bg != null;

        // Source crop (fractions of the render) — keep only the panel band.
        private const float CropTop = 0.18f, CropBottom = 0.78f;
        // Number positions as fractions of the (cropped) window.
        private const float WinsX = 0.19f, LossX = 0.79f, MmrX = 0.5f, NumY = 0.52f;
        private const float SideH = 0.52f, MmrH = 0.64f;
        private const float StreakX = 0.575f, StreakY = 0.915f, StreakH = 0.11f;
        // Small playlist tag in the gap between the baked "MMR" label and the number.
        private const float ModeX = 0.5f, ModeY = 0.235f, ModeH = 0.075f;

        private static readonly Color Green = Color.FromArgb(60, 230, 110);
        private static readonly Color Red = Color.FromArgb(240, 60, 75);
        private static readonly Color Purple = Color.FromArgb(150, 90, 255);
        private static readonly Color Muted = Color.FromArgb(150, 160, 195);

        // Fallback when we have nothing else to go on (never played this session, no MMR typed).
        private const string DefaultMode = "2v2";
        private static readonly TimeSpan StaleAfter = TimeSpan.FromDays(7);

        private readonly SessionStore _store = new();
        private readonly MmrStore _mmrStore = new();
        private readonly StatsApiClient _client = StatsApiClient.Instance;
        private readonly System.Windows.Forms.Timer _dataTimer;
        private readonly System.Windows.Forms.Timer _topTimer;

        private int _wins, _losses, _streak;
        private int _mmr;
        private bool _hasMmr, _mmrStale;
        private string _mode = DefaultMode;
        private Image? _rankIcon;

        private bool _dragging;
        private Point _dragOffset;

        public OverlayWindow()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            DoubleBuffered = true;
            BackColor = Color.FromArgb(8, 8, 14);

            if (HasImage)
            {
                int w = 380;
                int h = (int)Math.Round(w * (Bg!.Height * (CropBottom - CropTop)) / Bg.Width);
                Size = new Size(w, h);
                Opacity = 0.90;
            }
            else
            {
                Size = new Size(600, 200);
                Opacity = 0.95;
            }

            var wa = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
            Location = new Point(wa.Left + (wa.Width - Width) / 2, wa.Top + 40);

            SetStyle(ControlStyles.ResizeRedraw, true);
            Recompute();

            _client.MatchLogged += OnMatch;
            _client.ConnectionChanged += OnConn;
            _client.AccountDetected += OnAccount;

            _dataTimer = new System.Windows.Forms.Timer { Interval = 30000 };
            _dataTimer.Tick += (s, e) => { Recompute(); Invalidate(); };
            _dataTimer.Start();

            // keep the overlay above full-screen(borderless) games
            _topTimer = new System.Windows.Forms.Timer { Interval = 1500 };
            _topTimer.Tick += (s, e) => ReassertTopmost();
            _topTimer.Start();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            using var p = Rounded(new Rectangle(0, 0, Width, Height), HasImage ? 12 : 22);
            Region = new Region(p);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ReassertTopmost();
        }

        private void ReassertTopmost()
        {
            if (IsHandleCreated && !IsDisposed)
                SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
        }

        private void OnMatch(Models.SessionMatch _) { if (!IsDisposed) { Recompute(); Invalidate(); } }
        private void OnConn(bool c) { if (!IsDisposed) Invalidate(); }
        private void OnAccount(string _) { if (!IsDisposed) { Recompute(); Invalidate(); } }

        private void Recompute()
        {
            // Follow the account that is actually in the game, not the one selected in the app —
            // you can be playing on one profile while browsing another. Before any match is seen
            // (game closed, or still in the menu) there is nothing to follow, so fall back to the
            // selected profile.
            string account = _client.DetectedAccount ?? Accounts.ActiveName;

            var session = _store.LoadFor(account)
                .Where(m => m.Time >= _client.StartedAt)
                .OrderBy(m => m.Time).ToList();

            _wins = session.Count(m => m.Won);
            _losses = session.Count - _wins;

            _streak = 0;
            if (session.Count > 0)
            {
                bool last = session[^1].Won;
                int c = 0;
                for (int i = session.Count - 1; i >= 0 && session[i].Won == last; i--) c++;
                _streak = last ? c : -c;
            }

            // Follow the playlist actually being played, not a hardcoded 2v2 — each mode has its
            // own rating, so pinning it to 2v2 showed the wrong number in every other mode.
            _mode = ActiveMode(account, session);

            // MMR comes from the MMR tab, not from ballchasing. Ballchasing stopped returning
            // player ranks (verified: no replay uploaded after ~April has rank data, not even
            // other people's), and rank was the only thing we could approximate MMR from — so
            // the ballchasing value would be frozen at whatever it was months ago. Entries typed
            // in by hand land in the same store, so whatever you enter shows up here.
            var points = _mmrStore.LoadFor(account)
                .Where(e => e.Mode == _mode && e.Value > 0)
                .OrderBy(e => e.Timestamp).ToList();

            _hasMmr = points.Count > 0;
            if (_hasMmr)
            {
                var latest = points[^1];
                _mmr = latest.Value;

                // Months-old data must not read as live — the number is dimmed when stale.
                _mmrStale = DateTime.Now - latest.Timestamp > StaleAfter;

                _rankIcon = RankIcons.Get(RankMmr.TierName(_mmr));
            }
            else { _mmr = 0; _rankIcon = null; _mmrStale = false; }
        }

        // Which playlist the MMR is for, in priority order:
        //   1. the match in progress right now (the game is feeding it live),
        //   2. the last match of this session (just finished a game, back in the menu),
        //   3. the playlist whose MMR you most recently touched (nothing played yet this run),
        //   4. 2v2, so there is always something to show.
        private string ActiveMode(string account, System.Collections.Generic.List<Models.SessionMatch> session)
        {
            string live = _client.CurrentMode;
            if (live.Length > 0) return live;

            for (int i = session.Count - 1; i >= 0; i--)
                if (session[i].Mode.Length > 0) return session[i].Mode;

            var last = _mmrStore.LoadFor(account)
                .Where(e => e.Value > 0)
                .OrderByDescending(e => e.Timestamp)
                .FirstOrDefault();
            if (last != null && last.Mode.Length > 0) return last.Mode;

            return DefaultMode;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            if (HasImage)
            {
                g.Clear(Color.FromArgb(8, 8, 14));
                g.DrawImage(Bg!, new Rectangle(0, 0, Width, Height),
                    0, Bg!.Height * CropTop, Bg.Width, Bg.Height * (CropBottom - CropTop), GraphicsUnit.Pixel);
                return;
            }

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = Rounded(rect, 22);
            using var bg = new LinearGradientBrush(rect, Color.FromArgb(20, 20, 30), Color.FromArgb(6, 6, 12), 90f);
            g.FillPath(bg, path);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            if (HasImage) PaintNumbers(g);
            else PaintVector(g);
        }

        // Draw only the live numbers (labels are baked into the render).
        private void PaintNumbers(Graphics g)
        {
            var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            DrawNumber(g, _wins.ToString(), WinsX, NumY, SideH, Color.FromArgb(150, 255, 170), Green, center, false);

            // Stale MMR is drawn grey instead of purple, so a months-old number never passes
            // for a live one. Nothing to show at all → a dash, not a zero.
            string mmrText = _hasMmr ? _mmr.ToString() : "—";
            var mmrTop = _mmrStale ? Color.FromArgb(150, 155, 175) : Color.FromArgb(130, 160, 255);
            var mmrBottom = _mmrStale ? Color.FromArgb(95, 100, 120) : Color.FromArgb(175, 95, 255);
            DrawNumber(g, mmrText, MmrX, NumY, MmrH, mmrTop, mmrBottom, center, false);
            DrawModeTag(g, ModeX, ModeY, Height * ModeH, center);

            DrawNumber(g, _losses.ToString(), LossX, NumY, SideH, Color.FromArgb(255, 130, 140), Red, center, false);

            string streakVal = _streak == 0 ? "0" : _streak > 0 ? $"{_streak}W" : $"{-_streak}L";
            var streakColor = _streak > 0 ? Green : _streak < 0 ? Red : Color.FromArgb(200, 200, 220);
            DrawNumber(g, streakVal, StreakX, StreakY, StreakH, streakColor, streakColor, center, false);
        }

        // Small "3V3" / "HOOPS" tag so it is always clear which playlist the MMR belongs to.
        private void DrawModeTag(Graphics g, float cxF, float cyF, float px, StringFormat center)
        {
            string label = ModeLabel(_mode);
            if (label.Length == 0) return;
            using var font = new Font("Segoe UI", Math.Max(8f, px), FontStyle.Bold, GraphicsUnit.Pixel);
            var color = _mmrStale ? Color.FromArgb(150, 150, 165) : Color.FromArgb(190, 175, 255);
            using var br = new SolidBrush(color);
            g.DrawString(label, font, br, Width * cxF, Height * cyF, center);
        }

        private static string ModeLabel(string mode) => mode switch
        {
            "1v1" => "1V1",
            "2v2" => "2V2",
            "3v3" => "3V3",
            _ => mode.ToUpperInvariant(),
        };

        private void DrawNumber(Graphics g, string text, float cxF, float cyF, float hF,
            Color top, Color bottom, StringFormat center, bool mask)
        {
            float px = Height * hF;
            using var font = BlockFont(px);
            var size = g.MeasureString(text, font);
            float cx = Width * cxF, cy = Height * cyF;
            var rect = new RectangleF(cx - size.Width / 2, cy - size.Height / 2, size.Width, size.Height);

            if (mask)
            {
                var m = RectangleF.Inflate(rect, size.Width * 0.16f, -size.Height * 0.12f);
                using var mb = new SolidBrush(Color.FromArgb(225, 12, 11, 20));
                g.FillEllipse(mb, m);
            }

            using (var glow = new SolidBrush(Color.FromArgb(80, bottom)))
                for (int dx = -2; dx <= 2; dx += 2)
                    for (int dy = -2; dy <= 2; dy += 2)
                        g.DrawString(text, font, glow, new RectangleF(rect.X + dx, rect.Y + dy, rect.Width, rect.Height), center);
            using (var br = new LinearGradientBrush(rect, top, bottom, 90f))
                g.DrawString(text, font, br, rect, center);
        }

        // ===== VECTOR MODE (no render) =====
        private void PaintVector(Graphics g)
        {
            var leftRect = new RectangleF(22, 60, 250, 120);
            var rightRect = new RectangleF(Width - 272, 60, 250, 120);
            var centerRect = new RectangleF(Width / 2f - 118, 34, 236, 150);

            DrawPanel(g, leftRect, 20, Green);
            DrawPanel(g, rightRect, 20, Red);
            DrawPanel(g, centerRect, 28, Purple);

            using var labelFont = new Font("Segoe UI", 11f, FontStyle.Bold);
            using var bigSide = BlockFont(46f);
            using var bigMid = BlockFont(60f);
            using var footFont = new Font("Segoe UI", 10f, FontStyle.Bold);
            var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            DrawLabel(g, "WINS", labelFont, Green, new RectangleF(22, 78, 200, 20), center);
            DrawBigNumber(g, _wins.ToString(), bigSide, new RectangleF(22, 96, 200, 74), Color.FromArgb(120, 255, 150), Green, center);

            DrawLabel(g, "LOSSES", labelFont, Red, new RectangleF(Width - 222, 78, 200, 20), center);
            DrawBigNumber(g, _losses.ToString(), bigSide, new RectangleF(Width - 222, 96, 200, 74), Color.FromArgb(255, 120, 130), Red, center);

            DrawGem(g, new RectangleF(Width / 2f - 20, 4, 40, 40));

            string mmrLabel = ModeLabel(_mode) is { Length: > 0 } m ? $"MMR · {m}" : "MMR";
            DrawLabel(g, mmrLabel, labelFont, Color.FromArgb(190, 170, 255), new RectangleF(Width / 2f - 118, 48, 236, 20), center);
            string mmrText = _hasMmr ? _mmr.ToString() : "—";
            DrawBigNumber(g, mmrText, bigMid, new RectangleF(Width / 2f - 118, 66, 236, 96), Color.FromArgb(120, 150, 255), Color.FromArgb(170, 90, 255), center);

            string streakVal = _streak == 0 ? "0" : _streak > 0 ? $"{_streak}W" : $"{-_streak}L";
            var streakColor = _streak > 0 ? Green : _streak < 0 ? Red : Muted;
            using (var mb = new SolidBrush(Muted))
                g.DrawString("STREAK", footFont, mb, new RectangleF(Width / 2f - 70, Height - 26, 90, 18),
                    new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center });
            using (var sc = new SolidBrush(streakColor))
                g.DrawString(streakVal, footFont, sc, new RectangleF(Width / 2f + 24, Height - 26, 50, 18),
                    new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center });
        }

        private static void DrawPanel(Graphics g, RectangleF r, float cut, Color neon)
        {
            using var path = Chamfer(r, cut);
            using (var fill = new LinearGradientBrush(r, Color.FromArgb(40, neon), Color.FromArgb(10, 12, 18), 90f))
                g.FillPath(fill, path);
            for (int i = 6; i >= 1; i--)
                using (var pen = new Pen(Color.FromArgb(20, neon), 2f + i * 2.5f) { LineJoin = LineJoin.Round })
                    g.DrawPath(pen, path);
            using (var core = new Pen(neon, 2.2f) { LineJoin = LineJoin.Round })
                g.DrawPath(core, path);
        }

        private static void DrawLabel(Graphics g, string text, Font font, Color color, RectangleF area, StringFormat sf)
        {
            using var b = new SolidBrush(color);
            g.DrawString(text, font, b, area, sf);
        }

        private static void DrawBigNumber(Graphics g, string text, Font font, RectangleF area, Color top, Color bottom, StringFormat sf)
        {
            using (var glow = new SolidBrush(Color.FromArgb(70, bottom)))
                for (int dx = -2; dx <= 2; dx += 2)
                    for (int dy = -2; dy <= 2; dy += 2)
                        g.DrawString(text, font, glow, new RectangleF(area.X + dx, area.Y + dy, area.Width, area.Height), sf);
            using var br = new LinearGradientBrush(area, top, bottom, 90f);
            g.DrawString(text, font, br, area, sf);
        }

        private void DrawGem(Graphics g, RectangleF r)
        {
            if (_rankIcon != null)
            {
                using (var glow = new SolidBrush(Color.FromArgb(70, Purple)))
                    g.FillEllipse(glow, RectangleF.Inflate(r, 4, 4));
                var img = _rankIcon;
                float ar = img.Width / (float)img.Height;
                float w = r.Width, h = r.Height;
                if (ar > 1) h = w / ar; else w = h * ar;
                g.DrawImage(img, r.X + (r.Width - w) / 2, r.Y + (r.Height - h) / 2, w, h);
                return;
            }
            var cx = r.X + r.Width / 2; var cy = r.Y + r.Height / 2;
            using var path = new GraphicsPath();
            path.AddPolygon(new[] { new PointF(cx, r.Top), new PointF(r.Right, cy), new PointF(cx, r.Bottom), new PointF(r.Left, cy) });
            using var fill = new LinearGradientBrush(r, Color.FromArgb(180, 130, 255), Color.FromArgb(90, 60, 220), 90f);
            g.FillPath(fill, path);
        }

        private static Font BlockFont(float pixels)
        {
            try { return new Font("Arial Black", pixels * 0.72f, FontStyle.Bold, GraphicsUnit.Pixel); }
            catch { return new Font("Segoe UI", pixels * 0.72f, FontStyle.Bold, GraphicsUnit.Pixel); }
        }

        private static GraphicsPath Chamfer(RectangleF r, float c)
        {
            var p = new GraphicsPath();
            p.AddPolygon(new[]
            {
                new PointF(r.Left + c, r.Top),  new PointF(r.Right - c, r.Top),
                new PointF(r.Right, r.Top + c), new PointF(r.Right, r.Bottom - c),
                new PointF(r.Right - c, r.Bottom), new PointF(r.Left + c, r.Bottom),
                new PointF(r.Left, r.Bottom - c), new PointF(r.Left, r.Top + c)
            });
            p.CloseAllFigures();
            return p;
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

        // ===== drag to move, right-click to close =====
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left) { _dragging = true; _dragOffset = e.Location; }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragging)
                Location = new Point(Cursor.Position.X - _dragOffset.X, Cursor.Position.Y - _dragOffset.Y);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _dragging = false;
            if (e.Button == MouseButtons.Right) Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dataTimer?.Stop(); _dataTimer?.Dispose();
                _topTimer?.Stop(); _topTimer?.Dispose();
                _client.MatchLogged -= OnMatch;
                _client.ConnectionChanged -= OnConn;
                _client.AccountDetected -= OnAccount;
            }
            base.Dispose(disposing);
        }

        // keep on top of borderless games without stealing focus
        private static readonly IntPtr HWND_TOPMOST = new(-1);
        private const uint SWP_NOSIZE = 0x0001, SWP_NOMOVE = 0x0002, SWP_NOACTIVATE = 0x0010;
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    }
}
