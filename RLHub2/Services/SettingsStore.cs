using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using RLHub2.Helpers;

namespace RLHub2.Services
{
    public class AppConfig
    {
        public string Language { get; set; } = "pl"; // "pl" or "en"
        public string TrackerKey { get; set; } = ""; // tracker.gg API key (public-api.tracker.gg)
        public string LastPage { get; set; } = "home";
        public bool SidebarCollapsed { get; set; } = false;
        public string Theme { get; set; } = "dark"; // "dark" or "light"
        public string Accent { get; set; } = "#783CFF"; // hex accent color
        public string TrackedNick { get; set; } = ""; // player nick for tracker.gg fetch
        public string BallchasingKey { get; set; } = ""; // ballchasing.com API key
        public bool BallchasingAutoUpload { get; set; } = true; // auto-upload local replays
    }

    // Persists app settings (language, API key, ...) as JSON in %LocalAppData%\RLHub2\settings.json
    public class SettingsStore
    {
        private readonly string _path;
        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        public SettingsStore()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RLHub2");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "settings.json");
        }

        public AppConfig Load()
        {
            try
            {
                if (!File.Exists(_path))
                    return new AppConfig();
                return JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(_path)) ?? new AppConfig();
            }
            catch
            {
                return new AppConfig();
            }
        }

        public void Save(AppConfig config)
        {
            try
            {
                File.WriteAllText(_path, JsonSerializer.Serialize(config, Options));
            }
            catch
            {
                // ignore write failures
            }
        }

        public AppLanguage LoadLanguage()
        {
            return Load().Language?.ToLowerInvariant() == "en"
                ? AppLanguage.English
                : AppLanguage.Polish;
        }

        public void SaveLanguage(AppLanguage lang)
        {
            var cfg = Load();
            cfg.Language = lang == AppLanguage.English ? "en" : "pl";
            Save(cfg);
        }

        public string LoadTrackerKey() => Load().TrackerKey ?? "";

        public void SaveTrackerKey(string key)
        {
            var cfg = Load();
            cfg.TrackerKey = (key ?? "").Trim();
            Save(cfg);
        }

        public void SaveLastPage(string page)
        {
            var cfg = Load();
            cfg.LastPage = page;
            Save(cfg);
        }

        public void SaveSidebarCollapsed(bool collapsed)
        {
            var cfg = Load();
            cfg.SidebarCollapsed = collapsed;
            Save(cfg);
        }

        public AppTheme LoadTheme()
            => Load().Theme?.ToLowerInvariant() == "light" ? AppTheme.Light : AppTheme.Dark;

        public void SaveTheme(AppTheme theme)
        {
            var cfg = Load();
            cfg.Theme = theme == AppTheme.Light ? "light" : "dark";
            Save(cfg);
        }

        public string LoadTrackedNick() => Load().TrackedNick ?? "";

        public void SaveTrackedNick(string nick)
        {
            var cfg = Load();
            cfg.TrackedNick = (nick ?? "").Trim();
            Save(cfg);
        }

        public string LoadBallchasingKey() => Load().BallchasingKey ?? "";

        public void SaveBallchasingKey(string key)
        {
            var cfg = Load();
            cfg.BallchasingKey = (key ?? "").Trim();
            Save(cfg);
        }

        public bool LoadBallchasingAutoUpload() => Load().BallchasingAutoUpload;

        public void SaveBallchasingAutoUpload(bool on)
        {
            var cfg = Load();
            cfg.BallchasingAutoUpload = on;
            Save(cfg);
        }

        public Color LoadAccent()
        {
            try
            {
                var hex = Load().Accent;
                if (!string.IsNullOrWhiteSpace(hex))
                    return ColorTranslator.FromHtml(hex);
            }
            catch { }
            return Color.FromArgb(120, 60, 255);
        }

        public void SaveAccent(Color c)
        {
            var cfg = Load();
            cfg.Accent = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
            Save(cfg);
        }
    }
}
