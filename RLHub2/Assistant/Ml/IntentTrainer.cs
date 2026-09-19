using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RLHub2.Assistant.Ml
{
    // Corpus in, weights out. Deliberately free of any reference to the rest of NexPlay — no
    // stores, no settings, no logger — so the exact pipeline that runs inside the app can also be
    // run from a bare console harness and measured before it ships. If this needed a running app
    // to exercise, its accuracy would be a claim rather than a number.
    public static class IntentTrainer
    {
        // Fixed, so the model a user gets is the model that was measured, not one of many.
        public const int Seed = 20240917;
        public const int Epochs = 18;
        public const float LearningRate = 0.35f;
        public const float L2 = 1e-6f;

        // Share of the corpus held out to measure accuracy. It is never trained on.
        public const double HoldOut = 0.15;

        // A model that cannot reach this on held-out data is not wired up at all. Falling back to
        // the rules and the LLM beats letting a bad classifier answer confidently.
        public const float MinAccuracy = 0.85f;

        public sealed class Result
        {
            // Null when training was attempted and the model missed the accuracy bar.
            public LinearClassifier? Model;
            public float Accuracy;
            public int TrainCount;
            public int TestCount;
            public bool FromCache;
        }

        public static List<LinearClassifier.Sample> Featurize(IReadOnlyList<IntentCorpus.Phrase> phrases)
        {
            var indices = new List<int>();
            var values = new List<float>();
            var samples = new List<LinearClassifier.Sample>(phrases.Count);

            foreach (var p in phrases)
            {
                TextFeatures.Extract(p.Text, indices, values);
                if (indices.Count == 0) continue;
                samples.Add(new LinearClassifier.Sample
                {
                    Indices = indices.ToArray(),
                    Values = values.ToArray(),
                    Label = p.Label,
                });
            }
            return samples;
        }

        // Trains from scratch and reports how well the result does on data it never saw.
        public static Result Train(Action<string>? log = null)
        {
            var started = DateTime.UtcNow;
            var samples = Featurize(IntentCorpus.Build(Seed));

            // Deterministic split: same corpus, same split, same reported accuracy everywhere.
            var rng = new Random(Seed);
            var shuffled = samples.OrderBy(_ => rng.Next()).ToList();
            int testCount = Math.Max(1, (int)(shuffled.Count * HoldOut));
            var test = shuffled.Take(testCount).ToList();
            var train = shuffled.Skip(testCount).ToList();

            var model = new LinearClassifier(IntentCorpus.Labels.Length);
            model.Train(train, Epochs, LearningRate, L2, Seed);

            var acc = model.Accuracy(test);
            log?.Invoke($"IntentNet: trained on {train.Count} samples in " +
                        $"{(DateTime.UtcNow - started).TotalSeconds:F1}s, held-out accuracy {acc:P1}.");

            if (acc < MinAccuracy)
            {
                log?.Invoke("IntentNet: accuracy below threshold, model not used.");
                return new Result { Accuracy = acc, TrainCount = train.Count, TestCount = test.Count };
            }

            return new Result
            {
                Model = model,
                Accuracy = acc,
                TrainCount = train.Count,
                TestCount = test.Count,
            };
        }

        // Reuses the cached weights when they were trained on this exact corpus, otherwise
        // retrains and writes a fresh cache. Losing the cache costs one second on next launch, so
        // every failure here is downgraded to a log line.
        public static Result TrainOrLoad(string cachePath, Action<string>? log = null)
        {
            var stamp = IntentCorpus.Stamp();

            if (File.Exists(cachePath))
            {
                try
                {
                    using var fs = File.OpenRead(cachePath);
                    var cached = LinearClassifier.Load(fs, IntentCorpus.Labels.Length, stamp);
                    if (cached != null)
                    {
                        log?.Invoke("IntentNet: loaded cached model.");
                        // The cache is only ever written after the accuracy gate below, so a file
                        // that matches this stamp is known to have passed it.
                        return new Result { Model = cached, Accuracy = MinAccuracy, FromCache = true };
                    }
                }
                catch (IOException ex)
                {
                    log?.Invoke($"IntentNet: cache unreadable ({ex.Message}), retraining.");
                }
            }

            var result = Train(log);
            if (result.Model == null) return result;

            try
            {
                var dir = Path.GetDirectoryName(cachePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                using var fs = File.Create(cachePath);
                result.Model.Save(fs, stamp);
            }
            catch (IOException ex)
            {
                log?.Invoke($"IntentNet: could not cache model ({ex.Message}).");
            }

            return result;
        }
    }
}
