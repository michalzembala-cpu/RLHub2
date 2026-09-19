using System;
using System.Collections.Generic;
using System.Linq;
using RLHub2.Helpers;
using RLHub2.Models;
using RLHub2.Services;

namespace RLHub2.Assistant
{
    // Everything the assistant is allowed to do, in one place. Both brains share this catalog:
    // the offline matcher picks a tool by phrase, the LLM picks one by name through tool-calling.
    // Nothing else in the assistant touches a store, so "what can it actually do" is answered by
    // reading this one file — and a tool that isn't here cannot be invoked by either path.
    public static class AssistantTools
    {
        private static bool Pl => Localization.IsPolish;

        private static readonly List<AssistantTool> Catalog = Build();

        public static IReadOnlyList<AssistantTool> All => Catalog;

        public static AssistantTool? Find(string name) =>
            Catalog.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));

        // Neither a mishearing nor a model is trusted to stay inside a parameter's allowed set:
        // anything outside it is replaced with the declared default before a tool ever runs, so
        // a bogus value cannot reach a store by either path.
        public static Dictionary<string, string> Sanitize(
            AssistantTool tool, Dictionary<string, string> args)
        {
            foreach (var p in tool.Params)
            {
                if (p.Allowed.Length == 0) continue;
                if (!args.TryGetValue(p.Name, out var v) || !p.Allowed.Contains(v))
                    args[p.Name] = p.Default.Length > 0 ? p.Default : p.Allowed[0];
            }
            return args;
        }

        // ===================== data helpers =====================

        // "session" means the current sitting. RL and CS2 clear their session stores between
        // sittings, so the whole file is the session.
        private static DateTime Since(string period, GameId g) => period switch
        {
            "today" => DateTime.Today,
            "week" => DateTime.Today.AddDays(-7),
            "all" => DateTime.MinValue,
            _ => DateTime.MinValue,
        };

        private static (int Won, int Lost, int Total) Record(GameId g, string period)
        {
            var since = Since(period, g);
            switch (g)
            {
                case GameId.Cs2:
                {
                    var ms = new Cs2SessionStore().Load().Where(m => m.Time >= since).ToList();
                    return (ms.Count(m => m.Won), ms.Count(m => !m.Won && !m.Draw), ms.Count);
                }
                default:
                {
                    // The live session feed is the freshest source for Rocket League; ballchasing
                    // is the fallback for anything older than the current sitting.
                    var live = new SessionStore().LoadForActive().Where(m => m.Time >= since).ToList();
                    if (live.Count > 0) return (live.Count(m => m.Won), live.Count(m => !m.Won), live.Count);
                    var ball = new BallMatchStore().LoadForActive().Where(m => m.Date >= since).ToList();
                    return (ball.Count(m => m.Won), ball.Count(m => !m.Won), ball.Count);
                }
            }
        }

        // Most recent first, as plain win/loss flags — enough to measure a streak.
        private static List<bool> RecentResults(GameId g)
        {
            switch (g)
            {
                case GameId.Cs2:
                    return new Cs2SessionStore().Load().Where(m => !m.Draw)
                        .OrderByDescending(m => m.Time).Select(m => m.Won).ToList();
                default:
                {
                    var live = new SessionStore().LoadForActive().OrderByDescending(m => m.Time)
                        .Select(m => m.Won).ToList();
                    if (live.Count > 0) return live;
                    return new BallMatchStore().LoadForActive().OrderByDescending(m => m.Date)
                        .Select(m => m.Won).ToList();
                }
            }
        }

        private static int Streak(GameId g, out bool winning)
        {
            var r = RecentResults(g);
            winning = r.Count > 0 && r[0];
            if (r.Count == 0) return 0;
            int n = 0;
            foreach (var won in r)
            {
                if (won != r[0]) break;
                n++;
            }
            return n;
        }

        private static string NoData(GameId g) => Pl
            ? $"Nie mam jeszcze żadnych meczów w {Games.Name(g)}."
            : $"I have no {Games.Name(g)} matches logged yet.";

        // ===================== catalog =====================

        private static List<AssistantTool> Build() => new()
        {
            // ---------- reads ----------
            new AssistantTool
            {
                Name = "get_record",
                Kind = ToolKind.Read,
                Description = "Wins, losses and win rate for the game currently open, over a period.",
                Params =
                {
                    new ToolParam
                    {
                        Name = "period",
                        Description = "today, session, week or all",
                        Allowed = new[] { "today", "session", "week", "all" },
                        Default = "session",
                    },
                },
                Run = a =>
                {
                    var g = Games.Active;
                    var period = a.TryGetValue("period", out var p) && p.Length > 0 ? p : "session";
                    var (w, l, total) = Record(g, period);
                    if (total == 0) return NoData(g);
                    int pct = (int)Math.Round(100.0 * w / total);
                    var when = Pl
                        ? period switch { "today" => "dzisiaj", "week" => "w tym tygodniu", "all" => "łącznie", _ => "w tej sesji" }
                        : period switch { "today" => "today", "week" => "this week", "all" => "overall", _ => "this session" };
                    return Pl
                        ? $"{Games.Name(g)} {when}: {w} wygranych, {l} przegranych, {pct} procent skuteczności."
                        : $"{Games.Name(g)} {when}: {w} wins, {l} losses, {pct} percent win rate.";
                },
            },

            new AssistantTool
            {
                Name = "get_streak",
                Kind = ToolKind.Read,
                Description = "The current winning or losing streak for the game currently open.",
                Run = _ =>
                {
                    var g = Games.Active;
                    int n = Streak(g, out bool winning);
                    if (n == 0) return NoData(g);
                    if (Pl)
                        return winning ? $"Masz {n} wygranych z rzędu." : $"Masz {n} przegranych z rzędu.";
                    return winning ? $"You are on a {n} game win streak." : $"You are on a {n} game losing streak.";
                },
            },

            new AssistantTool
            {
                Name = "get_session",
                Kind = ToolKind.Read,
                Description = "Summary of the current session: record, streak and how it is trending.",
                Run = _ =>
                {
                    var g = Games.Active;
                    var (w, l, total) = Record(g, "session");
                    if (total == 0) return Pl
                        ? "Sesja jeszcze się nie zaczęła — brak meczów."
                        : "The session hasn't started yet — no matches.";
                    int pct = (int)Math.Round(100.0 * w / total);
                    int n = Streak(g, out bool winning);
                    var tail = Pl
                        ? (winning ? $" Ostatnio {n} z rzędu na plus." : $" Ostatnio {n} z rzędu na minus.")
                        : (winning ? $" Last {n} in a row won." : $" Last {n} in a row lost.");
                    return (Pl
                        ? $"Sesja: {total} meczów, {w} na {l}, {pct} procent."
                        : $"Session: {total} matches, {w} and {l}, {pct} percent.") + tail;
                },
            },

            new AssistantTool
            {
                Name = "get_rank",
                Kind = ToolKind.Read,
                Description = "Current rank, MMR or rating for the game currently open.",
                Run = _ =>
                {
                    switch (Games.Active)
                    {
                        case GameId.Cs2:
                        {
                            var store = new Cs2RatingStore();
                            var latest = store.Latest();
                            if (latest == null) return Pl
                                ? "Nie mam jeszcze zapisanego ratingu CS2."
                                : "I have no CS2 rating logged yet.";
                            int d = store.Delta();
                            var move = d == 0
                                ? ""
                                : Pl ? $" Zmiana: {(d > 0 ? "+" : "")}{d}." : $" Change: {(d > 0 ? "+" : "")}{d}.";
                            return (Pl
                                ? $"Twój rating Premier to {latest.Value}."
                                : $"Your Premier rating is {latest.Value}.") + move;
                        }
                        default:
                        {
                            var mmr = new MmrStore().LoadForActive()
                                .OrderByDescending(e => e.Timestamp).FirstOrDefault();
                            if (mmr == null) return Pl
                                ? "Nie mam jeszcze zapisanego MMR."
                                : "I have no MMR logged yet.";
                            var tier = RankMmr.TierName(mmr.Value);
                            return Pl
                                ? $"Twoje MMR w {mmr.Mode} to {mmr.Value}, czyli {tier}."
                                : $"Your {mmr.Mode} MMR is {mmr.Value}, which is {tier}.";
                        }
                    }
                },
            },

            new AssistantTool
            {
                Name = "get_last_match",
                Kind = ToolKind.Read,
                Description = "How the most recent match went, with its key numbers.",
                Run = _ =>
                {
                    switch (Games.Active)
                    {
                        case GameId.Cs2:
                        {
                            var m = new Cs2SessionStore().Load().OrderByDescending(x => x.Time).FirstOrDefault();
                            if (m == null) return NoData(GameId.Cs2);
                            var res = m.Draw
                                ? (Pl ? "remis" : "a draw")
                                : m.Won ? (Pl ? "wygrana" : "a win") : (Pl ? "przegrana" : "a loss");
                            return Pl
                                ? $"Ostatni mecz na {m.Map}: {res}, {m.RoundsWon} do {m.RoundsLost}. {m.Kills} zabójstw, {m.Deaths} śmierci, {m.Assists} asyst."
                                : $"Last match on {m.Map}: {res}, {m.RoundsWon} to {m.RoundsLost}. {m.Kills} kills, {m.Deaths} deaths, {m.Assists} assists.";
                        }
                        default:
                        {
                            var s = new SessionStore().LoadForActive().OrderByDescending(x => x.Time).FirstOrDefault();
                            if (s != null)
                            {
                                var res = s.Won ? (Pl ? "wygrana" : "a win") : (Pl ? "przegrana" : "a loss");
                                return Pl
                                    ? $"Ostatni mecz {s.Mode}: {res}, {s.TeamGoals} do {s.OppGoals}. {s.Goals} goli, {s.Saves} obron, {s.Assists} asyst."
                                    : $"Last {s.Mode} match: {res}, {s.TeamGoals} to {s.OppGoals}. {s.Goals} goals, {s.Saves} saves, {s.Assists} assists.";
                            }
                            var b = new BallMatchStore().LoadForActive().OrderByDescending(x => x.Date).FirstOrDefault();
                            if (b == null) return NoData(GameId.RocketLeague);
                            var r2 = b.Won ? (Pl ? "wygrana" : "a win") : (Pl ? "przegrana" : "a loss");
                            return Pl
                                ? $"Ostatni mecz {b.Mode}: {r2}, {b.TeamGoals} do {b.OppGoals}. {b.Goals} goli, {b.Saves} obron."
                                : $"Last {b.Mode} match: {r2}, {b.TeamGoals} to {b.OppGoals}. {b.Goals} goals, {b.Saves} saves.";
                        }
                    }
                },
            },

            new AssistantTool
            {
                Name = "get_goals",
                Kind = ToolKind.Read,
                Description = "The user's goal list and how much of it is done.",
                Run = _ =>
                {
                    var goals = new GoalStore().Load();
                    if (goals.Count == 0) return Pl ? "Nie masz jeszcze żadnych celów." : "You have no goals yet.";
                    var open = goals.Where(g => !g.Done).ToList();
                    if (open.Count == 0) return Pl
                        ? $"Wszystkie {goals.Count} cele odhaczone. Czysto."
                        : $"All {goals.Count} goals are done. Clean sheet.";
                    var list = string.Join(", ", open.Take(3).Select(g => g.Text));
                    var more = open.Count > 3
                        ? (Pl ? $" i {open.Count - 3} więcej" : $" and {open.Count - 3} more")
                        : "";
                    return Pl
                        ? $"Zostało {open.Count} z {goals.Count}: {list}{more}."
                        : $"{open.Count} of {goals.Count} left: {list}{more}.";
                },
            },

            // ---------- actions ----------
            new AssistantTool
            {
                Name = "open_page",
                Kind = ToolKind.Action,
                Description = "Open one of the app's pages.",
                Params =
                {
                    new ToolParam
                    {
                        Name = "page",
                        Description = "page key",
                        Required = true,
                        Allowed = new[]
                        {
                            "home", "mmr", "road", "coach", "session", "records", "news", "profile",
                            "tournaments", "seasons", "settings", "cs2", "cs2ai", "cs2xhair",
                            "cs2maps", "cs2prac",
                        },
                    },
                },
                Run = a =>
                {
                    var page = a.TryGetValue("page", out var p) ? p : "home";
                    if (!AssistantBridge.Navigate(page))
                        return Pl
                            ? "Tej strony nie ma w grze, która jest teraz otwarta."
                            : "That page doesn't belong to the game that's open right now.";
                    return Pl ? "Otwieram." : "Opening.";
                },
            },

            new AssistantTool
            {
                Name = "toggle_overlay",
                Kind = ToolKind.Action,
                Description = "Show or hide the always-on-top overlay for the game currently open.",
                Run = _ =>
                {
                    OverlayWindow.Toggle();
                    return OverlayWindow.IsOpen
                        ? (Pl ? "Overlay włączony." : "Overlay on.")
                        : (Pl ? "Overlay wyłączony." : "Overlay off.");
                },
            },

            new AssistantTool
            {
                Name = "switch_game",
                Kind = ToolKind.Action,
                Description = "Switch the app to a different game.",
                Params =
                {
                    new ToolParam
                    {
                        Name = "game",
                        Description = "rl or cs2",
                        Required = true,
                        Allowed = new[] { "rl", "cs2" },
                    },
                },
                Run = a =>
                {
                    var key = a.TryGetValue("game", out var g) ? g : "";
                    GameId? id = key switch
                    {
                        "cs2" => GameId.Cs2,
                        "rl" => GameId.RocketLeague,
                        _ => null,
                    };
                    if (id == null) return Pl ? "Nie znam takiej gry." : "I don't know that game.";
                    Games.SetActive(id.Value);
                    AssistantBridge.Navigate(Games.HomePage(id.Value));
                    return Pl ? $"Przełączam na {Games.Name(id.Value)}." : $"Switching to {Games.Name(id.Value)}.";
                },
            },

            // ---------- writes (always confirmed out loud first) ----------
            new AssistantTool
            {
                Name = "add_goal",
                Kind = ToolKind.Write,
                Description = "Add a goal to the goal list.",
                Params =
                {
                    new ToolParam { Name = "text", Description = "the goal, in the user's own words", Required = true },
                },
                Preview = a =>
                {
                    var t = a.TryGetValue("text", out var x) ? x : "";
                    return Pl ? $"Dodać cel: {t}?" : $"Add the goal: {t}?";
                },
                Run = a =>
                {
                    var text = (a.TryGetValue("text", out var t) ? t : "").Trim();
                    if (text.Length == 0) return Pl ? "Nie usłyszałem treści celu." : "I didn't catch the goal.";
                    var store = new GoalStore();
                    var goals = store.Load();
                    goals.Add(new Goal { Text = text, Done = false });
                    store.Save(goals);
                    AssistantBridge.Refresh();
                    return Pl ? "Dodane." : "Added.";
                },
            },

            new AssistantTool
            {
                Name = "complete_goal",
                Kind = ToolKind.Write,
                Description = "Mark a goal as done. Matches on any part of its text.",
                Params =
                {
                    new ToolParam { Name = "text", Description = "part of the goal's text", Required = true },
                },
                Preview = a =>
                {
                    var t = a.TryGetValue("text", out var x) ? x : "";
                    return Pl ? $"Odhaczyć cel zawierający: {t}?" : $"Mark the goal containing: {t}?";
                },
                Run = a =>
                {
                    var needle = (a.TryGetValue("text", out var t) ? t : "").Trim();
                    if (needle.Length == 0) return Pl ? "Nie usłyszałem, który cel." : "I didn't catch which goal.";
                    var store = new GoalStore();
                    var goals = store.Load();
                    var hit = goals.FirstOrDefault(g =>
                        !g.Done && g.Text.Contains(needle, StringComparison.CurrentCultureIgnoreCase));
                    if (hit == null) return Pl
                        ? "Nie znalazłem otwartego celu z takim tekstem."
                        : "I couldn't find an open goal matching that.";
                    hit.Done = true;
                    store.Save(goals);
                    AssistantBridge.Refresh();
                    return Pl ? $"Odhaczone: {hit.Text}." : $"Done: {hit.Text}.";
                },
            },

            new AssistantTool
            {
                Name = "reset_session",
                Kind = ToolKind.Write,
                Description = "Start a fresh session for the game currently open, clearing its logged matches.",
                Preview = _ => Pl
                    ? $"Wyczyścić sesję {Games.Name(Games.Active)}? Zapisanych meczów nie da się odzyskać."
                    : $"Clear the {Games.Name(Games.Active)} session? The logged matches can't be recovered.",
                Run = _ =>
                {
                    switch (Games.Active)
                    {
                        case GameId.Cs2: new Cs2SessionStore().Clear(); break;
                        default: new SessionStore().Clear(); break;
                    }
                    AssistantBridge.Refresh();
                    return Pl ? "Nowa sesja." : "Fresh session.";
                },
            },
        };
    }
}
