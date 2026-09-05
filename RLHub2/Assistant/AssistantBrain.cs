using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RLHub.Helpers;
using RLHub2.Helpers;
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

            // ---- offline intents ----
            var hit = IntentMatcher.Match(utterance);
            if (hit != null)
            {
                if (hit.Tool.Kind == ToolKind.Write)
                {
                    _pending = hit.Tool;
                    _pendingArgs = hit.Args;
                    return hit.Tool.Preview?.Invoke(hit.Args) ?? (Pl ? "Potwierdzasz?" : "Confirm?");
                }
                return Safely(() => hit.Tool.Run(hit.Args));
            }

            // ---- the model, only if the user opted in ----
            var llm = ClaudeBrain.FromSettings();
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
