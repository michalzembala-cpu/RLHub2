using System;
using System.Collections.Generic;
using System.Linq;

namespace RLHub2.Assistant.Ml
{
    // The training data, written out by the app itself.
    //
    // There is no dataset to download and no labelling to do by hand: every example is generated
    // from phrasings of the twelve tools NexPlay already exposes, in both languages the app
    // speaks. The seed phrases are the part a human writes; everything after them is machine
    // augmentation whose only job is to stop the model memorising the exact wording.
    //
    // The thirteenth class, Unknown, is the one that makes the whole thing safe. Without it a
    // classifier has to answer with one of twelve tools no matter what it hears, so "what's the
    // weather" becomes a confident request for a win rate. Trained against a few dozen genuinely
    // out-of-scope sentences, the model learns a place to put anything it doesn't recognise, and
    // the brain forwards that to the LLM instead of guessing.
    public static class IntentCorpus
    {
        // Bumped whenever the phrases or the augmentation below change, so a cached model that
        // was trained on the old corpus is thrown away rather than silently reused.
        private const int CorpusVersion = 1;

        public const string Unknown = "unknown";

        // Index in this array is the class number the classifier works in. Order is fixed: a
        // reshuffle would silently remap a cached model's rows.
        public static readonly string[] Labels =
        {
            "get_record", "get_streak", "get_session", "get_rank", "get_last_match", "get_goals",
            "open_page", "toggle_overlay", "switch_game",
            "add_goal", "complete_goal", "reset_session",
            Unknown,
        };

        public static int LabelIndex(string label) => Array.IndexOf(Labels, label);

        // At most this many examples per class, so the combinatorial classes (open_page alone can
        // produce a hundred base phrasings) don't drown out the ones written by hand.
        private const int PerLabelCap = 420;

        public sealed class Phrase
        {
            public string Text = "";
            public int Label;
        }

        // ===================== seed phrasings =====================

        private static readonly string[] Record =
        {
            "jaki mam winrate", "ile mam wygranych", "jaki jest moj bilans", "jaka mam skutecznosc",
            "ile procent wygrywam", "ile wygralem dzisiaj", "ile dzis przegralem", "jak wyglada moj bilans",
            "procent wygranych", "moj winrate", "ile mam zwyciestw", "stosunek wygranych do przegranych",
            "ile gier wygralem w tym tygodniu", "jaki mam wynik lacznie", "bilans z dzisiaj",
            "ile meczow wygralem lacznie", "jaki winrate w tym tygodniu", "ile porazek mam dzisiaj",
            "podaj moj winrate", "jaka jest moja skutecznosc w tej sesji",
            "whats my winrate", "how many wins do i have", "whats my record", "win percentage",
            "how many did i lose today", "my win rate this week", "wins and losses",
            "whats my overall record", "how many games have i won", "give me my winrate",
        };

        private static readonly string[] Streak =
        {
            "jaka mam serie", "ile z rzedu", "seria wygranych", "ile pod rzad wygralem",
            "mam jakas serie", "ile meczow z rzedu przegralem", "jaka seria", "ile wygranych pod rzad",
            "czy mam serie wygranych", "ile razy z rzedu przegralem", "moja seria",
            "whats my streak", "how many in a row", "win streak", "am i on a streak",
            "losing streak", "how many games in a row have i won", "do i have a streak",
        };

        private static readonly string[] Session =
        {
            "jak mi idzie", "jak mi dzis idzie", "co z sesja", "podsumuj sesje", "jak wyglada sesja",
            "jak leci", "jak sesja", "co dzisiaj nagralem", "jak mi poszlo dzisiaj",
            "podsumowanie sesji", "jak stoi sesja", "jak mi szlo w tej sesji", "co mowi sesja",
            "jak dzis gram", "jak mi idzie dzisiaj",
            "how am i doing", "hows the session", "summarize my session", "hows it going today",
            "session summary", "how did i do today", "how is my session looking",
        };

