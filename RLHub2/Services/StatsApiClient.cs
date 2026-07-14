using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using RLHub2.Models;

namespace RLHub2.Services
{
    // Background client for the official Rocket League Stats API.
    // Connects to the local game socket (127.0.0.1:49123), parses the concatenated
    // JSON event stream, and raises an event with a SessionMatch when a match ends.
    //
    // The feed does NOT contain MMR or playlist type — only live per-match stats,
    // so this powers a session tracker (W/L, goals/saves/assists), not the MMR chart.
    public sealed class StatsApiClient
    {
        public static StatsApiClient Instance { get; } = new();

        private const string Host = "127.0.0.1";
        private const int Port = 49123;

        private Thread? _thread;
        private volatile bool _running;
        private volatile bool _connected;
        private SynchronizationContext? _sync;

        // Raised (on the UI thread) whenever the connection state changes.
        public event Action<bool>? ConnectionChanged;
        // Raised (on the UI thread) with a finished match.
        public event Action<SessionMatch>? MatchLogged;

        public bool IsConnected => _connected;

        // When listening began — used to scope "this session".
        public DateTime StartedAt { get; private set; } = DateTime.Now;

        private readonly SessionStore _store = new();

        // Latest per-match snapshot, rebuilt from UpdateState events.
        private readonly Dictionary<string, PlayerStat> _players = new(StringComparer.OrdinalIgnoreCase);
        private string _lastMatchGuid = "";
        private string _loggedMatchGuid = "";

        private StatsApiClient() { }

        public void Start()
        {
            if (_running) return;
            _running = true;
            StartedAt = DateTime.Now;
            _sync = SynchronizationContext.Current;
            _thread = new Thread(RunLoop) { IsBackground = true, Name = "StatsApiClient" };
            _thread.Start();
        }

        public void Stop() => _running = false;

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
            while (_running)
            {
                try
                {
                    using var client = new TcpClient();
                    var ar = client.BeginConnect(Host, Port, null, null);
                    if (!ar.AsyncWaitHandle.WaitOne(2000) || !client.Connected)
                    {
                        SetConnected(false);
                        Sleep(3000);
                        continue;
                    }
                    client.EndConnect(ar);
                    SetConnected(true);

                    using var stream = client.GetStream();
                    ReadStream(stream);
                }
                catch
                {
                    // fall through to reconnect
                }
                SetConnected(false);
                _players.Clear();
                if (_running) Sleep(3000);
            }
        }

        private void Sleep(int ms)
        {
            int step = 0;
            while (_running && step < ms) { Thread.Sleep(100); step += 100; }
        }

        private readonly StringBuilder _buf = new();

        private void ReadStream(Stream stream)
        {
            var reader = new StreamReader(stream, Encoding.UTF8, false, 8192);
            var chunk = new char[8192];
            _buf.Clear();

            while (_running)
            {
                int read;
                try { read = reader.Read(chunk, 0, chunk.Length); }
                catch { break; }
                if (read <= 0) break; // socket closed

                _buf.Append(chunk, 0, read);
                ExtractObjects();

                // guard against unbounded growth on malformed input
                if (_buf.Length > 4_000_000) _buf.Clear();
            }
        }

        // Pull complete top-level JSON objects out of the buffer (string/escape aware).
        private void ExtractObjects()
        {
            int depth = 0, start = -1, consumedTo = 0;
            bool inStr = false, esc = false;

            for (int i = 0; i < _buf.Length; i++)
            {
                char c = _buf[i];
                if (inStr)
                {
                    if (esc) esc = false;
                    else if (c == '\\') esc = true;
                    else if (c == '"') inStr = false;
                    continue;
                }
                if (c == '"') { inStr = true; continue; }
                if (c == '{') { if (depth == 0) start = i; depth++; }
                else if (c == '}')
                {
                    if (depth > 0) depth--;
                    if (depth == 0 && start >= 0)
                    {
                        string json = _buf.ToString(start, i - start + 1);
                        consumedTo = i + 1;
                        try { HandleMessage(json); } catch { }
                    }
                }
            }

            if (consumedTo > 0) _buf.Remove(0, consumedTo);
        }

        private void HandleMessage(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string ev = GetString(root, "Event", "event") ?? "";
            if (ev.Length == 0) return;

            // Data may be a nested object OR a JSON-encoded string.
            if (!TryGetProp(root, out var data, "Data", "data")) return;
            if (data.ValueKind == JsonValueKind.String)
            {
                string inner = data.GetString() ?? "";
                if (inner.Length == 0) return;
                using var dataDoc = JsonDocument.Parse(inner);
                DispatchEvent(ev, dataDoc.RootElement);
            }
            else
            {
                DispatchEvent(ev, data);
            }
        }

