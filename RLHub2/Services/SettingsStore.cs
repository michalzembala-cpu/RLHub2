using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using RLHub2.Helpers;
using RLHub2.Models;

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
        public string TrackedNick { get; set; } = ""; // legacy single nick (migrated to Accounts)
        public string BallchasingKey { get; set; } = ""; // ballchasing.com API key
        public bool BallchasingAutoUpload { get; set; } = true; // auto-upload local replays

        public List<Account> Accounts { get; set; } = new();
        public string ActiveAccount { get; set; } = "";

        // Show the "Who's playing?" picker on launch (only ever asked when there is >1 account).
        public bool AskProfileOnStart { get; set; } = true;

        // Send local .replay files to the Recycle Bin once they are safely on ballchasing
        // and older than this many days. 0 = never delete.
        public int DeleteReplaysAfterDays { get; set; } = 0;
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

        // Load() is called a lot — every Accounts.ActiveName lookup goes through it, and those
        // happen once per stored match when a page filters its data. Hitting the disk each time
        // meant hundreds of reads + JSON parses per page load, so the config is cached and only
        // re-read when the file actually changes on disk (or we write it ourselves).
        private static readonly object CacheLock = new();
        private static AppConfig? _cache;
        private static DateTime _cacheStamp;
        private static long _cacheLength = -1;

        public AppConfig Load()
        {
            lock (CacheLock)
            {
                try
                {
                    var info = new FileInfo(_path);
                    if (!info.Exists)
                    {
                        _cache = null;
                        _cacheLength = -1;
                        return new AppConfig();
                    }

                    if (_cache != null && info.LastWriteTimeUtc == _cacheStamp && info.Length == _cacheLength)
                        return _cache;

                    var cfg = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(_path)) ?? new AppConfig();
                    _cache = cfg;
                    _cacheStamp = info.LastWriteTimeUtc;
                    _cacheLength = info.Length;
                    return cfg;
                }
                catch
                {
                    return _cache ?? new AppConfig();
                }
            }
        }

        public void Save(AppConfig config)
        {
            lock (CacheLock)
            {
                try
                {
                    File.WriteAllText(_path, JsonSerializer.Serialize(config, Options));

                    var info = new FileInfo(_path);
                    _cache = config;
                    _cacheStamp = info.LastWriteTimeUtc;
                    _cacheLength = info.Length;
                }
                catch
                {
                    // ignore write failures — but never serve a config we failed to persist
                    _cache = null;
                    _cacheLength = -1;
                }
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

        // ===== accounts =====

        // Accounts, migrating the legacy single nick on first use.
        public List<Account> LoadAccounts()
        {
            var cfg = Load();
            var list = cfg.Accounts ?? new List<Account>();
            if (list.Count == 0 && !string.IsNullOrWhiteSpace(cfg.TrackedNick))
            {
                list.Add(new Account { Name = cfg.TrackedNick.Trim() });
                cfg.Accounts = list;
                cfg.ActiveAccount = list[0].Name;
                Save(cfg);
            }
            return list;
        }

        public void SaveAccounts(List<Account> accounts)
        {
            var cfg = Load();
            cfg.Accounts = accounts ?? new List<Account>();
            if (!cfg.Accounts.Any(a => a.Name == cfg.ActiveAccount))
                cfg.ActiveAccount = cfg.Accounts.FirstOrDefault()?.Name ?? "";
            Save(cfg);
        }

        public string LoadActiveAccountName()
        {
            var cfg = Load();
            var accounts = LoadAccounts();
            if (accounts.Any(a => a.Name == cfg.ActiveAccount)) return cfg.ActiveAccount;
            return accounts.FirstOrDefault()?.Name ?? "";
        }

        public void SaveActiveAccount(string name)
        {
            var cfg = Load();
            cfg.ActiveAccount = name ?? "";
            Save(cfg);
        }

        public bool LoadAskProfileOnStart() => Load().AskProfileOnStart;

        public void SaveAskProfileOnStart(bool on)
        {
            var cfg = Load();
            cfg.AskProfileOnStart = on;
            Save(cfg);
        }

        public string LoadBallchasingKey() => Load().BallchasingKey ?? "";

        public void SaveBallchasingKey(string key)
        {
            var cfg = Load();
            cfg.BallchasingKey = (key ?? "").Trim();
            Save(cfg);
        }

        public int LoadDeleteReplaysAfterDays() => Load().DeleteReplaysAfterDays;

        public void SaveDeleteReplaysAfterDays(int days)
        {
            var cfg = Load();
            cfg.DeleteReplaysAfterDays = days < 0 ? 0 : days;
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
