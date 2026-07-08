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

        // Accent (configurable) stays the same in both modes; AccentSoft is a lighter tint.
        public static Color Accent => _accent;
        public static Color AccentSoft => Mix(_accent, Color.White, 0.3f);

        public static Color PageBg      => D(12, 12, 26,   238, 240, 247);
        public static Color Sidebar     => D(18, 18, 38,   228, 231, 240);
        public static Color Surface     => D(18, 18, 38,   255, 255, 255);
        public static Color SurfaceAlt  => D(28, 28, 56,   236, 238, 245);
        // Card bodies are semi-transparent so the arena backdrop shows through them.
        public static Color CardTop     => Color.FromArgb(IsDark ? 188 : 222, D(26, 26, 52,   255, 255, 255));
        public static Color CardBottom  => Color.FromArgb(IsDark ? 188 : 222, D(16, 16, 34,   238, 240, 247));

        public static Color TextPrimary   => D(255, 255, 255,  24, 26, 42);
        public static Color TextSecondary => D(170, 185, 220,  78, 86, 110);
        public static Color TextMuted      => D(140, 160, 200,  120, 130, 155);

        public static Color GridHeaderBg => D(26, 26, 52,   224, 227, 236);
        public static Color GridRowBg    => D(18, 18, 38,   255, 255, 255);
        public static Color GridAltBg    => D(22, 22, 44,   244, 246, 250);
        public static Color GridLines    => D(40, 40, 72,   210, 214, 224);

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
