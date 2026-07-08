using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using RLHub2.Helpers;
using RLHub2.Models;

namespace RLHub2.Services
{
    // Fetches real Rocket League news from Google News RSS (no API key, reliable).
    // Titles are translated to Polish via the free Google Translate endpoint.
    // Falls back to offline sample data when offline.
    public class NewsService
    {
        private static readonly HttpClient Http = CreateClient();

        // (search query, category) — order sets dedup priority (first wins).
        private static readonly (string Query, string Category)[] Feeds =
        {
            ("Rocket League RLCS esports", "Esports"),
            ("Rocket League patch notes update", "Updates"),
            ("Rocket League", "General"),
        };

        private static HttpClient CreateClient()
        {
            var c = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
            c.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                "(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
            return c;
        }

        public async Task<List<NewsItem>> GetNewsAsync()
        {
            try
            {
                var all = new List<NewsItem>();
                var seen = new HashSet<string>();

                foreach (var (query, category) in Feeds)
                {
                    foreach (var item in await FetchFeedAsync(query, category))
                    {
                        var key = item.Title.ToLowerInvariant().Trim();
                        if (key.Length == 0 || !seen.Add(key))
                            continue;
                        all.Add(item);
                    }
                }

                if (all.Count == 0)
                    return Fallback();

                // Translate titles to Polish only when the app language is Polish.
                if (Localization.IsPolish)
                {
                    var translated = await Task.WhenAll(all.Select(n => TranslateAsync(n.Title)));
                    for (int i = 0; i < all.Count; i++)
                        all[i].Title = translated[i];
                }

                return all.OrderByDescending(n => n.PublishedDate).ToList();
            }
            catch
            {
                return Fallback();
            }
        }

        private static async Task<List<NewsItem>> FetchFeedAsync(string query, string category)
        {
            var result = new List<NewsItem>();

            string url = "https://news.google.com/rss/search?q="
                         + Uri.EscapeDataString(query)
                         + "&hl=en-US&gl=US&ceid=US:en";

            var xml = await Http.GetStringAsync(url);
            var doc = XDocument.Parse(xml);

            foreach (var item in doc.Descendants("item").Take(7))
            {
                string rawTitle = (string?)item.Element("title") ?? "";
                string link = (string?)item.Element("link") ?? "";
                string pub = (string?)item.Element("pubDate") ?? "";
                string source = (string?)item.Element("source") ?? "";

                string title = WebUtility.HtmlDecode(rawTitle).Trim();

                // Google News titles look like "Headline - Publisher"; split off the publisher.
                int idx = title.LastIndexOf(" - ", StringComparison.Ordinal);
                if (idx > 0)
                {
                    if (string.IsNullOrEmpty(source))
                        source = title[(idx + 3)..].Trim();
                    title = title[..idx].Trim();
                }

                DateTime date = DateTime.TryParse(pub, out var d) ? d.ToLocalTime() : DateTime.Now;

                result.Add(new NewsItem
                {
                    Title = title,
                    Description = string.IsNullOrWhiteSpace(source) ? "Google News" : source,
                    Category = category,
                    Link = link,
                    PublishedDate = date
                });
            }

            return result;
        }

        // Free Google Translate endpoint (en/auto -> pl). Returns the original text on failure.
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

                var translated = sb.ToString();
                return string.IsNullOrWhiteSpace(translated) ? text : translated;
            }
            catch
            {
                return text;
            }
        }

        // ===== OFFLINE FALLBACK =====
        private static List<NewsItem> Fallback()
        {
            var now = DateTime.Now;
            return new List<NewsItem>
            {
                new NewsItem { Title = "RLCS 2026 — wyniki Majora", Description = "Offline • Esports", Category = "Esports", PublishedDate = now.AddHours(-3) },
                new NewsItem { Title = "Regional Open — kwalifikacje", Description = "Offline • Esports", Category = "Esports", PublishedDate = now.AddDays(-1) },
                new NewsItem { Title = "Patch notes v2.41", Description = "Offline • Updates", Category = "Updates", PublishedDate = now.AddHours(-8) },
                new NewsItem { Title = "Zmiany balansu rozgrywki", Description = "Offline • Updates", Category = "Updates", PublishedDate = now.AddDays(-2) },
                new NewsItem { Title = "Season 19 — start sezonu", Description = "Offline • General", Category = "General", PublishedDate = now.AddHours(-20) },
                new NewsItem { Title = "Limitowany event w grze", Description = "Offline • General", Category = "General", PublishedDate = now.AddDays(-3) },
            };
        }
    }
}
