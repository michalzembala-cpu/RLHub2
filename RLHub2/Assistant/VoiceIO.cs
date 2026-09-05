using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RLHub.Helpers;
using RLHub2.Helpers;
using Windows.Media.SpeechRecognition;

namespace RLHub2.Assistant
{
    public sealed class VoiceUnavailableException : Exception
    {
        public VoiceUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
    }

    // Microphone in, speaker out.
    //
    // Recognition uses the WinRT recognizer rather than the older desktop one because that is the
    // half that speaks Polish — the legacy SAPI recognizer never shipped a pl-PL model, which
    // would have made the whole feature useless for the language the app is actually used in.
    // Speech synthesis goes the other way and uses System.Speech: it plays through the default
    // device with two lines and no stream plumbing, and the Polish SAPI voice is there whenever
    // the Windows language pack is.
    public sealed class VoiceIO : IDisposable
    {
        private readonly System.Speech.Synthesis.SpeechSynthesizer _tts = new();
        private SpeechRecognizer? _recognizer;
        private bool _disposed;

        public VoiceIO()
        {
            _tts.SetOutputToDefaultAudioDevice();
            PickVoice();
        }

        // Prefer a voice in the app's language; Windows often has several, and the default is
        // whatever the system locale is rather than what the user set in Settings.
        private void PickVoice()
        {
            try
            {
                var want = Localization.IsPolish ? "pl" : "en";
                var voice = _tts.GetInstalledVoices()
                    .Where(v => v.Enabled)
                    .Select(v => v.VoiceInfo)
                    .FirstOrDefault(v => v.Culture.TwoLetterISOLanguageName
                        .Equals(want, StringComparison.OrdinalIgnoreCase));
                if (voice != null) _tts.SelectVoice(voice.Name);
            }
            catch (Exception ex)
            {
                // No matching voice installed — the default one still reads the text out.
                Logger.Log($"Assistant TTS voice selection failed: {ex.Message}");
            }
        }

        // ===================== output =====================

        public void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || _disposed) return;
            try
            {
                _tts.SpeakAsyncCancelAll();
                _tts.SpeakAsync(text);
            }
            catch (Exception ex)
            {
                Logger.Log($"Assistant TTS failed: {ex.Message}");
            }
        }

        public void StopSpeaking()
        {
            if (_disposed) return;
            try { _tts.SpeakAsyncCancelAll(); }
            catch (Exception ex) { Logger.Log($"Assistant TTS stop failed: {ex.Message}"); }
        }

        // ===================== input =====================

        private static Windows.Globalization.Language PickLanguage()
        {
            var want = Localization.IsPolish ? "pl-PL" : "en-US";
            try
            {
                // Dictation only works for languages Windows has a topic model for. Asking for
                // one it doesn't have throws at compile time, so fall back to whatever the
                // system is set up for instead of failing the whole feature.
                var supported = SpeechRecognizer.SupportedTopicLanguages;
                var hit = supported.FirstOrDefault(l =>
                    l.LanguageTag.Equals(want, StringComparison.OrdinalIgnoreCase));
                if (hit != null) return hit;

                var sameLanguage = supported.FirstOrDefault(l =>
                    l.LanguageTag.StartsWith(want.Substring(0, 2), StringComparison.OrdinalIgnoreCase));
                if (sameLanguage != null) return sameLanguage;
            }
            catch (Exception ex)
            {
                Logger.Log($"Assistant STT language probe failed: {ex.Message}");
            }
            return SpeechRecognizer.SystemSpeechLanguage;
        }

        private async Task<SpeechRecognizer> EnsureRecognizerAsync()
        {
            if (_recognizer != null) return _recognizer;

            SpeechRecognizer recognizer;
            try
            {
                recognizer = new SpeechRecognizer(PickLanguage());
            }
            catch (Exception ex)
            {
                throw Unavailable(ex);
            }

            // Long enough that you can think for a beat after pressing the key, short enough
            // that a silent room doesn't leave the window listening forever.
            recognizer.Timeouts.InitialSilenceTimeout = TimeSpan.FromSeconds(5);
            recognizer.Timeouts.EndSilenceTimeout = TimeSpan.FromSeconds(1);
            recognizer.Timeouts.BabbleTimeout = TimeSpan.FromSeconds(4);

            var compiled = await recognizer.CompileConstraintsAsync();
            if (compiled.Status != SpeechRecognitionResultStatus.Success)
            {
                recognizer.Dispose();
                throw Unavailable(null, compiled.Status.ToString());
            }

            _recognizer = recognizer;
            return recognizer;
        }

        private static VoiceUnavailableException Unavailable(Exception? inner, string? status = null)
        {
            // Almost always one of two things: the microphone permission is off, or Windows
            // has "Online speech recognition" disabled — dictation needs it. Say which knobs
            // to check rather than surfacing an HRESULT.
            var msg = Localization.IsPolish
                ? "Nie mogę uruchomić rozpoznawania mowy. Sprawdź w Ustawieniach Windows: "
                  + "Prywatność > Mowa (rozpoznawanie mowy online) oraz dostęp do mikrofonu. "
                  + "Możesz w tym czasie pisać komendy w polu poniżej."
                : "I can't start speech recognition. Check Windows Settings: "
                  + "Privacy > Speech (online speech recognition) and microphone access. "
                  + "You can type commands in the box below in the meantime.";
            if (status != null) Logger.Log($"Assistant STT compile status: {status}");
            return new VoiceUnavailableException(msg, inner);
        }

        // One utterance, or "" when nothing intelligible was said.
        public async Task<string> ListenAsync(CancellationToken ct)
        {
            var recognizer = await EnsureRecognizerAsync();
            ct.ThrowIfCancellationRequested();

            SpeechRecognitionResult result;
            try
            {
                result = await recognizer.RecognizeAsync().AsTask(ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw Unavailable(ex);
            }

            if (result.Status != SpeechRecognitionResultStatus.Success) return "";

            // Rejected means it heard sound but matched nothing — treat that as silence rather
            // than feeding noise into the brain as if it were a command.
            if (result.Confidence == SpeechRecognitionConfidence.Rejected) return "";

            return result.Text ?? "";
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { _tts.SpeakAsyncCancelAll(); } catch (Exception ex) { Logger.Log($"Assistant dispose (tts): {ex.Message}"); }
            _tts.Dispose();
            _recognizer?.Dispose();
            _recognizer = null;
        }
    }
}
