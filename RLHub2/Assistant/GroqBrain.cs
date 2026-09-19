using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using RLHub2.Helpers;
using RLHub2.Services;

namespace RLHub2.Assistant
{
    // The optional half of the brain. It only ever runs when the offline matcher found nothing
    // and the user has pasted their own API key in Settings, so the app stays fully usable —
    // and fully local — for anyone who never turns this on.
    //
    // Talks to Groq's OpenAI-compatible endpoint over plain HTTP (no SDK): fast, has a free tier
    // and no age gate. The model is given the same tool catalog the offline path uses and nothing
    // else: it cannot reach a store except through a tool, and a tool marked Write is never
    // executed here — when the model asks for one, the loop stops and hands the pending call back
    // for the spoken yes/no, so the confirmation guarantee holds no matter which brain answered.
    public sealed class GroqBrain
    {
        // Groq's OpenAI-compatible Chat Completions endpoint.
        private const string Endpoint = "https://api.groq.com/openai/v1/chat/completions";

        // A fast, tool-capable model on Groq's free tier. Settings lets you pick another
        // (e.g. a smaller one for even lower latency on this voice path).
        public const string DefaultModel = "llama-3.3-70b-versatile";

        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

        private readonly string _apiKey;
        private readonly string _model;

        public GroqBrain(string apiKey, string model)
        {
            _apiKey = apiKey;
            _model = string.IsNullOrWhiteSpace(model) ? DefaultModel : model;
        }

        public static GroqBrain? FromSettings()
        {
            var cfg = new SettingsStore().Load();
            if (!cfg.AssistantUseLlm || string.IsNullOrWhiteSpace(cfg.AssistantApiKey)) return null;
            return new GroqBrain(cfg.AssistantApiKey.Trim(), cfg.AssistantModel);
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

        // Tools in OpenAI/Groq function-calling shape.
        private static JsonArray BuildTools()
        {
            var tools = new JsonArray();
            foreach (var t in AssistantTools.All)
            {
                var props = new JsonObject();
                var required = new JsonArray();
                foreach (var p in t.Params)
                {
                    var schema = new JsonObject { ["type"] = "string", ["description"] = p.Description };
                    // Constrained parameters go over as a JSON enum, so the model can't invent a
                    // value; the result is validated again on the way back regardless.
                    var allowed = p.Allowed.Where(v => v.Length > 0).ToArray();
                    if (allowed.Length > 0)
                    {
                        var en = new JsonArray();
                        foreach (var v in allowed) en.Add(v);
                        schema["enum"] = en;
                    }
                    props[p.Name] = schema;
                    if (p.Required) required.Add(p.Name);
                }

                tools.Add(new JsonObject
                {
                    ["type"] = "function",
                    ["function"] = new JsonObject
                    {
                        ["name"] = t.Name,
                        ["description"] = t.Description,
                        ["parameters"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["properties"] = props,
                            ["required"] = required,
                        },
                    },
                });
            }
            return tools;
        }

        // The model's arguments arrive as a JSON string. Flattening to strings here keeps the
        // tool handlers identical for both brains.
        private static Dictionary<string, string> ArgsFrom(string? argsJson)
        {
            var args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(argsJson)) return args;
            try
            {
                using var doc = JsonDocument.Parse(argsJson);
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
            var tools = BuildTools();
            var messages = new JsonArray
            {
                new JsonObject { ["role"] = "system", ["content"] = SystemPrompt() },
                new JsonObject { ["role"] = "user", ["content"] = utterance },
            };

            // Each round is one model turn plus the tool results it asked for. Four is well past
            // what any of these questions need and stops a loop from running up a bill.
            for (int round = 0; round < 4; round++)
            {
                ct.ThrowIfCancellationRequested();

                var body = new JsonObject
                {
                    ["model"] = _model,
                    ["max_tokens"] = 1024,
                    // A command router is a deterministic task on a latency-sensitive voice path;
                    // no creativity wanted.
                    ["temperature"] = 0,
                    ["messages"] = messages.DeepClone(),
                    ["tools"] = tools.DeepClone(),
                    ["tool_choice"] = "auto",
                };

                using var req = new HttpRequestMessage(HttpMethod.Post, Endpoint);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                req.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");

                using var resp = await Http.SendAsync(req, ct);
                var respBody = await resp.Content.ReadAsStringAsync(ct);
                if (!resp.IsSuccessStatusCode)
                    // Bubble up: the caller catches this and falls back to the offline half.
                    throw new HttpRequestException($"Groq {(int)resp.StatusCode}: {respBody}");

                using var doc = JsonDocument.Parse(respBody);
                var msg = doc.RootElement.GetProperty("choices")[0].GetProperty("message");

                string? content = msg.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.String
                    ? c.GetString()
                    : null;

                bool hasToolCalls = msg.TryGetProperty("tool_calls", out var toolCalls)
                    && toolCalls.ValueKind == JsonValueKind.Array
                    && toolCalls.GetArrayLength() > 0;

                if (!hasToolCalls)
                {
                    var answer = (content ?? "").Trim();
                    return BrainResult.Spoken(answer.Length > 0
                        ? answer
                        : Localization.IsPolish ? "Nie mam na to odpowiedzi." : "I don't have an answer for that.");
                }

                // Re-emit the assistant turn (only the fields Groq needs back) plus a matching
                // tool result for every call it made, then loop.
                var callsOut = new JsonArray();
                var results = new List<(string Id, string Content)>();

                foreach (var call in toolCalls.EnumerateArray())
                {
                    var id = call.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";
                    var fn = call.GetProperty("function");
                    var name = fn.TryGetProperty("name", out var nEl) ? nEl.GetString() ?? "" : "";
                    var argsJson = fn.TryGetProperty("arguments", out var aEl)
                        ? (aEl.ValueKind == JsonValueKind.String ? aEl.GetString() : aEl.GetRawText())
                        : "{}";

                    callsOut.Add(new JsonObject
                    {
                        ["id"] = id,
                        ["type"] = "function",
                        ["function"] = new JsonObject
                        {
                            ["name"] = name,
                            ["arguments"] = argsJson ?? "{}",
                        },
                    });

                    var tool = AssistantTools.Find(name);
                    if (tool == null)
                    {
                        results.Add((id, "No such tool."));
                        continue;
                    }

                    var args = AssistantTools.Sanitize(tool, ArgsFrom(argsJson));

                    // The one thing the model is never allowed to do on its own.
                    if (tool.Kind == ToolKind.Write)
                        return BrainResult.NeedsConfirmation(tool, args);

                    results.Add((id, tool.Run(args)));
                }

                messages.Add(new JsonObject
                {
                    ["role"] = "assistant",
                    ["content"] = content,
                    ["tool_calls"] = callsOut,
                });
                foreach (var (id, res) in results)
                {
                    messages.Add(new JsonObject
                    {
                        ["role"] = "tool",
                        ["tool_call_id"] = id,
                        ["content"] = res,
                    });
                }
            }

            return BrainResult.Spoken(Localization.IsPolish
                ? "Zgubiłem się przy tym pytaniu."
                : "I lost the thread on that one.");
        }
    }
}
