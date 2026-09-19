using System;
using System.Collections.Generic;

namespace RLHub2.Assistant.Ml
{
    // Turns an utterance into the sparse vector the classifier actually sees.
    //
    // Two families of features, both hashed into one flat space:
    //   * whole words and word pairs — carry the literal command ("nowa sesja", "last match");
    //   * character n-grams inside each word — carry the stem. This is the half that matters in
    //     Polish: "ranga", "rangi", "range" and "rangę" share "rang", so a phrasing the corpus
    //     never listed still lands near the ones it did. It also absorbs a mistyped or misheard
    //     letter, since only the n-grams touching that letter change.
    //
    // Hashing means no vocabulary file to ship or keep in sync: a feature is just a number, and
    // an unseen word hashes to some bucket whose weight is near zero — harmless. The hash is
    // FNV-1a rather than string.GetHashCode, which is salted per process on .NET Core and would
    // give a different model on every run.
    public static class TextFeatures
    {
        // 65 536 buckets against a few thousand distinct features: collisions are rare enough not
        // to matter, and the whole weight matrix stays around 3 MB.
        public const int Buckets = 1 << 16;

        private const int MinGram = 3;
        private const int MaxGram = 5;

        // Lowercases, strips Polish diacritics and collapses everything that isn't a letter or a
        // digit to a single space. Speech recognition is inconsistent about diacritics and a user
        // typing into the box rarely bothers with them, so "rangę?" and "range" have to arrive
        // here as the same string. Both halves of the offline brain normalise through this.
        public static string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";

            var folded = new System.Text.StringBuilder(s.Length);
            foreach (var ch in s.ToLowerInvariant())
            {
                folded.Append(ch switch
                {
                    'ą' => 'a', 'ć' => 'c', 'ę' => 'e', 'ł' => 'l', 'ń' => 'n',
                    'ó' => 'o', 'ś' => 's', 'ż' => 'z', 'ź' => 'z',
                    _ => ch,
                });
            }

            var cleaned = new System.Text.StringBuilder(folded.Length);
            foreach (var ch in folded.ToString())
                cleaned.Append(char.IsLetterOrDigit(ch) ? ch : ' ');

            return string.Join(" ", cleaned.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        private static uint Hash(string s, char tag)
        {
            // FNV-1a, with the family tag folded in first so a word feature and a char-gram
            // feature spelling the same text can't collide.
            unchecked
            {
                uint h = 2166136261u;
                h = (h ^ tag) * 16777619u;
                foreach (var ch in s) h = (h ^ ch) * 16777619u;
                return h;
            }
        }

        private static uint Hash(string s, int start, int len, char tag)
        {
            unchecked
            {
                uint h = 2166136261u;
                h = (h ^ tag) * 16777619u;
                for (int i = 0; i < len; i++) h = (h ^ s[start + i]) * 16777619u;
                return h;
            }
        }

        // Fills `indices` with distinct bucket numbers and `values` with their weights.
        // The vector is L2-normalised, so a long rambling sentence and a two-word command push
        // on the weights with the same total force — otherwise every long utterance would look
        // more confident than it is.
        public static void Extract(string utterance, List<int> indices, List<float> values)
        {
            indices.Clear();
            values.Clear();

            var norm = Normalize(utterance);
            if (norm.Length == 0) return;

            var seen = new HashSet<int>();
            void Add(uint h)
            {
                int idx = (int)(h & (Buckets - 1));
                if (seen.Add(idx)) indices.Add(idx);
            }

            var words = norm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                var w = words[i];
                Add(Hash(w, 'w'));
                if (i + 1 < words.Length) Add(Hash(w + "_" + words[i + 1], 'b'));

                // Boundary markers let an n-gram tell a prefix from the same letters mid-word,
                // which is how "wy-" (wygrana) stays distinct from "-wy" (meczowy).
                var padded = "^" + w + "$";
                for (int n = MinGram; n <= MaxGram; n++)
                    for (int s = 0; s + n <= padded.Length; s++)
                        Add(Hash(padded, s, n, 'c'));
            }

            var scale = (float)(1.0 / Math.Sqrt(indices.Count));
            for (int i = 0; i < indices.Count; i++) values.Add(scale);
        }
    }
}