        private void DispatchEvent(string ev, JsonElement data)
        {
            string e = ev.ToLowerInvariant();

            if (e.Contains("matchcreated") || e.Contains("match_created") ||
                e.Contains("initialized") || e.Contains("countdown"))
            {
                _players.Clear();
                _lastMatchGuid = GetString(data, "match_guid", "MatchGuid", "matchGuid") ?? _lastMatchGuid;
                return;
            }

            if (e.Contains("updatestate") || e.Contains("update_state"))
            {
                UpdateFromState(data);
                return;
            }

            if (e.Contains("matchended") || e.Contains("match_ended") ||
                e.Contains("podium") || e.Contains("finished"))
            {
                FinishMatch();
                return;
            }
        }

        private void UpdateFromState(JsonElement data)
        {
            string guid = GetString(data, "match_guid", "MatchGuid", "matchGuid") ?? "";
            if (guid.Length > 0) _lastMatchGuid = guid;

            if (!TryGetProp(data, out var players, "players", "Players")) return;
            if (players.ValueKind != JsonValueKind.Array) return;

            foreach (var p in players.EnumerateArray())
            {
                string name = GetString(p, "name", "Name") ?? "";
                if (name.Length == 0) continue;

                _players[name] = new PlayerStat
                {
                    Name = name,
                    Team = GetInt(p, "team_num", "TeamNum", "Team", "team"),
                    Goals = GetInt(p, "goals", "Goals"),
                    Saves = GetInt(p, "saves", "Saves"),
                    Assists = GetInt(p, "assists", "Assists"),
                    Shots = GetInt(p, "shots", "Shots"),
                    Score = GetInt(p, "score", "Score"),
                };
            }
        }

        private void FinishMatch()
        {
            if (_players.Count == 0) return;
            // avoid double-logging the same match (MatchEnded + PodiumStart both fire)
            if (_lastMatchGuid.Length > 0 && _lastMatchGuid == _loggedMatchGuid) return;

            // Find which of the user's accounts is playing (matches any alias, so a
            // renamed account is still recognised).
            PlayerStat? me = null;
            Models.Account? acc = null;
            foreach (var p in _players.Values)
            {
                var a = Helpers.Accounts.MatchByName(p.Name);
                if (a != null) { me = p; acc = a; break; }
            }

            // team goals from the snapshot
            var teamGoals = new Dictionary<int, int>();
            foreach (var p in _players.Values)
            {
                teamGoals.TryGetValue(p.Team, out int g);
                teamGoals[p.Team] = g + p.Goals;
            }

            int myTeam = me?.Team ?? 0;
            int mine = teamGoals.TryGetValue(myTeam, out var mg) ? mg : 0;
            int opp = 0;
            foreach (var kv in teamGoals) if (kv.Key != myTeam && kv.Value > opp) opp = kv.Value;

            var match = new SessionMatch
            {
                Time = DateTime.Now,
                Account = acc?.Name ?? "",
                Won = mine > opp,
                Goals = me?.Goals ?? 0,
                Saves = me?.Saves ?? 0,
                Assists = me?.Assists ?? 0,
                Shots = me?.Shots ?? 0,
                Score = me?.Score ?? 0,
                TeamGoals = mine,
                OppGoals = opp,
            };

            _loggedMatchGuid = _lastMatchGuid;
            _store.Append(match);
            _players.Clear();

            Post(() => MatchLogged?.Invoke(match));
        }

        // ===== JSON helpers (case/name tolerant) =====

        private static bool TryGetProp(JsonElement obj, out JsonElement value, params string[] names)
        {
            value = default;
            if (obj.ValueKind != JsonValueKind.Object) return false;
            foreach (var n in names)
                if (obj.TryGetProperty(n, out value)) return true;
            // case-insensitive fallback
            foreach (var prop in obj.EnumerateObject())
                foreach (var n in names)
                    if (string.Equals(prop.Name, n, StringComparison.OrdinalIgnoreCase))
                    {
                        value = prop.Value;
                        return true;
                    }
            return false;
        }

        private static string? GetString(JsonElement obj, params string[] names)
        {
            if (!TryGetProp(obj, out var v, names)) return null;
            return v.ValueKind switch
            {
                JsonValueKind.String => v.GetString(),
                JsonValueKind.Number => v.ToString(),
                _ => null
            };
        }

        private static int GetInt(JsonElement obj, params string[] names)
        {
            if (!TryGetProp(obj, out var v, names)) return 0;
            if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out int n)) return n;
            if (v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out int m)) return m;
            return 0;
        }

        private sealed class PlayerStat
        {
            public string Name = "";
            public int Team, Goals, Saves, Assists, Shots, Score;
        }
    }
}
