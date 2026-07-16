using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using RLHub2.Models;

namespace RLHub2.Services
{
    // Listens for Counter-Strike 2's Game State Integration feed and logs finished matches.
    //
    // CS2 POSTs a JSON snapshot of the game to a local URL as you play. That gives kills,
    // deaths, assists, MVPs, score, map, mode and the round score — but NO rank and NO CS
    // Rating; Valve does not expose them. So, exactly like the Rocket League side, this
    // powers a session tracker, not a rating chart.
    //
    // Deliberately a raw TcpListener rather than HttpListener: HttpListener needs a URL
    // reservation (netsh/admin) on Windows, which a desktop app should not demand. The feed
    // is a plain POST, so it is cheaper to answer it directly than to require elevation.
    public sealed class Cs2GsiClient
    {
        public static Cs2GsiClient Instance { get; } = new();

        public const int Port = 49124;   // RL's Stats API sits on 49123; keep them adjacent

        // No packet for this long and we treat the game as gone (CS2 heartbeats every 10s).
        private static readonly TimeSpan Silence = TimeSpan.FromSeconds(30);

        private TcpListener? _listener;
        private Thread? _thread;
        private volatile bool _running;
        private volatile bool _connected;
        private SynchronizationContext? _sync;
        private DateTime _lastPacket = DateTime.MinValue;

        public event Action<bool>? ConnectionChanged;
        public event Action<Cs2Match>? MatchLogged;

        public bool IsConnected => _connected && DateTime.Now - _lastPacket < Silence;

        // When listening began — used to scope "this session".
        public DateTime StartedAt { get; private set; } = DateTime.Now;

        // Live state, for a page or overlay to show mid-match.
        public string CurrentMap { get; private set; } = "";
        public string CurrentMode { get; private set; } = "";
        public int RoundsWon { get; private set; }
        public int RoundsLost { get; private set; }

        private readonly Cs2SessionStore _store = new();

        private string _lastPhase = "";
        private string _loggedKey = "";

        // Damage and headshots arrive per round and reset when the next one starts, so the
        // running totals are banked as each round turns over. _roundDmg/_roundHs hold the
        // latest values for the round in progress; they only count once the round is done.
        private int _matchDmg, _matchHs;
        private int _roundDmg, _roundHs;
        private int _lastRound = -1;

        private Cs2GsiClient() { }

        public void Start()
        {
            if (_running) return;
            _running = true;
            StartedAt = DateTime.Now;
            _sync = SynchronizationContext.Current;
            _thread = new Thread(RunLoop) { IsBackground = true, Name = "Cs2GsiClient" };
            _thread.Start();
        }

        public void Stop()
        {
            _running = false;
            try { _listener?.Stop(); } catch { }
        }

        private void Post(Action a)
        {
            if (_sync != null) _sync.Post(_ => a(), null);
            else a();
        }

        private void SetConnected(bool v)
        {
            if (_connected == v) return;
            _connected = v;
            Post(() => ConnectionChanged?.Invoke(v));
        }

        private void RunLoop()
        {
            try
            {
                _listener = new TcpListener(IPAddress.Loopback, Port);
                _listener.Start();
            }
            catch
            {
                // port taken — nothing to do but stay quiet; the page shows "waiting"
                _running = false;
                return;
            }

            while (_running)
            {
                try
                {
                    using var client = _listener.AcceptTcpClient();
                    client.ReceiveTimeout = 5000;
                    using var stream = client.GetStream();

                    var body = ReadRequest(stream);

                    // CS2 doesn't care about the reply, but it waits for one.
                    var ok = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
                    stream.Write(ok, 0, ok.Length);

                    if (!string.IsNullOrEmpty(body))
                    {
                        _lastPacket = DateTime.Now;
                        SetConnected(true);
                        try { Handle(body!); } catch { /* one bad packet must not kill the loop */ }
                    }
                }
                catch
                {
                    if (!_running) break;
                }

                if (DateTime.Now - _lastPacket > Silence) SetConnected(false);
            }

            SetConnected(false);
        }

