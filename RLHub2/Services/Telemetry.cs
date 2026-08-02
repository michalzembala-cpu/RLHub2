using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RLHub2.Services
{
    // Anonymous, opt-in usage telemetry through Aptabase's HTTP ingest API — no SDK, matching the
    // rest of the app's raw-HTTP style.
    //
    // Sends NOTHING unless BOTH are true: an app key is compiled in below, AND the user opted in
    // (Settings). No IP is attached, no nick, no file paths, no exception messages — only a random
    // per-run session id, coarse event names, and the OS/app version. Every send is fire-and-forget
    // and swallows all errors, so telemetry can never slow down or crash the app.
    public static class Telemetry
    {
        // Paste the Aptabase App Key here — it looks like "A-EU-1234567890" (the region is part of
        // the key). Leave empty to keep telemetry fully off for everyone, regardless of consent.
        private const string AppKey = "A-EU-8734560932";

        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };
        private static bool _enabled;
        private static string _sessionId = "";
        private static string _baseUrl = "";
        private static string _appVersion = "1.0.0";

        // True when a key is compiled in — i.e. telemetry is even an option worth asking about.
        public static bool IsAvailable => AppKey.Length > 0;

        public static void Init()
        {
            _enabled = IsAvailable && new SettingsStore().TelemetryEnabled;
            if (!_enabled) return;

            _baseUrl = RegionUrl(AppKey);
            _sessionId = Guid.NewGuid().ToString("N");   // random, not tied to the machine or user
            var v = Assembly.GetExecutingAssembly().GetName().Version;
            if (v != null) _appVersion = $"{v.Major}.{v.Minor}.{v.Build}";
        }

        // The region is encoded in the key prefix, so the ingest host comes straight from it.
        private static string RegionUrl(string key)
        {
            string up = key.ToUpperInvariant();
            if (up.StartsWith("A-US")) return "https://us.aptabase.com";
            if (up.StartsWith("A-DEV")) return "https://localhost";   // self-hosted dev, rarely used
            return "https://eu.aptabase.com";
        }

        public static void Track(string eventName, Dictionary<string, object>? props = null)
        {
            if (!_enabled) return;
            _ = SendAsync(eventName, props);   // fire-and-forget
        }

        private static async Task SendAsync(string eventName, Dictionary<string, object>? props)
        {
            try
            {
                var payload = new
                {
                    timestamp = DateTime.UtcNow.ToString("o"),
                    sessionId = _sessionId,
                    eventName,
                    systemProps = new
                    {
                        isDebug = System.Diagnostics.Debugger.IsAttached,
                        osName = "Windows",
                        osVersion = Environment.OSVersion.Version.ToString(),
                        locale = CultureInfo.CurrentUICulture.Name,
                        appVersion = _appVersion,
                        sdkVersion = "nexplay-http@1.0.0",
                    },
                    props = props ?? new Dictionary<string, object>(),
                };

                string json = JsonSerializer.Serialize(payload);
                using var req = new HttpRequestMessage(HttpMethod.Post, _baseUrl + "/api/v0/event");
                req.Headers.Add("App-Key", AppKey);
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                await Http.SendAsync(req).ConfigureAwait(false);
            }
            catch { /* telemetry must never affect the app */ }
        }
    }
}
