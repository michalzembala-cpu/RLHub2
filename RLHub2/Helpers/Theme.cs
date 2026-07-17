using System;
using System.Drawing;

namespace RLHub2.Helpers
{
    public enum AppTheme { Dark, Light }

    // App-wide color palette. Controls read these at paint time; pages re-read on rebuild.
    public static class Theme
    {
        public static AppTheme Mode { get; private set; } = AppTheme.Dark;
        public static event Action? ThemeChanged;

        public static bool IsDark => Mode == AppTheme.Dark;

        public static void Initialize(AppTheme mode) => Mode = mode;

        // ===== PER-GAME PALETTE =====
        // Each game has its own look: Rocket League is navy, CS2 graphite. Held as a field and
        // set explicitly rather than read from Games.Active — these properties are hit many
        // times per paint, and a settings lookup behind each one would be a syscall per pixel-ish.
        private static GameId _game = GameId.RocketLeague;

        public static void SetGame(GameId g)
        {
            if (_game == g) return;
            _game = g;
            ThemeChanged?.Invoke();
        }

        public static void InitializeGame(GameId g) => _game = g;

        private static bool Cs2 => _game == GameId.Cs2;

        // Dark-mode color that differs per game: (rocket league) vs (cs2).
        private static Color G(int r, int g, int b, int cr, int cg, int cb)
            => Cs2 ? Color.FromArgb(cr, cg, cb) : Color.FromArgb(r, g, b);

        public static void SetTheme(AppTheme mode)
        {
            if (Mode == mode) return;
            Mode = mode;
            ThemeChanged?.Invoke();
        }

        // ===== ACCENT =====
        private static Color _accent = Color.FromArgb(120, 60, 255);

        // Preset accent choices shown in Settings.
        public static readonly Color[] Accents =
        {
            Color.FromArgb(120, 60, 255),  // purple
            Color.FromArgb(0, 140, 255),   // blue
            Color.FromArgb(0, 200, 180),   // teal
            Color.FromArgb(255, 120, 30),  // orange
            Color.FromArgb(235, 70, 140),  // pink
            Color.FromArgb(46, 204, 113),  // green
        };

        public static void InitializeAccent(Color c) => _accent = c;

        public static void SetAccent(Color c)
        {
            if (_accent.ToArgb() == c.ToArgb()) return;
            _accent = c;
            ThemeChanged?.Invoke();
        }

        private static Color D(int r, int g, int b, int lr, int lg, int lb)
            => IsDark ? Color.FromArgb(r, g, b) : Color.FromArgb(lr, lg, lb);

        // Same, but the dark value differs per game.
        private static Color DG(int r, int g, int b, int cr, int cg, int cb, int lr, int lg, int lb)
            => IsDark ? G(r, g, b, cr, cg, cb) : Color.FromArgb(lr, lg, lb);

        // Accent (configurable) stays the same in both modes; AccentSoft is a lighter tint.
        public static Color Accent => _accent;
        public static Color AccentSoft => Mix(_accent, Color.White, 0.3f);

        // CS2 is graphite (page #0F1115, cards #171B22); Rocket League stays navy.
        public static Color PageBg      => DG(12, 12, 26,   15, 17, 21,    238, 240, 247);
        public static Color Sidebar     => DG(18, 18, 38,   19, 22, 28,    228, 231, 240);
        public static Color Surface     => DG(18, 18, 38,   23, 27, 34,    255, 255, 255);
        public static Color SurfaceAlt  => DG(28, 28, 56,   30, 35, 44,    236, 238, 245);
        // Card bodies are semi-transparent so the backdrop shows through them.
        public static Color CardTop     => Color.FromArgb(IsDark ? 188 : 222, DG(26, 26, 52,   26, 31, 39,   255, 255, 255));
        public static Color CardBottom  => Color.FromArgb(IsDark ? 188 : 222, DG(16, 16, 34,   18, 21, 27,   238, 240, 247));

        public static Color TextPrimary   => D(255, 255, 255,  24, 26, 42);
        public static Color TextSecondary => DG(170, 185, 220, 176, 182, 194,  78, 86, 110);
        public static Color TextMuted      => DG(140, 160, 200, 130, 138, 152,  120, 130, 155);

        public static Color GridHeaderBg => DG(26, 26, 52,   26, 31, 39,   224, 227, 236);
        public static Color GridRowBg    => DG(18, 18, 38,   23, 27, 34,   255, 255, 255);
        public static Color GridAltBg    => DG(22, 22, 44,   28, 33, 42,   244, 246, 250);
        public static Color GridLines    => DG(40, 40, 72,   44, 50, 60,   210, 214, 224);

        // Valve's blue, for the second accent the CS2 pages use alongside orange.
        public static Color Cs2Blue => Color.FromArgb(88, 164, 232);

        // Tints a base color toward the accent. Preserves the base color's alpha
        // so translucent card colors stay translucent after tinting.
        public static Color Mix(Color a, Color b, float t)
            => Color.FromArgb(
                a.A,
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
    }
}
