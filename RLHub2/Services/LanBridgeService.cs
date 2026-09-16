using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RLHub2.Services
{
    // Most po lokalnej sieci: NexPlay wystawia podsumowanie sesji RL, NexAthlete (Android) je czyta.
    //
    // Mówi dokładnie tym samym protokołem co NexHub — GET /profile z nagłówkiem x-token i
    // odpowiedzią {"rl": {...}} — więc apka na telefonie nie wymaga żadnych zmian. Różnica jest
    // taka, że dane nigdy nie opuszczają mieszkania i nie trzeba konta w chmurze.
    //
    // Świadomie TcpListener, a nie HttpListener: HttpListener na Windows wymaga rezerwacji urlacl
    // albo uprawnień administratora, a to jest apka odpalana normalnie.
    //
    // Bezpieczeństwo: tylko odczyt, tylko z poprawnym tokenem, tylko w zasięgu twojego Wi-Fi.
    // Token jest generowany raz i leży w lan_bridge.json obok reszty danych NexPlay.
    public class LanBridgeService : IDisposable
    {
        public const int Port = 8777;

        private readonly MmrStore _mmrStore = new();
        private readonly SessionStore _sessionStore = new();
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;

        public string Token { get; private set; } = "";
        public string? LastError { get; private set; }
        public bool Running => _listener != null;

        public string Url
        {
            get
            {
                var ip = LocalIp();
                return ip == null ? $"http://<ip-laptopa>:{Port}" : $"http://{ip}:{Port}";
            }
        }

        private static string ConfigPath
        {
            get
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RLHub2");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "lan_bridge.json");
            }
        }

        public void Start()
        {
            if (_listener != null) return;
            Token = LoadOrCreateToken();
            try
            {
                _listener = new TcpListener(IPAddress.Any, Port);
                _listener.Start();
            }
            catch (Exception ex)
            {
                // Najczęściej: port zajęty albo zablokowany. Apka ma działać dalej bez mostu.
                LastError = ex.Message;
                _listener = null;
                return;
            }

            _cts = new CancellationTokenSource();
            _ = Task.Run(() => AcceptLoopAsync(_cts.Token));
            WriteConfig();
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts = null;
            try { _listener?.Stop(); } catch { }
            _listener = null;
        }

        public void Dispose() => Stop();

        private async Task AcceptLoopAsync(CancellationToken ct)
        {
            var listener = _listener;
            if (listener == null) return;

            while (!ct.IsCancellationRequested)
            {
                TcpClient client;
                try { client = await listener.AcceptTcpClientAsync(); }
                catch { break; }

                _ = Task.Run(() => HandleAsync(client));
            }
        }

        private async Task HandleAsync(TcpClient client)
        {
            try
            {
                using (client)
                {
                    client.ReceiveTimeout = 5000;
                    client.SendTimeout = 5000;
                    using var stream = client.GetStream();

                    var request = await ReadRequestAsync(stream);
                    if (request == null) return;

                    var lines = request.Head.Split(new[] { "\r\n" }, StringSplitOptions.None);
                    var requestLine = lines.FirstOrDefault() ?? "";
                    var token = HeaderValue(lines, "x-token");

                    // Preflight z przeglądarki — przydatne przy testowaniu z telefonu.
                    if (requestLine.StartsWith("OPTIONS", StringComparison.OrdinalIgnoreCase))
                    {
                        await WriteAsync(stream, 204, "");
                        return;
                    }

                    if (!string.Equals(token, Token, StringComparison.Ordinal))
                    {
                        await WriteAsync(stream, 401, "{\"error\":\"Zly token\"}");
                        return;
                    }

                    var parts = requestLine.Split(' ');
                    var method = parts.Length > 0 ? parts[0].ToUpperInvariant() : "";
                    var path = parts.Length > 1 ? parts[1] : "/";

                    if (method == "GET" && path.StartsWith("/profile"))
                    {
                        await WriteAsync(stream, 200, BuildProfileJson());
                        return;
                    }

                    // NexDrone wysyła tu sekcję drone po locie, tak samo jak do NexHuba.
                    if (method == "PUT" && (path == "/profile/drone" || path == "/profile/meta"))
                    {
                        var section = path.EndsWith("drone") ? "drone" : "meta";
                        MergeSection(section, request.Body);
                        await WriteAsync(stream, 200, "{\"ok\":true}");
                        return;
                    }

                    // Sekcji rl nie przyjmujemy — NexPlay i tak liczy ją na żywo z własnych danych.
                    if (method == "PUT" && path == "/profile/rl")
                    {
                        await WriteAsync(stream, 200, "{\"ok\":true,\"note\":\"rl liczone lokalnie\"}");
                        return;
                    }

                    await WriteAsync(stream, 404, "{\"error\":\"Nieznany endpoint\"}");
                }
            }
            catch
            {
                // Zerwane połączenie z telefonu nie może położyć NexPlay.
            }
        }

        private sealed class Request
        {
            public string Head = "";
            public string Body = "";
        }

        // Czytamy do końca nagłówków, a potem dobieramy tyle bajtów, ile obiecał Content-Length.
        private static async Task<Request?> ReadRequestAsync(NetworkStream stream)
        {
            var buffer = new byte[4096];
            var raw = new List<byte>();
            int headerEnd = -1;

            while (raw.Count < 64 * 1024)
            {
                var read = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (read <= 0) break;
                for (int i = 0; i < read; i++) raw.Add(buffer[i]);

                headerEnd = IndexOfHeaderEnd(raw);
                if (headerEnd >= 0) break;
            }
            if (raw.Count == 0) return null;
            if (headerEnd < 0) headerEnd = raw.Count;

            var head = Encoding.UTF8.GetString(raw.ToArray(), 0, headerEnd);
            var lines = head.Split(new[] { "\r\n" }, StringSplitOptions.None);
            var lengthText = HeaderValue(lines, "content-length");
            int.TryParse(lengthText, out var contentLength);

            var bodyStart = Math.Min(headerEnd + 4, raw.Count);
            var body = new List<byte>();
            for (int i = bodyStart; i < raw.Count; i++) body.Add(raw[i]);

            while (contentLength > 0 && body.Count < contentLength && body.Count < 256 * 1024)
            {
                var read = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (read <= 0) break;
                for (int i = 0; i < read; i++) body.Add(buffer[i]);
            }

            return new Request
            {
                Head = head,
                Body = Encoding.UTF8.GetString(body.ToArray())
            };
        }

        private static int IndexOfHeaderEnd(List<byte> raw)
        {
            for (int i = 0; i + 3 < raw.Count; i++)
            {
                if (raw[i] == 13 && raw[i + 1] == 10 && raw[i + 2] == 13 && raw[i + 3] == 10)
                    return i;
            }
            return -1;
        }

        /** Dokłada pola do zapisanej sekcji profilu (drone albo meta) i zapisuje na dysk. */
        private void MergeSection(string section, string json)
        {
            try
            {
                var store = LoadSections();
                if (!store.TryGetValue(section, out var existing) || existing == null)
                    existing = new Dictionary<string, JsonElement>();

                using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
                foreach (var prop in doc.RootElement.EnumerateObject())
                    existing[prop.Name] = prop.Value.Clone();

                store[section] = existing;
                File.WriteAllText(
                    SectionsPath,
                    JsonSerializer.Serialize(store, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        private Dictionary<string, Dictionary<string, JsonElement>> LoadSections()
        {
            try
            {
                if (File.Exists(SectionsPath))
                {
                    var loaded = JsonSerializer
                        .Deserialize<Dictionary<string, Dictionary<string, JsonElement>>>(
                            File.ReadAllText(SectionsPath));
                    if (loaded != null) return loaded;
                }
            }
            catch { }
            return new Dictionary<string, Dictionary<string, JsonElement>>();
        }

        private static string SectionsPath
        {
            get
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RLHub2");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "lan_bridge_profile.json");
            }
        }

        private static string HeaderValue(string[] lines, string name)
        {
            foreach (var line in lines)
            {
                var idx = line.IndexOf(':');
                if (idx <= 0) continue;
                if (line.Substring(0, idx).Trim().Equals(name, StringComparison.OrdinalIgnoreCase))
                    return line.Substring(idx + 1).Trim();
            }
            return "";
        }

        private static async Task WriteAsync(NetworkStream stream, int status, string body)
        {
            var reason = status switch
            {
                200 => "OK",
                204 => "No Content",
                401 => "Unauthorized",
                405 => "Method Not Allowed",
                _ => "Error"
            };
            var bytes = Encoding.UTF8.GetBytes(body);
            var header =
                $"HTTP/1.1 {status} {reason}\r\n" +
                "Content-Type: application/json; charset=utf-8\r\n" +
                $"Content-Length: {bytes.Length}\r\n" +
                "Access-Control-Allow-Origin: *\r\n" +
                "Access-Control-Allow-Headers: x-token, content-type\r\n" +
                "Cache-Control: no-store\r\n" +
                "Connection: close\r\n\r\n";
            var headerBytes = Encoding.ASCII.GetBytes(header);
            await stream.WriteAsync(headerBytes, 0, headerBytes.Length);
            if (bytes.Length > 0) await stream.WriteAsync(bytes, 0, bytes.Length);
            await stream.FlushAsync();
        }

        // Kształt odpowiedzi jest celowo identyczny z NexHubem, żeby NexAthlete nie musiał
        // rozróżniać, skąd dane przyszły.
        private string BuildProfileJson()
        {
            var entries = _mmrStore.LoadForActive();
            int? m1 = LatestForMode(entries, "1v1");
            int? m2 = LatestForMode(entries, "2v2");
            int? m3 = LatestForMode(entries, "3v3");

            var rl = new Dictionary<string, object>();
            if (m1.HasValue) rl["mmr_1v1"] = m1.Value;
            if (m2.HasValue) rl["mmr_2v2"] = m2.Value;
            if (m3.HasValue) rl["mmr_3v3"] = m3.Value;

            var rank = NexHubSyncService.RankFor(m1, m2, m3);
            if (rank != "Unranked") rl["rank"] = rank;

            // Zmiana MMR dzisiaj — pierwszy i ostatni odczyt z głównego trybu.
            var today = DateTime.Now.Date;
            var mainMode = m2.HasValue ? "2v2" : (m3.HasValue ? "3v3" : "1v1");
            var todayEntries = entries
                .Where(e => e.Mode == mainMode && e.Timestamp.ToLocalTime().Date == today)
                .OrderBy(e => e.Timestamp)
                .ToList();
            if (todayEntries.Count >= 2)
                rl["mmr_delta_today"] = todayEntries.Last().Value - todayEntries.First().Value;

            // Mecze i winrate z dzisiejszej sesji.
            var matches = _sessionStore.LoadForActive()
                .Where(m => m.Time.ToLocalTime().Date == today)
                .OrderBy(m => m.Time)
                .ToList();
            if (matches.Count > 0)
            {
                rl["games_today"] = matches.Count;
                var wins = matches.Count(m => m.Won);
                rl["winrate"] = Math.Round(wins * 100.0 / matches.Count, 1);
                var span = matches.Last().Time - matches.First().Time;
                var minutes = (int)Math.Round(span.TotalMinutes);
                // Ostatni mecz też trwał — doliczamy średnią długość meczu RL.
                if (minutes > 0) rl["session_minutes"] = minutes + 7;
            }

            rl["updatedAt"] = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            var stored = LoadSections();
            var profile = new Dictionary<string, object>
            {
                ["rl"] = rl,
                ["drone"] = stored.TryGetValue("drone", out var d) ? d : new Dictionary<string, JsonElement>(),
                ["meta"] = stored.TryGetValue("meta", out var m) ? m : new Dictionary<string, JsonElement>(),
                ["source"] = "nexplay-lan"
            };
            return JsonSerializer.Serialize(profile);
        }

        private static int? LatestForMode(List<Models.MmrEntry> entries, string mode)
        {
            var latest = entries.Where(e => e.Mode == mode)
                .OrderByDescending(e => e.Timestamp)
                .FirstOrDefault();
            return latest?.Value;
        }

        private string LoadOrCreateToken()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(ConfigPath));
                    if (doc.RootElement.TryGetProperty("token", out var t))
                    {
                        var existing = t.GetString();
                        if (!string.IsNullOrWhiteSpace(existing)) return existing!;
                    }
                }
            }
            catch { }
            return Guid.NewGuid().ToString("N");
        }

        private void WriteConfig()
        {
            try
            {
                var payload = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["token"] = Token,
                    ["url"] = Url,
                    ["port"] = Port,
                    ["updatedAt"] = DateTimeOffset.Now.ToUnixTimeMilliseconds()
                }, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, payload);
            }
            catch { }
        }

        // Adres w sieci domowej — pomijamy loopback, APIPA i interfejsy, które nie są podłączone.
        public static string? LocalIp()
        {
            try
            {
                return NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                                n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                    .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(a => a.Address.ToString())
                    .FirstOrDefault(ip => !ip.StartsWith("127.") && !ip.StartsWith("169.254."));
            }
            catch { return null; }
        }
    }
}
