using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using RLHub2.Models;

namespace RLHub2.Services
{
    // Reads a public Overwatch 2 career profile through OverFast (an open, unofficial API that
    // scrapes the profile Blizzard itself publishes). No official OW2 stats API exists; this is
    // the same data source Overbuff uses. The profile must be set to Public in the game, or the
    // stats endpoints return 404 and we report the career as private.
    public class OverFastClient
    {
        private const string Base = "https://overfast-api.tekrop.fr";

        private static readonly HttpClient Http = new()
        {
            Timeout = TimeSpan.FromSeconds(20),
        };

        static OverFastClient()
        {
            // Identify ourselves politely; the hosted instance is a courtesy service.
            if (!Http.DefaultRequestHeaders.UserAgent.TryParseAdd("NexPlay/1.0 (+overwatch)"))
                Http.DefaultRequestHeaders.Add("User-Agent", "NexPlay");
        }

        // Battle.net BattleTag "Nick#21837" -> the OverFast player id "Nick-21837".
        public static string ToPlayerId(string battleTag)
            => (battleTag ?? "").Trim().Replace('#', '-');

        public async Task<OwProfile> FetchAsync(string battleTag, string platform)
        {
            var p = new OwProfile();
            var dashId = ToPlayerId(battleTag);
            if (string.IsNullOrWhiteSpace(dashId) || !dashId.Contains('-'))
            {
                p.Error = "no-tag";
                return p;
            }
            platform = platform == "console" ? "console" : "pc";

            try
            {
                // Resolve the player id. Blizzard has migrated career URLs to a hashed id, so the
                // legacy "Name-1234" form 404s for many accounts. Try it first (when it works it's
                // an exact match on the discriminator), then fall back to the name search, which
                // returns the hashed id the modern profile pages use.
                var sumResp = await Http.GetAsync($"{Base}/players/{dashId}/summary");
                if ((int)sumResp.StatusCode == 429) { p.Error = "rate-limited"; return p; }

                if (sumResp.StatusCode == HttpStatusCode.NotFound)
                {
                    var hashedId = await SearchIdAsync(battleTag);
                    if (hashedId != null)
                        sumResp = await Http.GetAsync($"{Base}/players/{hashedId}/summary");

                    if (sumResp.StatusCode == HttpStatusCode.NotFound)
                    {
                        // OverFast marks a freshly-public (or just-requested) profile with a
                        // retry_after while it caches the career page — that is "indexing", not
                        // "gone". Anything else genuinely isn't there.
                        var body = await sumResp.Content.ReadAsStringAsync();
                        p.Error = body.Contains("retry_after") ? "indexing" : "not-found";
                        return p;
                    }
                    if ((int)sumResp.StatusCode == 429) { p.Error = "rate-limited"; return p; }
                    if (hashedId != null) dashId = hashedId;   // reuse the working id for stats
                }
                sumResp.EnsureSuccessStatusCode();

                using (var doc = JsonDocument.Parse(await sumResp.Content.ReadAsStringAsync()))
                    ParseSummary(doc.RootElement, p, platform);

                // --- stats/summary: per-role and per-hero aggregates (competitive) ---
                var statsUrl = $"{Base}/players/{dashId}/stats/summary" +
                               $"?gamemode=competitive&platform={platform}";
                var stResp = await Http.GetAsync(statsUrl);
                if (stResp.StatusCode == HttpStatusCode.NotFound)
                {
                    // Profile found but the career is private (or has no competitive data).
                    p.Found = true;
                    if (p.Ranks.Count == 0) p.Private = true;
                    return p;
                }
                if (stResp.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(await stResp.Content.ReadAsStringAsync());
                    ParseStats(doc.RootElement, p);
                }

                p.Found = true;
                if (p.Ranks.Count == 0 && p.General == null) p.Private = true;
                return p;
            }
            catch (Exception ex)
            {
                p.Error = ex.Message;
                return p;
            }
        }

        // Resolve a BattleTag to the hashed player id via name search. The search returns accounts
        // by display name but not the #discriminator, so when several share a name we can't tell
        // them apart — we take the first public match, which is right for the common single-hit case.
        private async Task<string?> SearchIdAsync(string battleTag)
        {
            try
            {
                var name = (battleTag ?? "").Split('#', '-')[0].Trim();
                if (name.Length == 0) return null;
                var resp = await Http.GetAsync($"{Base}/players?name={Uri.EscapeDataString(name)}");
                if (!resp.IsSuccessStatusCode) return null;
                using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                if (!doc.RootElement.TryGetProperty("results", out var results) ||
                    results.ValueKind != JsonValueKind.Array) return null;

                string? first = null;
                foreach (var r in results.EnumerateArray())
                {
                    var pid = Str(r, "player_id");
                    if (string.IsNullOrEmpty(pid)) continue;
                    bool pub = r.TryGetProperty("is_public", out var ip) &&
                               ip.ValueKind == JsonValueKind.True;
                    // Prefer an exact case-insensitive name match that is public.
                    if (pub && string.Equals(Str(r, "name"), name, StringComparison.OrdinalIgnoreCase))
                        return pid;
                    first ??= pid;
                }
                return first;
            }
            catch { return null; }
        }

