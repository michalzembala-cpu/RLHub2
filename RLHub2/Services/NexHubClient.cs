using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace RLHub2.Services
{
    // Klient NexHub — wspólny backend z NexDrone (Android).
    // NexPlay wysyła MMR/rank/winrate, NexDrone czyta jak zapytasz Jarvis'a.
    public class NexHubClient
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private readonly string _token;

        public NexHubClient(string baseUrl, string token)
        {
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            _baseUrl = baseUrl.TrimEnd('/');
            _token = token;
        }

        public static async Task<string?> CreateProfileAsync(string baseUrl)
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            try
            {
                var resp = await http.PostAsync($"{baseUrl.TrimEnd('/')}/profile", null);
                var body = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                return doc.RootElement.TryGetProperty("token", out var t) ? t.GetString() : null;
            }
            catch { return null; }
        }

        public async Task<bool> PutRocketLeagueAsync(
            int? mmr1v1 = null,
            int? mmr2v2 = null,
            int? mmr3v3 = null,
            string? rank = null,
            int? gamesToday = null,
            double? winrate = null)
        {
            var payload = new Dictionary<string, object?>();
            if (mmr1v1.HasValue) payload["mmr_1v1"] = mmr1v1.Value;
            if (mmr2v2.HasValue) payload["mmr_2v2"] = mmr2v2.Value;
            if (mmr3v3.HasValue) payload["mmr_3v3"] = mmr3v3.Value;
            if (rank != null) payload["rank"] = rank;
            if (gamesToday.HasValue) payload["games_today"] = gamesToday.Value;
            if (winrate.HasValue) payload["winrate"] = winrate.Value;

            if (payload.Count == 0) return true;
            return await PutAsync("/profile/rl", payload);
        }

        public async Task<bool> PutMetaAsync(string? pilotName = null)
        {
            var payload = new Dictionary<string, object?>();
            if (pilotName != null) payload["pilot_name"] = pilotName;
            if (payload.Count == 0) return true;
            return await PutAsync("/profile/meta", payload);
        }

        private async Task<bool> PutAsync(string path, object payload)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl}{path}")
                {
                    Content = JsonContent.Create(payload),
                };
                req.Headers.Add("x-token", _token);
                var resp = await _http.SendAsync(req);
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }
    }
}
