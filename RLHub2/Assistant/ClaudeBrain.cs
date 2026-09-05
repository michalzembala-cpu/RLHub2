using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Anthropic;
using Anthropic.Models.Messages;
using RLHub2.Helpers;
using RLHub2.Services;

namespace RLHub2.Assistant
{
    // The optional half of the brain. It only ever runs when the offline matcher found nothing
    // and the user has pasted their own API key in Settings, so the app stays fully usable —
    // and fully local — for anyone who never turns this on.
    //
    // The model is given the same tool catalog the offline path uses and nothing else: it cannot
    // reach a store except through a tool, and a tool marked Write is never executed here. When
    // the model asks for one, the loop stops and hands the pending call back for the spoken
    // yes/no, so the confirmation guarantee holds no matter which brain answered.
    public sealed class ClaudeBrain
    {
        // Anthropic's most capable model is the default. It is also the slowest, and this is a
        // voice path where latency is felt directly — Settings lets you drop to a smaller model.
        public const string DefaultModel = "claude-opus-5";

        private readonly string _apiKey;
        private readonly string _model;

        public ClaudeBrain(string apiKey, string model)
        {
            _apiKey = apiKey;
            _model = string.IsNullOrWhiteSpace(model) ? DefaultModel : model;
        }

        public static ClaudeBrain? FromSettings()
        {
            var cfg = new SettingsStore().Load();
            if (!cfg.AssistantUseLlm || string.IsNullOrWhiteSpace(cfg.AssistantApiKey)) return null;
            return new ClaudeBrain(cfg.AssistantApiKey.Trim(), cfg.AssistantModel);
        }

        private static string SystemPrompt()
        {
            bool pl = Localization.IsPolish;
            var game = Games.Name(Games.Active);
            var who = Accounts.ActiveName;
            var whoLine = who.Length > 0
                ? (pl ? $" Aktywny profil: {who}." : $" Active profile: {who}.")
                : "";

            // Spoken aloud, so length is the whole game: two sentences read back fine, a
            // paragraph does not. The model is also told not to invent numbers, because every
            // real figure is available through a tool and a plausible-sounding guess about the
            // user's own rank is worse than saying nothing.
            return pl
                ? "Jesteś głosowym asystentem w aplikacji NexPlay, która śledzi statystyki gracza. "
                  + $"Otwarta gra: {game}.{whoLine} "
                  + "Odpowiadaj po polsku, maksymalnie dwoma krótkimi zdaniami — Twoja odpowiedź jest czytana na głos. "
                  + "Wszystkie liczby bierz wyłącznie z narzędzi. Nigdy nie zgaduj statystyk, rangi ani wyniku. "
                  + "Jeśli nie da się czegoś sprawdzić narzędziem, powiedz wprost, że tego nie wiesz."
                : "You are a voice assistant inside NexPlay, an app that tracks a player's stats. "
                  + $"Current game: {game}.{whoLine} "
                  + "Answer in English, in at most two short sentences — your reply is read aloud. "
                  + "Take every number from a tool. Never guess a stat, a rank or a result. "
                  + "If a tool can't answer it, say plainly that you don't know.";
        }

        private static List<ToolUnion> BuildTools()
        {
            var list = new List<ToolUnion>();
            foreach (var t in AssistantTools.All)
            {
                var props = new Dictionary<string, JsonElement>();
                var required = new List<string>();
                foreach (var p in t.Params)
                {
                    // Constrained parameters go over as a JSON enum, so the model can't invent a
                    // value; the result is validated again on the way back regardless.
                    var allowed = p.Allowed.Where(v => v.Length > 0).ToArray();
                    props[p.Name] = allowed.Length > 0
                        ? JsonSerializer.SerializeToElement(
                            new { type = "string", description = p.Description, @enum = allowed })
                        : JsonSerializer.SerializeToElement(
                            new { type = "string", description = p.Description });
                    if (p.Required) required.Add(p.Name);
                }

                list.Add(new Tool
                {
                    Name = t.Name,
                    Description = t.Description,
                    InputSchema = new() { Properties = props, Required = required },
                });
            }
            return list;
        }