        private static void ParseSummary(JsonElement root, OwProfile p, string platform)
        {
            p.Username = Str(root, "username");
            p.AvatarUrl = Str(root, "avatar");
            p.Title = Str(root, "title");
            if (root.TryGetProperty("endorsement", out var end) && end.ValueKind == JsonValueKind.Object)
                p.Endorsement = (int)Num(end, "level");

            if (!root.TryGetProperty("competitive", out var comp) || comp.ValueKind != JsonValueKind.Object)
                return;
            if (!comp.TryGetProperty(platform, out var plat) || plat.ValueKind != JsonValueKind.Object)
                return;

            foreach (var role in new[] { "tank", "damage", "support" })
            {
                if (!plat.TryGetProperty(role, out var r) || r.ValueKind != JsonValueKind.Object)
                    continue;
                p.Ranks.Add(new OwRank
                {
                    Role = role,
                    Division = Str(r, "division"),
                    Tier = (int)Num(r, "tier"),
                    RankIcon = Str(r, "rank_icon"),
                });
            }
        }

        private static void ParseStats(JsonElement root, OwProfile p)
        {
            if (root.TryGetProperty("general", out var gen) && gen.ValueKind == JsonValueKind.Object)
                p.General = ReadBlock(gen, "general");

            if (root.TryGetProperty("roles", out var roles) && roles.ValueKind == JsonValueKind.Object)
                foreach (var role in new[] { "tank", "damage", "support" })
                    if (roles.TryGetProperty(role, out var r) && r.ValueKind == JsonValueKind.Object)
                        p.Roles.Add(ReadBlock(r, role));

            if (root.TryGetProperty("heroes", out var heroes) && heroes.ValueKind == JsonValueKind.Object)
                foreach (var h in heroes.EnumerateObject())
                {
                    if (h.Value.ValueKind != JsonValueKind.Object) continue;
                    p.Heroes.Add(new OwHero
                    {
                        Key = h.Name,
                        Name = Prettify(h.Name),
                        GamesPlayed = (int)Num(h.Value, "games_played"),
                        Winrate = Num(h.Value, "winrate"),
                        Kda = Num(h.Value, "kda"),
                        TimePlayedSeconds = (long)Num(h.Value, "time_played"),
                    });
                }
        }

        private static OwStatBlock ReadBlock(JsonElement e, string key)
        {
            var b = new OwStatBlock
            {
                Key = key,
                GamesPlayed = (int)Num(e, "games_played"),
                GamesWon = (int)Num(e, "games_won"),
                GamesLost = (int)Num(e, "games_lost"),
                TimePlayedSeconds = (long)Num(e, "time_played"),
                Winrate = Num(e, "winrate"),
                Kda = Num(e, "kda"),
            };
            if (e.TryGetProperty("average", out var avg) && avg.ValueKind == JsonValueKind.Object)
            {
                b.AvgEliminations = Num(avg, "eliminations");
                b.AvgAssists = Num(avg, "assists");
                b.AvgDeaths = Num(avg, "deaths");
                b.AvgDamage = Num(avg, "damage");
                b.AvgHealing = Num(avg, "healing");
            }
            if (e.TryGetProperty("total", out var tot) && tot.ValueKind == JsonValueKind.Object)
            {
                b.TotalEliminations = (long)Num(tot, "eliminations");
                b.TotalAssists = (long)Num(tot, "assists");
                b.TotalDeaths = (long)Num(tot, "deaths");
                b.TotalDamage = (long)Num(tot, "damage");
                b.TotalHealing = (long)Num(tot, "healing");
            }
            return b;
        }

        // ---- JSON helpers: values may arrive as numbers or numeric strings ----
        private static string Str(JsonElement e, string prop)
            => e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? (v.GetString() ?? "") : "";

        private static double Num(JsonElement e, string prop)
        {
            if (!e.TryGetProperty(prop, out var v)) return 0;
            if (v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d)) return d;
            if (v.ValueKind == JsonValueKind.String &&
                double.TryParse(v.GetString(), System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, out var s)) return s;
            return 0;
        }

        // "soldier-76" -> "Soldier 76", "wrecking-ball" -> "Wrecking Ball"
        private static string Prettify(string key)
        {
            if (string.IsNullOrEmpty(key)) return key;
            var parts = key.Replace('_', '-').Split('-', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
                parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
            return string.Join(' ', parts);
        }

        // Downloads the avatar image bytes (small PNG). Returns null on any failure.
        public async Task<byte[]?> DownloadAvatarAsync(string url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url)) return null;
                return await Http.GetByteArrayAsync(url);
            }
            catch { return null; }
        }
    }
}
