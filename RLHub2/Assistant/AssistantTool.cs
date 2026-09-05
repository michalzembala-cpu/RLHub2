using System;
using System.Collections.Generic;

namespace RLHub2.Assistant
{
    public enum ToolKind
    {
        Read,   // answers a question, changes nothing
        Action, // moves the UI around; undone by one click
        Write,  // puts something in a store — always confirmed out loud first
    }

    public sealed class ToolParam
    {
        public string Name = "";
        public string Description = "";

        // When non-empty the value must be one of these. The LLM gets it as a JSON enum and the
        // offline matcher uses it to validate what it scraped out of the phrase, so a misheard
        // word can never reach a store as a bogus value.
        public string[] Allowed = Array.Empty<string>();

        public bool Required;
        public string Default = "";
    }

    public sealed class AssistantTool
    {
        public string Name = "";
        public string Description = "";
        public ToolKind Kind = ToolKind.Read;
        public List<ToolParam> Params = new();

        // Produces the sentence spoken back to the user.
        public Func<IReadOnlyDictionary<string, string>, string> Run = _ => "";

        // Writes only: the question asked before anything is stored, e.g.
        // "Add a win as support to the session — yes or no?". A mishearing costs one "no".
        public Func<IReadOnlyDictionary<string, string>, string>? Preview;

        public ToolParam? Param(string name) =>
            Params.Find(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}
