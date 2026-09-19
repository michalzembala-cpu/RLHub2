using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RLHub.Helpers;
using RLHub2.Helpers;
using System.Linq;
using RLHub2.Assistant.Ml;
using RLHub2.Services;

namespace RLHub2.Assistant
{
    public sealed class BrainResult
    {
        public string Text = "";

        // Set when a write is queued and the assistant is waiting for a spoken yes or no.
        public AssistantTool? Pending;
        public Dictionary<string, string> PendingArgs = new();

        public bool AwaitingConfirmation => Pending != null;

        public static BrainResult Spoken(string text) => new() { Text = text };

        public static BrainResult NeedsConfirmation(AssistantTool tool, Dictionary<string, string> args) => new()
        {
            Pending = tool,
            PendingArgs = args,
            Text = tool.Preview?.Invoke(args) ?? (Localization.IsPolish ? "Potwierdzasz?" : "Confirm?"),
        };
    }

    // Routes one utterance to an answer. Offline matching first — it is instant, free and works
    // with no key — then the LLM if the user turned it on. Anything that writes to a store stops
    // here for a spoken yes/no regardless of which path proposed it.
    public sealed class AssistantBrain
    {
        private AssistantTool? _pending;
        private Dictionary<string, string> _pendingArgs = new();

        public bool AwaitingConfirmation => _pending != null;

        private static bool Pl => Localization.IsPolish;

        public void CancelPending()
        {
            _pending = null;
            _pendingArgs = new Dictionary<string, string>();
        }

        public async Task<string> AskAsync(string utterance, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(utterance))
                return Pl ? "Nie usłyszałem." : "I didn't catch that.";

            // ---- settle a queued write first ----
            if (_pending != null)
            {
                var tool = _pending;
                var args = _pendingArgs;

                if (IntentMatcher.IsYes(utterance))
                {
                    CancelPending();
                    return Safely(() => tool.Run(args));
                }
                if (IntentMatcher.IsNo(utterance))
                {
                    CancelPending();
                    return Pl ? "Anulowane." : "Cancelled.";
                }

                // Anything else is treated as a new command rather than a nagging re-ask: the
                // user has clearly moved on, and an unconfirmed write simply never happens.
                CancelPending();
            }

            // ---- offline intents: the written rules first ----
            // They are exact and they extract slots from the very phrasings they match, so when
            // one fires it is right by construction. The classifier only sees what they missed.
            var hit = IntentMatcher.Match(utterance);
            if (hit != null) return Dispatch(hit.Tool, hit.Args);

            // ---- offline intents: the trained classifier ----
            // Same catalog, same sanitising, same confirmation on writes — the only difference is
            // that this half generalises, so a phrasing nobody wrote a rule for still lands on
            // the right tool. It stays quiet unless it is confident, and a write it proposes is
            // still read back for a spoken yes.
            var guess = IntentNet.Predict(utterance);
            if (guess != null)
            {
                var args = IntentMatcher.ArgsFor(guess.Tool, utterance);
                if (HasEveryRequiredArg(guess.Tool, args))
                {
                    Logger.Log($"IntentNet: {guess.Tool.Name} at {guess.Confidence:P0}.");
                    return Dispatch(guess.Tool, args);
                }

                // The tool is probably right but a required free-text slot (a goal's wording, say)
                // could not be scraped out. Guessing it would store the wrong thing, so this goes
                // to the LLM, which can ask about it properly.
                Logger.Log($"IntentNet: {guess.Tool.Name} dropped, required argument missing.");
            }

            // ---- the model, only if the user opted in ----
            var llm = GroqBrain.FromSettings();
            if (llm != null)
            {
                try
                {
                    var result = await llm.AskAsync(utterance, ct);
                    if (result.AwaitingConfirmation)
                    {
                        _pending = result.Pending;
                        _pendingArgs = result.PendingArgs;
                    }
                    return result.Text;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // A dead network or a rejected key must not take the window down — the
                    // offline half still works, so say so and carry on.
                    Logger.Log($"Assistant LLM failed: {ex.Message}");
                    return Pl
                        ? "Nie mogę teraz dosięgnąć modelu. Komendy offline działają dalej."
                        : "I can't reach the model right now. Offline commands still work.";
                }
            }

            return Pl
                ? "Nie rozumiem. Spróbuj: jaki mam winrate, jaka moja ranga, ostatni mecz, otwórz sesję."
                : "I don't understand. Try: what's my win rate, what's my rank, last match, open session.";
        }

        // The single place a chosen tool turns into an answer, so the confirmation guarantee is
        // written once and holds for every path that can pick a tool.
        private string Dispatch(AssistantTool tool, Dictionary<string, string> args)
        {
            if (tool.Kind == ToolKind.Write)
            {
                _pending = tool;
                _pendingArgs = args;
                return tool.Preview?.Invoke(args) ?? (Pl ? "Potwierdzasz?" : "Confirm?");
            }
            return Safely(() => tool.Run(args));
        }

        // Parameters with an allowed set are always filled by Sanitize, so this only ever catches
        // the free-text ones — the goal wording that has to come out of the sentence itself.
        private static bool HasEveryRequiredArg(AssistantTool tool, Dictionary<string, string> args) =>
            tool.Params.All(p => !p.Required
                || (args.TryGetValue(p.Name, out var v) && !string.IsNullOrWhiteSpace(v)));

        // Tool handlers touch the disk, so one unreadable file shouldn't surface as a crash.
        private static string Safely(Func<string> run)
        {
            try
            {
                return run();
            }
            catch (Exception ex)
            {
                Logger.Log($"Assistant tool failed: {ex}");
                return Pl ? "Coś poszło nie tak przy tej komendzie." : "Something went wrong with that one.";
            }
        }
    }
}