        // The model's arguments arrive as loosely typed JSON. Flattening to strings here keeps
        // the tool handlers identical for both brains.
        private static Dictionary<string, string> ArgsFrom(object? input)
        {
            var args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (input == null) return args;
            try
            {
                using var doc = JsonDocument.Parse(JsonSerializer.Serialize(input));
                if (doc.RootElement.ValueKind != JsonValueKind.Object) return args;
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    args[prop.Name] = prop.Value.ValueKind switch
                    {
                        JsonValueKind.String => prop.Value.GetString() ?? "",
                        JsonValueKind.Null => "",
                        _ => prop.Value.GetRawText(),
                    };
                }
            }
            catch (JsonException)
            {
                // A malformed tool call is the model's problem, not a crash: the caller sees no
                // arguments, the tool falls back to its defaults or reports it can't answer.
            }
            return args;
        }

        public async Task<BrainResult> AskAsync(string utterance, CancellationToken ct)
        {
            var client = new AnthropicClient { ApiKey = _apiKey };
            var tools = BuildTools();

            List<MessageParam> messages =
            [
                new() { Role = Role.User, Content = utterance },
            ];

            // Each round is one model turn plus the tool results it asked for. Four is well past
            // what any of these questions need and stops a loop from running up a bill.
            for (int round = 0; round < 4; round++)
            {
                ct.ThrowIfCancellationRequested();

                var response = await client.Messages.Create(new MessageCreateParams
                {
                    Model = _model,
                    MaxTokens = 1024,
                    System = SystemPrompt(),
                    // A command router is a simple task, and this is a latency-sensitive voice
                    // path — low effort is the lever for both.
                    OutputConfig = new OutputConfig { Effort = Effort.Low },
                    Tools = tools,
                    Messages = messages,
                });

                List<ContentBlockParam> assistantContent = [];
                List<ContentBlockParam> toolResults = [];
                var spoken = new List<string>();

                foreach (var block in response.Content)
                {
                    if (block.TryPickText(out TextBlock? text))
                    {
                        assistantContent.Add(new TextBlockParam { Text = text.Text });
                        if (!string.IsNullOrWhiteSpace(text.Text)) spoken.Add(text.Text.Trim());
                    }
                    else if (block.TryPickThinking(out ThinkingBlock? thinking))
                    {
                        assistantContent.Add(new ThinkingBlockParam
                        {
                            Thinking = thinking.Thinking,
                            Signature = thinking.Signature,
                        });
                    }
                    else if (block.TryPickRedactedThinking(out RedactedThinkingBlock? redacted))
                    {
                        assistantContent.Add(new RedactedThinkingBlockParam { Data = redacted.Data });
                    }
                    else if (block.TryPickToolUse(out ToolUseBlock? call))
                    {
                        assistantContent.Add(new ToolUseBlockParam
                        {
                            ID = call.ID,
                            Name = call.Name,
                            Input = call.Input,
                        });

                        var tool = AssistantTools.Find(call.Name);
                        if (tool == null)
                        {
                            toolResults.Add(new ToolResultBlockParam
                            {
                                ToolUseID = call.ID,
                                Content = "No such tool.",
                            });
                            continue;
                        }

                        var args = AssistantTools.Sanitize(tool, ArgsFrom(call.Input));

                        // The one thing the model is never allowed to do on its own.
                        if (tool.Kind == ToolKind.Write)
                            return BrainResult.NeedsConfirmation(tool, args);

                        toolResults.Add(new ToolResultBlockParam
                        {
                            ToolUseID = call.ID,
                            Content = tool.Run(args),
                        });
                    }
                }

                if (toolResults.Count == 0)
                {
                    var answer = string.Join(" ", spoken).Trim();
                    return BrainResult.Spoken(answer.Length > 0
                        ? answer
                        : Localization.IsPolish ? "Nie mam na to odpowiedzi." : "I don't have an answer for that.");
                }

                messages =
                [
                    .. messages,
                    new() { Role = Role.Assistant, Content = assistantContent },
                    new() { Role = Role.User, Content = toolResults },
                ];
            }

            return BrainResult.Spoken(Localization.IsPolish
                ? "Zgubiłem się przy tym pytaniu."
                : "I lost the thread on that one.");
        }
    }
}
