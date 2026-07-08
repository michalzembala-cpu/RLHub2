using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using RLHub2.Helpers;
using RLHub2.Models;

namespace RLHub2.Services
{
    // Reads real match data from ballchasing.com. Auth: raw token in the Authorization
    // header (no "Bearer"). The feed exposes per-player rank (tier/division) and core
    // stats per replay — no raw MMR, so MMR is approximated from rank via RankMmr.
    public class BallchasingService
    {
        private const string Base = "https://ballchasing.com/api";
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };

        public class NoKeyException : Exception { }

        private static HttpRequestMessage Req(HttpMethod method, string url, string key)
        {
            var r = new HttpRequestMessage(method, url);
            r.Headers.TryAddWithoutValidation("Authorization", key);
            r.Headers.TryAddWithoutValidation("Accept", "application/json");
            return r;
        }

        // Returns (ok, message). On success message is the steam name; otherwise an error.
        public async Task<(bool ok, string message)> ValidateAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return (false, "no key");
            try
            {
                using var req = Req(HttpMethod.Get, Base + "/", key);
                using var resp = await Http.SendAsync(req);
                if (resp.StatusCode == HttpStatusCode.Unauthorized) return (false, "401 invalid key");
                if (!resp.IsSuccessStatusCode) return (false, $"{(int)resp.StatusCode}");

                var json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                string name =
                    (root.TryGetProperty("steam_name", out var n) ? n.GetString() : null) ??
                    (root.TryGetProperty("name", out var n2) ? n2.GetString() : null) ?? "OK";
                return (true, name);
            }
            catch (Exception ex) { return (false, ex.GetType().Name); }
        }

        public enum UploadResult { Uploaded, Duplicate, RateLimited, Failed }

        // Uploads a local .replay file (private). Duplicate = already on ballchasing.
        public async Task<UploadResult> UploadReplayAsync(string filePath, string key)
        {
            if (string.IsNullOrWhiteSpace(key) || !File.Exists(filePath)) return UploadResult.Failed;
            try
            {
                using var content = new MultipartFormDataContent();
                var bytes = await File.ReadAllBytesAsync(filePath);
                var fileContent = new ByteArrayContent(bytes);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                content.Add(fileContent, "file", Path.GetFileName(filePath));

                using var req = new HttpRequestMessage(HttpMethod.Post, Base + "/v2/upload?visibility=private")
                { Content = content };
                req.Headers.TryAddWithoutValidation("Authorization", key);

                using var resp = await Http.SendAsync(req);
                if (resp.StatusCode == HttpStatusCode.Created) return UploadResult.Uploaded;
                if (resp.StatusCode == HttpStatusCode.Conflict) return UploadResult.Duplicate; // already uploaded
                if ((int)resp.StatusCode == 429) return UploadResult.RateLimited;
                return UploadResult.Failed;
            }
            catch { return UploadResult.Failed; }
        }

        // YOUR matches: fetches your uploaded replays (uploader=me) and AUTO-DETECTS which
        // player is you — the player present in the most replays (you're in every one of
        // your own games). nickHint is used only if it actually matches a player. Returns
        // the matches plus the detected in-game name.
        public async Task<(List<BallMatch> matches, string detectedNick, bool autoDetected)> GetOwnMatchesAsync(string nickHint, int count = 30)
        {
            string key = new SettingsStore().LoadBallchasingKey();
            if (string.IsNullOrWhiteSpace(key)) throw new NoKeyException();

            var ids = await FetchIdsAsync("uploader=me", count, key);
            var parsed = new List<PReplay>();
            foreach (var id in ids)
            {
                var pr = await FetchReplayAsync(id, key);
                if (pr != null) parsed.Add(pr);
                await Task.Delay(320);
            }
            if (parsed.Count == 0) return (new List<BallMatch>(), "", false);

            string hint = (nickHint ?? "").Trim();
            string identity;
            bool autoDetected;

            if (hint.Length > 0)
            {
                // Track ONLY the chosen main account. If it isn't present in the uploads,
                // return nothing rather than silently switching to another account.
                bool hintExists = parsed.Any(pr => pr.Players.Any(p =>
                    string.Equals(p.Name, hint, StringComparison.OrdinalIgnoreCase)));
                if (!hintExists) return (new List<BallMatch>(), "", false);
                identity = hint;
                autoDetected = false;
            }
            else
            {
                // No main set yet → auto-detect the player present in the most replays.
                var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var pr in parsed)
                    foreach (var p in pr.Players)
                        freq[p.Name] = freq.TryGetValue(p.Name, out var c) ? c + 1 : 1;
                identity = freq.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).FirstOrDefault().Key ?? "";
                autoDetected = true;
            }

            var matches = parsed
                .Select(pr => BuildMatch(pr, identity))
                .Where(m => m != null).Cast<BallMatch>()
                .OrderByDescending(m => m.Date).ToList();
            return (matches, identity, autoDetected);
        }

        // Read-only lookup of another player's public matches (by in-game name).
        public async Task<List<BallMatch>> GetPlayerMatchesAsync(string name, int count = 30)
        {
            string key = new SettingsStore().LoadBallchasingKey();
            if (string.IsNullOrWhiteSpace(key)) throw new NoKeyException();
            if (string.IsNullOrWhiteSpace(name)) return new List<BallMatch>();

            var ids = await FetchIdsAsync("player-name=" + Uri.EscapeDataString(name.Trim()), count, key);
            var matches = new List<BallMatch>();
            foreach (var id in ids)
            {
                var pr = await FetchReplayAsync(id, key);
                var m = pr == null ? null : BuildMatch(pr, name.Trim());
                if (m != null) matches.Add(m);
                await Task.Delay(320);
            }
            return matches.OrderByDescending(m => m.Date).ToList();
        }

        private async Task<List<string>> FetchIdsAsync(string filter, int count, string key)
        {
            var ids = new List<string>();
            try
            {
                var url = $"{Base}/replays?{filter}&sort-by=replay-date&sort-dir=desc&count={Math.Clamp(count, 1, 200)}";
                using var req = Req(HttpMethod.Get, url, key);
                using var resp = await Http.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return ids;

                var json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("list", out var list) && list.ValueKind == JsonValueKind.Array)
                    foreach (var r in list.EnumerateArray())
                        if (r.TryGetProperty("id", out var idEl) && idEl.GetString() is string id)
                            ids.Add(id);
            }
            catch { }
            return ids;
        }

        private async Task<PReplay?> FetchReplayAsync(string id, string key)
        {
            try
            {
                using var req = Req(HttpMethod.Get, $"{Base}/replays/{id}", key);
                using var resp = await Http.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return null;
                var json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                return ParseReplay(doc.RootElement, id);
            }
            catch { return null; }
        }

        // Parses a replay into all its players (skips replays not fully parsed by ballchasing).
        private static PReplay? ParseReplay(JsonElement root, string id)
        {
            string status = root.TryGetProperty("status", out var stEl) ? stEl.GetString() ?? "" : "";
            if (status != "ok") return null;

            var pr = new PReplay { Id = id };

            if (root.TryGetProperty("date", out var d) && d.GetString() is string ds &&
                DateTimeOffset.TryParse(ds, out var dto))
                pr.Date = dto.LocalDateTime;
            else
                pr.Date = DateTime.Now;

            pr.PlaylistId = root.TryGetProperty("playlist_id", out var pl) ? pl.GetString() ?? "" : "";
            pr.Mode = ModeOf(pr.PlaylistId);
            pr.Ranked = pr.PlaylistId.ToLowerInvariant().Contains("ranked");

            for (int ti = 0; ti < 2; ti++)
            {
                string color = ti == 0 ? "blue" : "orange";
                if (!root.TryGetProperty(color, out var team)) continue;
                pr.TeamGoals[ti] = TeamGoals(team);

                if (!team.TryGetProperty("players", out var players) || players.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var p in players.EnumerateArray())
                {
                    var pp = new PPlayer
                    {
                        Name = p.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                        Team = ti
                    };
                    if (p.TryGetProperty("id", out var idObj) && idObj.ValueKind == JsonValueKind.Object)
                        pp.Platform = idObj.TryGetProperty("platform", out var pf) ? pf.GetString() ?? "" : "";

                    if (p.TryGetProperty("stats", out var stats) && stats.TryGetProperty("core", out var core))
                    {
                        pp.Goals = CoreInt(core, "goals");
                        pp.Saves = CoreInt(core, "saves");
                        pp.Assists = CoreInt(core, "assists");
                        pp.Shots = CoreInt(core, "shots");
                        pp.Score = CoreInt(core, "score");
                        pp.Mvp = core.TryGetProperty("mvp", out var mv) && mv.ValueKind == JsonValueKind.True;
                    }
                    if (p.TryGetProperty("rank", out var rank) && rank.ValueKind == JsonValueKind.Object)
                    {
                        pp.RankName = rank.TryGetProperty("name", out var rn) ? rn.GetString() ?? "" : "";
                        pp.RankTier = rank.TryGetProperty("tier", out var rt) && rt.ValueKind == JsonValueKind.Number ? rt.GetInt32() : 0;
                        pp.RankDiv = rank.TryGetProperty("division", out var rd) && rd.ValueKind == JsonValueKind.Number ? rd.GetInt32() : 0;
                    }

                    if (pp.Name.Length > 0) pr.Players.Add(pp);
                }
            }
            return pr;
        }

        private static BallMatch? BuildMatch(PReplay pr, string identity)
        {
            var me = pr.Players.FirstOrDefault(p => string.Equals(p.Name, identity, StringComparison.OrdinalIgnoreCase));
            if (me == null) return null;

            int mine = pr.TeamGoals[me.Team];
            int opp = pr.TeamGoals[1 - me.Team];

            return new BallMatch
            {
                Id = pr.Id,
                Date = pr.Date,
                PlaylistId = pr.PlaylistId,
                Mode = pr.Mode,
                Ranked = pr.Ranked,
                TeamGoals = mine,
                OppGoals = opp,
                Won = mine > opp,
                Goals = me.Goals,
                Saves = me.Saves,
                Assists = me.Assists,
                Shots = me.Shots,
                Score = me.Score,
                Mvp = me.Mvp,
                RankName = me.RankName,
                RankTier = me.RankTier,
                RankDivision = me.RankDiv,
                MmrApprox = RankMmr.Approx(me.RankTier, me.RankDiv)
            };
        }

        private sealed class PPlayer
        {
            public string Name = "";
            public string Platform = "";
            public int Team, Goals, Saves, Assists, Shots, Score;
            public bool Mvp;
            public string RankName = "";
            public int RankTier, RankDiv;
        }

        private sealed class PReplay
        {
            public string Id = "";
            public DateTime Date;
            public string PlaylistId = "";
            public string Mode = "";
            public bool Ranked;
            public int[] TeamGoals = { 0, 0 };
            public List<PPlayer> Players = new();
        }

        // ===== helpers =====

        private static int TeamGoals(JsonElement team)
        {
            if (team.TryGetProperty("stats", out var s) && s.TryGetProperty("core", out var c))
                return CoreInt(c, "goals");
            // fallback: sum player goals
            int sum = 0;
            if (team.TryGetProperty("players", out var players) && players.ValueKind == JsonValueKind.Array)
                foreach (var p in players.EnumerateArray())
                    if (p.TryGetProperty("stats", out var ps) && ps.TryGetProperty("core", out var pc))
                        sum += CoreInt(pc, "goals");
            return sum;
        }

        private static int CoreInt(JsonElement core, string name)
        {
            if (core.TryGetProperty(name, out var v))
            {
                if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out int i)) return i;
                if (v.ValueKind == JsonValueKind.Number) return (int)v.GetDouble();
            }
            return 0;
        }

        private static string ModeOf(string playlistId)
        {
            string p = playlistId.ToLowerInvariant();
            if (p.Contains("duel")) return "1v1";
            if (p.Contains("double")) return "2v2";
            if (p.Contains("standard")) return "3v3";
            return "";
        }
    }
}