        private static readonly string[] Rank =
        {
            "jaka mam range", "moja ranga", "jaki mam mmr", "ile mam mmr", "jaka dywizja",
            "jaki mam rank", "moje elo", "ile mam punktow rankingowych", "jaka ranga teraz",
            "jaki mam rating", "powiedz moja range", "w jakiej jestem randze", "ile mmr mam teraz",
            "jaki jest moj rank", "moj aktualny mmr", "jaka mam dywizje",
            "whats my rank", "my mmr", "what rank am i", "current rating", "what division am i in",
            "my elo", "how much mmr do i have", "tell me my rank",
        };

        private static readonly string[] LastMatch =
        {
            "jak poszedl ostatni mecz", "ostatni mecz", "co z ostatnia gra", "wynik ostatniego meczu",
            "jak skonczyl sie ostatni mecz", "poprzedni mecz", "ostatnia gra", "jak bylo w ostatnim meczu",
            "opowiedz o ostatnim meczu", "co w poprzedniej grze", "wynik poprzedniej gry",
            "jak mi poszedl ten ostatni mecz",
            "how was my last match", "last game", "previous match result", "what happened last match",
            "tell me about my last game", "how did the last one go",
        };

        private static readonly string[] Goals =
        {
            "jakie mam cele", "moje cele", "pokaz cele", "co mam do zrobienia", "lista celow",
            "ile mam celow", "jakie cele zostaly", "przypomnij moje cele", "co jest na liscie celow",
            "jakie cele mam na dzis", "wymien moje cele",
            "what are my goals", "show my goals", "my goal list", "what do i have left",
            "list my goals", "remind me of my goals",
        };

        private static readonly string[] PageOpeners =
        {
            "otworz", "pokaz", "przejdz do", "wejdz w", "wskocz na", "przelacz na zakladke",
            "open", "show me", "go to", "take me to",
        };

        private static readonly string[] PageNames =
        {
            "glowna", "strone glowna", "pulpit", "mmr", "wykres mmr", "droge", "road", "trenera",
            "coach", "rekordy", "aktualnosci", "nowosci", "profil", "turnieje", "sezony",
            "ustawienia", "celownik", "mapy", "trening", "insights", "sesje", "zakladke sesja",
            "home", "records", "news", "profile", "tournaments", "seasons", "settings",
            "crosshair", "maps", "practice", "session",
        };

        private static readonly string[] Overlay =
        {
            "wlacz overlay", "wylacz nakladke", "pokaz nakladke", "schowaj overlay", "overlay",
            "przelacz nakladke", "wlacz nakladke", "zdejmij overlay", "daj overlay", "ukryj nakladke",
            "nakladka", "wylacz overlay",
            "toggle overlay", "turn on the overlay", "hide overlay", "show overlay", "overlay off",
        };

        private static readonly string[] SwitchVerbs =
        {
            "przelacz na", "zmien gre na", "wroc do", "przejdz na", "ustaw gre na",
            "switch to", "change game to", "go back to",
        };

        private static readonly string[] GameNames =
        {
            "rocket league", "rl", "rocketa", "cs2", "counter strike", "cs",
        };

        private static readonly string[] GoalTexts =
        {
            "trening 30 minut", "wbic diamenta", "poprawic aim", "zagrac 10 meczow", "nie tiltowac",
            "obejrzec replay", "przecwiczyc air dribble", "wygrac piec meczow z rzedu",
            "grac spokojniej", "poprawic rotacje", "practice for an hour", "hit diamond",
            "win five games", "review one replay",
        };

        private static readonly string[] AddGoalVerbs =
        {
            "dodaj cel", "nowy cel", "zapisz cel", "add goal", "new goal",
        };

        private static readonly string[] CompleteGoalVerbs =
        {
            "odhacz", "ukonczylem", "zrobione", "zaliczone", "complete goal", "mark goal", "goal done",
        };

        private static readonly string[] ResetSession =
        {
            "nowa sesja", "wyczysc sesje", "zresetuj sesje", "zacznij nowa sesje", "wyzeruj sesje",
            "skasuj sesje", "zaczynam od nowa", "wyczysc statystyki sesji", "restart sesji",
            "new session", "clear the session", "reset session", "start a new session",
            "wipe the session",
        };

