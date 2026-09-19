using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using RLHub.Helpers;

namespace RLHub2.Assistant.Ml
{
    public sealed class IntentPrediction
    {
        public AssistantTool Tool = null!;

        // Softmax probability of the winning class. Carried through to the brain, which refuses
        // to act on anything it isn't sure about rather than running the wrong tool.
        public float Confidence;
    }

    // The trained half of the offline brain, and the piece that makes this NexPlay's own model
    // rather than a rented one: the corpus, the features, the optimiser and the weights all live
    // in this repository, and answering a command never leaves the machine.
    //
    // Training happens once, in the background, the first time the assistant is opened, and the
    // weights are cached next to the settings file. The cache is keyed by the corpus stamp, so
    // editing a phrase in IntentCorpus retrains on the next launch without anyone having to
    // remember to delete anything.
    public static class IntentNet
    {
        // A win needs to be both strong on its own and clearly ahead of the runner-up. The margin
        // is what keeps "otworz sesje" (a page) from being settled by a coin flip against
        // "jak sesja" (a summary) — when the two are close, the question goes to the LLM instead.
        private const float MinConfidence = 0.55f;
        private const float MinMargin = 0.15f;

        private static readonly object Gate = new();
        private static LinearClassifier? _model;
        private static bool _started;

        public static bool IsReady => _model != null;

        // Held-out accuracy of the model in use, for display in Settings. Zero until trained.
        public static float Accuracy { get; private set; }

        private static string CachePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RLHub2", "intent-model.bin");

        // Fire-and-forget, and safe to call repeatedly. Called when the assistant window opens so
        // that the first spoken command doesn't pay for the training.
        public static void WarmUp()
        {
            lock (Gate)
            {
                if (_started) return;
                _started = true;
            }

            Task.Run(() =>
            {
                try
                {
                    var result = IntentTrainer.TrainOrLoad(CachePath, Logger.Log);
                    lock (Gate)
                    {
                        _model = result.Model;
                        Accuracy = result.Accuracy;
                    }
                }
                catch (Exception ex)
                {
                    // The assistant works without this model. A failure here must cost the user
                    // nothing beyond the offline rules they already had.
                    Logger.Log($"IntentNet warmup failed: {ex.Message}");
                }
            });
        }

        // Returns null whenever the model is not ready, does not recognise the utterance, or is
        // not confident enough. In every one of those cases the caller should carry on to the
        // LLM rather than act on a guess.
        public static IntentPrediction? Predict(string utterance)
        {
            var model = _model;
            if (model == null || string.IsNullOrWhiteSpace(utterance)) return null;

            var indices = new List<int>();
            var values = new List<float>();
            TextFeatures.Extract(utterance, indices, values);
            if (indices.Count == 0) return null;

            var probs = new float[model.ClassCount];
            model.Predict(indices, values, probs);

            int best = 0, second = -1;
            for (int c = 1; c < probs.Length; c++) if (probs[c] > probs[best]) best = c;
            for (int c = 0; c < probs.Length; c++)
                if (c != best && (second < 0 || probs[c] > probs[second])) second = c;

            var label = IntentCorpus.Labels[best];
            if (label == IntentCorpus.Unknown) return null;

            float margin = second >= 0 ? probs[best] - probs[second] : probs[best];
            if (probs[best] < MinConfidence || margin < MinMargin) return null;

            var tool = AssistantTools.Find(label);
            if (tool == null) return null;

            return new IntentPrediction { Tool = tool, Confidence = probs[best] };
        }
    }
}
