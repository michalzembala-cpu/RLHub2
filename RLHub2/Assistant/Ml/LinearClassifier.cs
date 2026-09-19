using System;
using System.Collections.Generic;
using System.IO;

namespace RLHub2.Assistant.Ml
{
    // A multinomial logistic regression, trained from scratch on the corpus this app generates.
    // Nothing here is inherited from a pretrained model: the weights start at zero and every
    // number in them comes from NexPlay's own tool catalog.
    //
    // A linear model is the right size for this job. There are fourteen outcomes and the signal
    // is mostly "which stems are present", which is exactly what a linear model over n-grams
    // captures; anything deeper would need far more data than can honestly be generated from a
    // catalog of thirteen tools, and would learn the templates instead of the language.
    //
    // The optimiser is AdaGrad: it gives a rare feature (a stem seen in four examples) a large
    // step and a common one (the word "mi") a small one, which is what a bag of n-grams needs
    // and what plain SGD with a single global rate does badly.
    public sealed class LinearClassifier
    {
        private const int Magic = 0x4E584D31; // NXM1

        public int ClassCount { get; private set; }

        private float[] _w = Array.Empty<float>(); // ClassCount * TextFeatures.Buckets, one row per class
        private float[] _b = Array.Empty<float>();

        public LinearClassifier(int classCount)
        {
            ClassCount = classCount;
            _w = new float[classCount * TextFeatures.Buckets];
            _b = new float[classCount];
        }

        private LinearClassifier() { }

        public sealed class Sample
        {
            public int[] Indices = Array.Empty<int>();
            public float[] Values = Array.Empty<float>();
            public int Label;
        }

        // Scores every class and squashes the scores to probabilities. `probs` belongs to the
        // caller, so the hot path — one call per spoken sentence — allocates nothing.
        public void Predict(IReadOnlyList<int> indices, IReadOnlyList<float> values, float[] probs)
        {
            for (int c = 0; c < ClassCount; c++)
            {
                int row = c * TextFeatures.Buckets;
                float s = _b[c];
                for (int i = 0; i < indices.Count; i++) s += _w[row + indices[i]] * values[i];
                probs[c] = s;
            }
            Softmax(probs, ClassCount);
        }

        private static void Softmax(float[] scores, int n)
        {
            float max = float.NegativeInfinity;
            for (int i = 0; i < n; i++) if (scores[i] > max) max = scores[i];

            float sum = 0f;
            for (int i = 0; i < n; i++)
            {
                scores[i] = MathF.Exp(scores[i] - max);
                sum += scores[i];
            }
            if (sum <= 0f) sum = 1f;
            for (int i = 0; i < n; i++) scores[i] /= sum;
        }

        // One pass per epoch over a shuffled corpus. The shuffle is seeded, so two machines on
        // the same app version train byte-identical weights.
        public void Train(IReadOnlyList<Sample> samples, int epochs, float learningRate, float l2, int seed)
        {
            var grad = new float[_w.Length];   // AdaGrad's accumulated squared gradient
            var gradB = new float[ClassCount];
            var probs = new float[ClassCount];

            var order = new int[samples.Count];
            for (int i = 0; i < order.Length; i++) order[i] = i;

            var rng = new Random(seed);
            for (int epoch = 0; epoch < epochs; epoch++)
            {
                for (int i = order.Length - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (order[i], order[j]) = (order[j], order[i]);
                }

                foreach (var idx in order)
                {
                    var s = samples[idx];
                    Predict(s.Indices, s.Values, probs);

                    for (int c = 0; c < ClassCount; c++)
                    {
                        // Gradient of cross-entropy with respect to this class's score.
                        float g = probs[c] - (c == s.Label ? 1f : 0f);
                        if (g > -1e-6f && g < 1e-6f) continue;

                        int row = c * TextFeatures.Buckets;
                        for (int k = 0; k < s.Indices.Length; k++)
                        {
                            int p = row + s.Indices[k];
                            // L2 touches only the features this example uses. Decaying all three
                            // million weights on every step would cost more than the training.
                            float gi = g * s.Values[k] + l2 * _w[p];
                            grad[p] += gi * gi;
                            _w[p] -= learningRate * gi / (MathF.Sqrt(grad[p]) + 1e-8f);
                        }

                        gradB[c] += g * g;
                        _b[c] -= learningRate * g / (MathF.Sqrt(gradB[c]) + 1e-8f);
                    }
                }
            }
        }

        // Accuracy over held-out samples. It decides whether the freshly trained model is
        // trustworthy enough to be allowed to answer at all.
        public float Accuracy(IReadOnlyList<Sample> samples)
        {
            if (samples.Count == 0) return 0f;
            var probs = new float[ClassCount];
            int ok = 0;
            foreach (var s in samples)
            {
                Predict(s.Indices, s.Values, probs);
                int best = 0;
                for (int c = 1; c < ClassCount; c++) if (probs[c] > probs[best]) best = c;
                if (best == s.Label) ok++;
            }
            return (float)ok / samples.Count;
        }

        // Most weights stay at exactly zero — only buckets that some training word touched ever
        // move — so the file stores just the non-zero ones and lands in the hundreds of KB
        // instead of the three MB the full matrix would take.
        public void Save(Stream stream, long corpusStamp)
        {
            using var w = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);
            w.Write(Magic);
            w.Write(TextFeatures.Buckets);
            w.Write(ClassCount);
            w.Write(corpusStamp);

            int nonZero = 0;
            foreach (var v in _w) if (v != 0f) nonZero++;

            w.Write(nonZero);
            for (int i = 0; i < _w.Length; i++)
            {
                if (_w[i] == 0f) continue;
                w.Write(i);
                w.Write(_w[i]);
            }
            foreach (var v in _b) w.Write(v);
        }

        // Returns null whenever the file came from another build, another feature layout or is
        // simply corrupt. The caller then retrains, which costs about a second.
        public static LinearClassifier? Load(Stream stream, int expectedClasses, long expectedStamp)
        {
            try
            {
                using var r = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
                if (r.ReadInt32() != Magic) return null;
                if (r.ReadInt32() != TextFeatures.Buckets) return null;

                int classes = r.ReadInt32();
                if (classes != expectedClasses) return null;
                if (r.ReadInt64() != expectedStamp) return null;

                var m = new LinearClassifier
                {
                    ClassCount = classes,
                    _w = new float[classes * TextFeatures.Buckets],
                    _b = new float[classes],
                };

                int nonZero = r.ReadInt32();
                if (nonZero < 0 || nonZero > m._w.Length) return null;
                for (int k = 0; k < nonZero; k++)
                {
                    int i = r.ReadInt32();
                    float v = r.ReadSingle();
                    if (i < 0 || i >= m._w.Length) return null;
                    m._w[i] = v;
                }
                for (int c = 0; c < classes; c++) m._b[c] = r.ReadSingle();
                return m;
            }
            catch (EndOfStreamException)
            {
                return null;
            }
        }
    }
}
