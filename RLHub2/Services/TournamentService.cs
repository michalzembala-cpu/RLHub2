using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using System.Threading.Tasks;
using RLHub2.Helpers;
using RLHub2.Models;

namespace RLHub2.Services
{
    // Real, current Rocket League tournament coverage from Google News RSS (no key, reliable).
    // One query per category; falls back to a small offline list when offline.
    public class TournamentService
    {
        private static readonly HttpClient Http = CreateClient();

        // (search query, category) — order sets dedup priority.
        private static readonly (string Query, string Category)[] Feeds =
        {
            ("Rocket League RLCS", "RLCS"),
            ("Rocket League Major", "MAJOR"),
            ("Rocket League regional tournament", "REGIONAL"),
        };

        private static HttpClient CreateClient()
        {
            var c = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
            c.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                "(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
            return c;
        }

        public async Task<List<TournamentEvent>> GetTournamentsAsync()
        {
            try
            {
                var all = new List<TournamentEvent>();
                var seen = new HashSet<string>();

                foreach (var (query, category) in Feeds)
                {
                    foreach (var item in await FetchAsync(query, category))
                    {
                        var key = item.Name.ToLowerInvariant().Trim();
                        if (key.Length == 0 || !seen.Add(key))
                            continue;
                        all.Add(item);
                    }
                }

                if (all.Count == 0)
                    return Fallback();

                // Translate tournament titles to Polish when the app language is Polish.
                if (Localization.IsPolish)
                {
                    var translated = await Task.WhenAll(all.Select(t => TranslateAsync(t.Name)));
                    for (int i = 0; i < all.Count; i++)
                        all[i].Name = translated[i];
                }

                all.Sort((a, b) => b.Date.CompareTo(a.Date));
                return all;
            }
            catch
            {
                return Fallback();
            }
        }

        private static async Task<List<TournamentEvent>> FetchAsync(string query, string category)
        {
            var result = new List<TournamentEvent>();

            string url = "https://news.google.com/rss/search?q="
                         + Uri.EscapeDataString(query)
                         + "&hl=en-US&gl=US&ceid=US:en";

            var xml = await Http.GetStringAsync(url);
            var doc = XDocument.Parse(xml);

            foreach (var item in System.Linq.Enumerable.Take(doc.Descendants("item"), 8))
            {
                string rawTitle = (string?)item.Element("title") ?? "";
                string link = (string?)item.Element("link") ?? "";
                string pub = (string?)item.Element("pubDate") ?? "";
                string source = (string?)item.Element("source") ?? "";

                string title = WebUtility.HtmlDecode(rawTitle).Trim();
                int idx = title.LastIndexOf(" - ", StringComparison.Ordinal);
                if (idx > 0)
                {
                    if (string.IsNullOrEmpty(source)) source = title[(idx + 3)..].Trim();
                    title = title[..idx].Trim();
                }

                DateTime date = DateTime.TryParse(pub, out var d) ? d.ToLocalTime() : DateTime.Now;

                result.Add(new TournamentEvent
                {
                    Name = title,
                    Source = string.IsNullOrWhiteSpace(source) ? "Google News" : source,
                    Category = category,
                    Link = link,
                    Date = date
                });
            }

            return result;
        }

        // Free Google Translate endpoint (auto -> pl). Returns the original text on failure.
        private static async Task<string> TranslateAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            try
            {
                string url = "https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl=pl&dt=t&q="
                             + Uri.EscapeDataString(text);

                var json = await Http.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);

                var segments = doc.RootElement[0];
                var sb = new StringBuilder();
                foreach (var seg in segments.EnumerateArray())
                    if (seg.GetArrayLength() > 0 && seg[0].ValueKind == JsonValueKind.String)
                        sb.Append(seg[0].GetString());

                var result = sb.ToString();
                return string.IsNullOrWhiteSpace(result) ? text : result;
            }
            catch
            {
                return text;
            }
        }

        // ===== OFFLINE FALLBACK =====
        private static List<TournamentEvent> Fallback()
        {
            var now = DateTime.Now;
            return new List<TournamentEvent>
            {
                new TournamentEvent { Name = "RLCS — brak połączenia (offline)", Source = "—", Category = "RLCS", Date = now },
                new TournamentEvent { Name = "Major — brak połączenia (offline)", Source = "—", Category = "MAJOR", Date = now.AddDays(-1) },
            };
        }
    }
}