        // Deliberately close to the domain in places — a sentence can be full of game words and
        // still not be a command. That is the distinction this class has to teach.
        private static readonly string[] OutOfScope =
        {
            "jaka jest pogoda", "opowiedz zart", "kim jestes", "co slychac", "ile to dwa plus dwa",
            "jak ugotowac makaron", "kto wygral mistrzostwa swiata", "ktora godzina",
            "jaki jest najlepszy samochod w rocket league", "jak poprawic aim", "daj mi rade",
            "co robisz", "dzien dobry", "czesc", "dziekuje", "opowiedz o sobie",
            "jaka jest stolica polski", "wlacz muzyke", "przypomnij mi o czyms",
            "jak zrobic air dribble", "dlaczego przegrywam", "co robie zle", "jaki mam komputer",
            "lubie cs2", "rocket league jest fajne", "kto jest najlepszym graczem na swiecie",
            "jak dziala ta aplikacja", "czy warto grac w cs2", "ile kosztuje ta gra",
            "jak sie nazywasz", "powiedz cos mi", "nudze sie", "co mam robic", "pomoz mi",
            "jak trenowac skutecznie", "jakie ustawienia czulosci sa najlepsze",
            "opowiedz historie", "jestem zmeczony", "dobranoc", "do zobaczenia",
            "whats the weather", "tell me a joke", "who are you", "what time is it",
            "how do i air dribble", "play some music", "whats the capital of france", "hello",
            "thanks", "why do i keep losing", "give me advice", "whats the best car in rocket league",
            "how does this app work", "what should i practice", "im tired", "good night",
            "can you help me", "tell me something interesting",

            // Near misses, and the most valuable rows in the corpus. Each one is a word away from
            // a real command — "jak mi idzie" is a session summary, "dlaczego mi nie idzie" is a
            // question about causes that only the LLM can take. Without these the classifier
            // learns the topic and ignores the question word, and answers a request for advice
            // with a statistic.
            "dlaczego tak slabo mi idzie", "czemu mi dzisiaj nie idzie", "dlaczego gram tak zle",
            "czemu tak slabo gram ostatnio", "dlaczego mam taki slaby winrate",
            "czemu moja ranga spada", "dlaczego ciagle przegrywam ostatnie mecze",
            "co zrobic zeby grac lepiej", "jak sie poprawic", "jak wejsc wyzej w randze",
            "dlaczego moj mmr nie rosnie", "co powinienem trenowac zeby wygrywac",
            "czy moja seria porazek to przypadek", "dlaczego przegrywam ostatnie sesje",
            "why am i playing so badly today", "why is my winrate so low",
            "how do i get a better rank", "why do i keep losing my last games",
            "what should i work on to win more", "why is my mmr dropping",
        };

        // ===================== generation =====================

        private static readonly string[] Prefixes =
        {
            "", "", "", "", "hej ", "sluchaj ", "a ", "no ", "ok ", "prosze ", "powiedz ",
            "hey ", "ok so ", "please ",
        };

        private static readonly string[] Suffixes =
        {
            "", "", "", "", "", " prosze", " dzieki", " please", " teraz", " szybko",
        };

        private static string Filler(string s, Random rng) =>
            Prefixes[rng.Next(Prefixes.Length)] + s + Suffixes[rng.Next(Suffixes.Length)];

        // Drops one word. Speech recognition swallows short words constantly, and a command that
        // survives losing "mi" or "the" is a command the model recognised by its stem rather than
        // by matching the template it was generated from.
        private static string DropWord(string s, Random rng)
        {
            var w = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (w.Length < 4) return s;
            int i = rng.Next(w.Length);
            return string.Join(" ", w.Where((_, k) => k != i));
        }

        // One character-level slip: a swap, a drop or a double. This is what a misheard word looks
        // like, and it is the noise the character n-grams exist to absorb.
        private static string Typo(string s, Random rng)
        {
            if (s.Length < 5) return s;
            int i = rng.Next(s.Length - 1);
            if (s[i] == ' ' || s[i + 1] == ' ') return s;

            var c = s.ToCharArray();
            switch (rng.Next(3))
            {
                case 0:
                    (c[i], c[i + 1]) = (c[i + 1], c[i]);
                    return new string(c);
                case 1:
                    return s.Remove(i, 1);
                default:
                    return s.Insert(i, s[i].ToString());
            }
        }

