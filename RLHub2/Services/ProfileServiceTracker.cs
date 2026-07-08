using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using RLHub2.Models;

namespace RLHub2.Services
{
    // Real Rocket League stats from the official tracker.gg API (public-api.tracker.gg).
    // Requires a valid TRN-Api-Key (configured in Settings). Tries several platforms.
    public class ProfileServiceTracker : IProfileService
    {
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };

        private static readonly string[] Platforms = { "epic", "steam", "psn", "xbl" };

        private static readonly Dictionary<int, string> PlaylistModes = new()
        {
            [10] = "1v1",
            [11] = "2v2",
            [13] = "3v3",
        };

        public class NoKeyException : Exception { }

        public async Task<Profile> GetProfileAsync(string nick)
        {
            if (string.IsNullOrWhiteSpace(nick))
                throw new ArgumentException("Empty nick");

            string key = new SettingsStore().LoadTrackerKey();
            if (string.IsNullOrWhiteSpace(key))
                throw new NoKeyException();

            nick = nick.Trim();
            var diag = new List<string>();

            foreach (var platform in Platforms)
            {
                try
                {
                    var uri = $"https://public-api.tracker.gg/v2/rocket-league/standard/profile/{platform}/{Uri.EscapeDataString(nick)}";
                    using var req = new HttpRequestMessage(HttpMethod.Get, uri);
                    req.Headers.TryAddWithoutValidation("TRN-Api-Key", key);
                    req.Headers.TryAddWithoutValidation("Accept", "application/json");
                    req.Headers.TryAddWithoutValidation("User-Agent", "RLHub2-Client/1.0");

                    using var resp = await Http.SendAsync(req);
                    diag.Add($"{platform}:{(int)resp.StatusCode}");

                    if (!resp.IsSuccessStatusCode)
                        continue;

                    var json = await resp.Content.ReadAsStringAsync();
                    var profile = Parse(json, nick);
                    if (profile.Ranks.Count > 0 || profile.Wins > 0)
                        return profile;
                }
                catch (Exception ex)
                {
                    diag.Add($"{platform}:ERR({ex.GetType().Name})");
                }
            }

            throw new Exception(string.Join("  ", diag));
        }

        private static Profile Parse(string json, string nick)
        {
            var p = new Profile { Nick = nick };

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("data", out var data))
                return p;
            if (!data.TryGetProperty("segments", out var segments) || segments.ValueKind != JsonValueKind.Array)
                return p;

            foreach (var seg in segments.EnumerateArray())
            {
                string type = seg.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
                if (!seg.TryGetProperty("stats", out var stats) || stats.ValueKind != JsonValueKind.Object)
                    continue;

                if (type == "overview")
                {
                    p.Wins = StatInt(stats, "wins");
                    p.Goals = StatInt(stats, "goals");
                    p.Assists = StatInt(stats, "assists");
                    p.Saves = StatInt(stats, "saves");
                    p.Mvps = StatInt(stats, "mVPs");
                    p.Matches = StatInt(stats, "matchesPlayed");
                }
                else if (type == "playlist")
                {
                    int playlistId = -1;
                    if (seg.TryGetProperty("attributes", out var attr) &&
                        attr.TryGetProperty("playlistId", out var pid) &&
                        pid.ValueKind == JsonValueKind.Number)
                        playlistId = pid.GetInt32();

                    if (!PlaylistModes.TryGetValue(playlistId, out var mode))
                        continue;

                    int rating = StatInt(stats, "rating");
                    string tier = StatName(stats, "tier");
                    string division = StatName(stats, "division");
                    string rank = string.IsNullOrWhiteSpace(division) ? tier : $"{tier} · {division}";

                    p.Ranks.Add(new PlaylistRank { Mode = mode, Mmr = rating, RankName = rank });

                    int peak = StatInt(stats, "peakRating");
                    if (peak > 0)
                        p.SeasonHistory.Add(new SeasonRecord { Season = $"{mode} peak", PeakRank = rank, PeakMmr = peak });
                }
            }

            var main = p.Ranks.Find(r => r.Mode == "2v2") ?? (p.Ranks.Count > 0 ? p.Ranks[0] : null);
            if (main != null) { p.Rank = main.RankName; p.MMR = main.Mmr; }

            return p;
        }

        private static int StatInt(JsonElement stats, string name)
        {
            if (stats.TryGetProperty(name, out var node) && node.ValueKind == JsonValueKind.Object &&
                node.TryGetProperty("value", out var v))
            {
                if (v.ValueKind == JsonValueKind.Number) return (int)v.GetDouble();
                if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out var i)) return i;
            }
            return 0;
        }

        private static string StatName(JsonElement stats, string name)
        {
            if (stats.TryGetProperty(name, out var node) && node.ValueKind == JsonValueKind.Object &&
                node.TryGetProperty("metadata", out var meta) &&
                meta.TryGetProperty("name", out var nm))
                return nm.GetString() ?? "";
            return "";
        }
    }
}