        // Minimal HTTP: skip to the blank line, then read exactly Content-Length bytes.
        private static string? ReadRequest(NetworkStream stream)
        {
            var head = new StringBuilder();
            int b, matched = 0;
            while (matched < 4 && (b = stream.ReadByte()) >= 0)
            {
                char c = (char)b;
                head.Append(c);
                matched = c == "\r\n\r\n"[matched] ? matched + 1 : (c == '\r' ? 1 : 0);
            }
            if (matched < 4) return null;

            int len = 0;
            foreach (var line in head.ToString().Split("\r\n"))
                if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                    int.TryParse(line.Substring(15).Trim(), out len);
            if (len <= 0) return null;

            var buf = new byte[len];
            int read = 0;
            while (read < len)
            {
                int n = stream.Read(buf, read, len - read);
                if (n <= 0) break;
                read += n;
            }
            return Encoding.UTF8.GetString(buf, 0, read);
        }

        private void Handle(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("map", out var map)) { _lastPhase = ""; return; }

            CurrentMap = Str(map, "name") ?? "";
            CurrentMode = Str(map, "mode") ?? "";

            string phase = Str(map, "phase") ?? "";

            // Which side am I on, and what's the score?
            root.TryGetProperty("player", out var player);
            string team = player.ValueKind == JsonValueKind.Object ? Str(player, "team") ?? "" : "";

            TrackRound(map, player);

            int ct = map.TryGetProperty("team_ct", out var tct) ? Int(tct, "score") : 0;
            int t = map.TryGetProperty("team_t", out var tt) ? Int(tt, "score") : 0;

            bool ctSide = team.Equals("CT", StringComparison.OrdinalIgnoreCase);
            RoundsWon = ctSide ? ct : t;
            RoundsLost = ctSide ? t : ct;

            // A match is over exactly once: when the map enters "gameover". Packets keep
            // arriving after that, so the result is keyed and only logged the first time.
            if (phase == "gameover" && _lastPhase != "gameover" && team.Length > 0)
            {
                string key = $"{CurrentMap}|{ct}-{t}|{Str(root.TryGetProperty("provider", out var pv) ? pv : default, "timestamp")}";
                if (key != _loggedKey)
                {
                    _loggedKey = key;
                    LogMatch(player, ctSide, ct, t);
                }
            }
            _lastPhase = phase;
        }

        // Bank the finished round's damage/headshots when the round number moves on.
        // Taking the last value seen rather than a sum: the round figures are running totals
        // for that round, so adding every packet would multiply them by the packet rate.
        private void TrackRound(JsonElement map, JsonElement player)
        {
            int round = Int(map, "round");

            if (round != _lastRound)
            {
                if (_lastRound >= 0)
                {
                    _matchDmg += _roundDmg;
                    _matchHs += _roundHs;
                }
                _roundDmg = 0;
                _roundHs = 0;
                _lastRound = round;
            }

            if (player.ValueKind == JsonValueKind.Object &&
                player.TryGetProperty("state", out var st))
            {
                _roundDmg = Math.Max(_roundDmg, Int(st, "round_totaldmg"));
                _roundHs = Math.Max(_roundHs, Int(st, "round_killhs"));
            }
        }

        private void LogMatch(JsonElement player, bool ctSide, int ct, int t)
        {
            int mine = ctSide ? ct : t;
            int opp = ctSide ? t : ct;

            var stats = player.TryGetProperty("match_stats", out var ms) ? ms : default;

            var m = new Cs2Match
            {
                Time = DateTime.Now,
                SteamId = Str(player, "steamid") ?? "",
                Map = CurrentMap,
                Mode = CurrentMode,
                Won = mine > opp,
                Draw = mine == opp,
                RoundsWon = mine,
                RoundsLost = opp,
                Kills = Int(stats, "kills"),
                Deaths = Int(stats, "deaths"),
                Assists = Int(stats, "assists"),
                Mvps = Int(stats, "mvps"),
                Score = Int(stats, "score"),

                // the last round never turns over, so bank it here
                Damage = _matchDmg + _roundDmg,
                HeadshotKills = _matchHs + _roundHs,
            };

            _store.Append(m);
            ResetMatchTotals();
            Post(() => MatchLogged?.Invoke(m));
        }

        private void ResetMatchTotals()
        {
            _matchDmg = _matchHs = _roundDmg = _roundHs = 0;
            _lastRound = -1;
        }

        private static string? Str(JsonElement e, string name)
        {
            if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(name, out var v)) return null;
            return v.ValueKind == JsonValueKind.String ? v.GetString() : v.ToString();
        }

        private static int Int(JsonElement e, string name)
        {
            if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(name, out var v)) return 0;
            return v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out int i) ? i : 0;
        }
    }
}