        private static void Emit(List<Phrase> into, string text, int label, Random rng, int variants)
        {
            text = text.Trim();
            if (text.Length == 0) return;

            into.Add(new Phrase { Text = text, Label = label });
            for (int v = 0; v < variants; v++)
            {
                var s = Filler(text, rng);
                if (rng.Next(100) < 30) s = DropWord(s, rng);
                if (rng.Next(100) < 25) s = Typo(s, rng);
                into.Add(new Phrase { Text = s, Label = label });
            }
        }

        private static void EmitAll(List<Phrase> into, string[] seeds, string label, Random rng, int variants)
        {
            int idx = LabelIndex(label);
            foreach (var s in seeds) Emit(into, s, idx, rng, variants);
        }

        public static List<Phrase> Build(int seed)
        {
            var rng = new Random(seed);
            var all = new List<Phrase>();

            // Hand-written classes get more variants each, because they have fewer seeds.
            EmitAll(all, Record, "get_record", rng, 12);
            EmitAll(all, Streak, "get_streak", rng, 14);
            EmitAll(all, Session, "get_session", rng, 14);
            EmitAll(all, Rank, "get_rank", rng, 12);
            EmitAll(all, LastMatch, "get_last_match", rng, 14);
            EmitAll(all, Goals, "get_goals", rng, 14);
            EmitAll(all, Overlay, "toggle_overlay", rng, 14);
            EmitAll(all, ResetSession, "reset_session", rng, 14);
            EmitAll(all, OutOfScope, Unknown, rng, 8);

            // Combinatorial classes: the verb and the object vary independently in real speech,
            // so crossing them teaches the model that either half alone is not enough.
            int openPage = LabelIndex("open_page");
            foreach (var verb in PageOpeners)
                foreach (var page in PageNames)
                    Emit(all, verb + " " + page, openPage, rng, 1);

            int switchGame = LabelIndex("switch_game");
            foreach (var verb in SwitchVerbs)
                foreach (var game in GameNames)
                    Emit(all, verb + " " + game, switchGame, rng, 2);

            int addGoal = LabelIndex("add_goal");
            foreach (var verb in AddGoalVerbs)
                foreach (var text in GoalTexts)
                    Emit(all, verb + " " + text, addGoal, rng, 2);

            int completeGoal = LabelIndex("complete_goal");
            foreach (var verb in CompleteGoalVerbs)
                foreach (var text in GoalTexts)
                    Emit(all, verb + " " + text, completeGoal, rng, 2);

            return Balance(all, rng);
        }

        // Caps every class at the same ceiling. An unbalanced corpus teaches the model that the
        // biggest class is the safest guess, which is exactly the bias that would make it answer
        // when it should have said Unknown.
        private static List<Phrase> Balance(List<Phrase> all, Random rng)
        {
            var kept = new List<Phrase>(all.Count);
            foreach (var group in all.GroupBy(p => p.Label))
            {
                var items = group.OrderBy(_ => rng.Next()).ToList();
                kept.AddRange(items.Take(PerLabelCap));
            }
            return kept;
        }

        // Identifies this exact corpus definition. A cached model carrying a different stamp was
        // trained on different data and is discarded on load.
        public static long Stamp()
        {
            unchecked
            {
                ulong h = 14695981039346656037ul;
                void Mix(string s)
                {
                    foreach (var ch in s) h = (h ^ ch) * 1099511628211ul;
                    h = (h ^ '|') * 1099511628211ul;
                }

                Mix(CorpusVersion.ToString());
                Mix(PerLabelCap.ToString());
                foreach (var l in Labels) Mix(l);

                foreach (var set in new[]
                {
                    Record, Streak, Session, Rank, LastMatch, Goals, PageOpeners, PageNames,
                    Overlay, SwitchVerbs, GameNames, GoalTexts,
                    AddGoalVerbs, CompleteGoalVerbs, ResetSession, OutOfScope, Prefixes, Suffixes,
                })
                    foreach (var s in set) Mix(s);

                return (long)h;
            }
        }
    }
}
