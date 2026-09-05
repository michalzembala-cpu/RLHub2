using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RLHub2.Helpers;

namespace RLHub2.Assistant
{
    public sealed class IntentHit
    {
        public AssistantTool Tool = null!;
        public Dictionary<string, string> Args = new();
    }

    // The offline half of the brain: phrase matching over the same tool catalog the LLM uses.
    // It exists so the assistant is useful the moment the app is installed — no key, no network,
    // no per-question cost — and so the common commands stay instant even when the LLM is on.
    // Everything is matched against a diacritic-stripped, lowercased form of the utterance,
    // because speech recognition is inconsistent about Polish diacritics and a user typing into
    // the box rarely bothers with them either.
    public static class IntentMatcher
    {
        // Rules are tried in order, so the specific ones (write, then action) come before the
        // broad read rules — otherwise "clear the session" would match the "session" read.
        private sealed class Rule
        {
            public string Tool = "";

            // Every group must contribute at least one hit for the rule to fire.
            public string[][] Groups = Array.Empty<string[]>();

            // Pulls slot values out of the utterance. Gets the normalized form and the original.
            public Func<string, string, Dictionary<string, string>>? Args;
        }

        public static string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s.ToLowerInvariant())
            {
                sb.Append(ch switch
                {
                    'ą' => 'a', 'ć' => 'c', 'ę' => 'e', 'ł' => 'l', 'ń' => 'n',
                    'ó' => 'o', 'ś' => 's', 'ż' => 'z', 'ź' => 'z',
                    _ => ch,
                });
            }
            // Collapse punctuation to spaces so "winrate?" and "winrate" match the same way.
            var cleaned = new StringBuilder(sb.Length);
            foreach (var ch in sb.ToString())
                cleaned.Append(char.IsLetterOrDigit(ch) ? ch : ' ');
            return string.Join(" ", cleaned.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        // "tak"/"yes" and friends, used to settle a pending write.
        public static bool IsYes(string s)
        {
            var n = Normalize(s);
            return n is "tak" or "yes" or "ok" or "okej" or "jasne" or "dawaj" or "potwierdzam"
                or "zapisz" or "confirm" or "yep" or "sure" or "do it";
        }

        public static bool IsNo(string s)
        {
            var n = Normalize(s);
            return n is "nie" or "no" or "anuluj" or "cancel" or "stop" or "nope" or "zostaw";
        }

        private static readonly Dictionary<string, string> PageWords = new()
        {
            ["glowna"] = "home", ["start"] = "home", ["home"] = "home", ["pulpit"] = "home",
            ["mmr"] = "mmr",
            ["droga"] = "road", ["road"] = "road",
            ["trener"] = "coach", ["coach"] = "coach",
            ["rekordy"] = "records", ["records"] = "records",
            ["aktualnosci"] = "news", ["nowosci"] = "news", ["news"] = "news",
            ["profil"] = "profile", ["profile"] = "profile",
            ["turnieje"] = "tournaments", ["tournaments"] = "tournaments",
            ["sezony"] = "seasons", ["seasons"] = "seasons",
            ["ustawienia"] = "settings", ["settings"] = "settings",
            ["celownik"] = "cs2xhair", ["crosshair"] = "cs2xhair",
            ["mapy"] = "cs2maps", ["maps"] = "cs2maps",
            ["trening"] = "cs2prac", ["practice"] = "cs2prac",
            ["insights"] = "cs2ai",
        };

        private static string PeriodOf(string n)
        {
            if (n.Contains("dzis") || n.Contains("dzisiaj") || n.Contains("today")) return "today";
            if (n.Contains("tydzien") || n.Contains("tygodni") || n.Contains("week")) return "week";
            if (n.Contains("lacznie") || n.Contains("wszystk") || n.Contains("overall") || n.Contains(" all ")) return "all";
            return "session";
        }

        // Everything after the last trigger word, taken from the ORIGINAL text so the goal keeps
        // its capitals and diacritics — it is shown back to the user and stored verbatim.
        private static string Tail(string original, params string[] triggers)
        {
            var lower = original.ToLowerInvariant();
            int best = -1;
            foreach (var t in triggers)
            {
                int i = lower.LastIndexOf(t, StringComparison.Ordinal);
                if (i >= 0 && i + t.Length > best) best = i + t.Length;
            }
            if (best < 0) return "";
            return original.Substring(best).Trim(' ', ',', ':', '.', '-');
        }

        private static readonly List<Rule> Rules = new()
        {
            // ---------- writes first: they are the most specific phrasings ----------
            new Rule
            {
                Tool = "reset_session",
                Groups = new[]
                {
                    new[] { "nowa sesja", "wyczysc sesje", "zresetuj sesje", "reset session", "clear session", "new session" },
                },
            },
            new Rule
            {
                Tool = "add_goal",
                Groups = new[] { new[] { "dodaj cel", "nowy cel", "add goal", "new goal" } },
                Args = (n, orig) => new() { ["text"] = Tail(orig, "dodaj cel", "nowy cel", "add goal", "new goal") },
            },
            new Rule
            {
                Tool = "complete_goal",
                Groups = new[] { new[] { "odhacz", "ukonczylem", "zrobione", "complete goal", "mark goal", "goal done" } },
                Args = (n, orig) => new()
                {
                    ["text"] = Tail(orig, "odhacz", "ukończyłem", "ukonczylem", "zrobione", "complete goal", "mark goal"),
                },
            },
            new Rule
            {
                Tool = "log_ow_match",
                Groups = new[]
                {
                    new[] { "zapisz", "dopisz", "dodaj", "log", "add" },
                    new[] { "wygrana", "wygrane", "przegrana", "przegrane", "remis", "win", "loss", "lost", "draw" },
                },
                Args = (n, _) =>
                {
                    var result = n.Contains("remis") || n.Contains("draw") ? "d"
                        : n.Contains("przegran") || n.Contains("loss") || n.Contains("lost") ? "l"
                        : "w";
                    var role =
                        n.Contains("tank") || n.Contains("czolg") ? "tank"
                        : n.Contains("damage") || n.Contains("dps") || n.Contains("dd") ? "damage"
                        : n.Contains("support") || n.Contains("sup") || n.Contains("wsparcie") ? "support"
                        : "";
                    return new() { ["result"] = result, ["role"] = role };
                },
            },

            // ---------- actions ----------
            new Rule
            {
                Tool = "toggle_overlay",
                Groups = new[] { new[] { "overlay", "nakladka", "nakladke" } },
            },
            new Rule
            {
                Tool = "switch_game",
                Groups = new[]
                {
                    new[] { "przelacz", "zmien", "switch", "wroc do", "go to" },
                    new[] { "rocket", "rl", "cs2", "counter", "cs", "overwatch", "ow" },
                },
                Args = (n, _) =>
                {
                    var game = n.Contains("overwatch") || n.Contains(" ow") ? "ow"
                        : n.Contains("cs2") || n.Contains("counter") || n.Contains(" cs ") ? "cs2"
                        : "rl";
                    return new() { ["game"] = game };
                },
            },
            new Rule
            {
                Tool = "open_page",
                Groups = new[]
                {
                    new[] { "otworz", "pokaz", "przejdz", "wejdz", "open", "show", "go to" },
                    new[]
                    {
                        "glowna", "start", "home", "pulpit", "mmr", "droga", "road", "trener", "coach",
                        "sesja", "sesje", "sesji", "session", "rekordy", "records", "aktualnosci",
                        "nowosci", "news", "profil", "profile", "turnieje", "tournaments", "sezony",
                        "seasons", "ustawienia", "settings", "celownik", "crosshair", "mapy", "maps",
                        "trening", "practice", "insights",
                    },
                },
                Args = (n, _) =>
                {
                    // The session page is per-game: Overwatch keeps its own.
                    if (n.Contains("sesj") || n.Contains("session"))
                        return new() { ["page"] = Games.Active == GameId.Overwatch ? "owsession" : "session" };
                    foreach (var word in n.Split(' '))
                        if (PageWords.TryGetValue(word, out var key))
                            return new() { ["page"] = key };
                    return new() { ["page"] = "home" };
                },
            },

            // ---------- reads ----------
            new Rule
            {
                Tool = "get_streak",
                Groups = new[] { new[] { "seria", "z rzedu", "streak", "pod rzad" } },
            },
            new Rule
            {
                Tool = "get_last_match",
                Groups = new[] { new[] { "ostatni mecz", "ostatnia gra", "ostatniego meczu", "last match", "last game", "previous match" } },
            },
            new Rule
            {
                Tool = "get_rank",
                Groups = new[] { new[] { "ranga", "range", "rangi", "rank", "mmr", "rating", "elo", "dywizja" } },
            },
            new Rule
            {
                Tool = "get_goals",
                Groups = new[] { new[] { "cel", "cele", "celow", "celi", "goal", "goals" } },
            },
            new Rule
            {
                Tool = "get_record",
                Groups = new[]
                {
                    new[]
                    {
                        "winrate", "win rate", "skutecznosc", "bilans", "procent", "ile wygralem",
                        "ile wygranych", "ile przegranych", "record", "wygranych", "przegranych",
                    },
                },
                Args = (n, _) => new() { ["period"] = PeriodOf(n) },
            },
            new Rule
            {
                Tool = "get_session",
                Groups = new[] { new[] { "sesja", "sesji", "sesje", "session", "jak mi idzie", "how am i doing" } },
            },
        };

        public static IntentHit? Match(string utterance)
        {
            var n = Normalize(utterance);
            if (n.Length == 0) return null;

            foreach (var rule in Rules)
            {
                bool all = rule.Groups.All(group => group.Any(word => n.Contains(Normalize(word))));
                if (!all) continue;

                var tool = AssistantTools.Find(rule.Tool);
                if (tool == null) continue;

                var args = rule.Args?.Invoke(n, utterance) ?? new Dictionary<string, string>();
                return new IntentHit { Tool = tool, Args = AssistantTools.Sanitize(tool, args) };
            }
            return null;
        }
    }
}
