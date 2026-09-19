using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using RLHub.Helpers;
using RLHub2.Helpers;
using RLHub2.Services;

namespace RLHub2.Assistant
{
    // The assistant's face: a small always-on-top panel you talk to.
    //
    // It stays typable as well as speakable on purpose. Speech recognition depends on Windows
    // privacy settings and a working microphone, and when either is missing a voice-only window
    // is a dead window — typing keeps every command reachable while the user sorts that out.
    public class AssistantWindow : Form
    {
        private static AssistantWindow? _instance;
        public static bool IsOpen => _instance != null && !_instance.IsDisposed;

        public static void Toggle()
        {
            if (IsOpen) { _instance!.Close(); _instance = null; }
            else { _instance = new AssistantWindow(); _instance.Show(); }
        }

        public static void ShowAndListen()
        {
            if (!IsOpen) { _instance = new AssistantWindow(); _instance.Show(); }
            _instance!.BeginListen();
        }

        // ---- global push-to-talk ----
        [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HotkeyId = 0xA55;
        private const uint ModControl = 0x0002, ModShift = 0x0004, ModNoRepeat = 0x4000;
        private const uint VkSpace = 0x20;
        private const int WmHotkey = 0x0312;
        private bool _hotkeyRegistered;

        private readonly VoiceIO _voice = new();
        private readonly AssistantBrain _brain = new();
        private CancellationTokenSource? _cts;
        private bool _busy;

        private readonly RichTextBox _log = new();
        private readonly TextBox _input = new();
        private readonly Button _talk = new();
        private readonly Label _status = new();
        private readonly Label _title = new();

        private static bool Pl => Localization.IsPolish;

        public AssistantWindow()
        {
            // Trains or loads the intent model off the UI thread while the window is being built,
            // so the first spoken command doesn't wait on it. Doing nothing is a valid outcome:
            // the assistant falls back to the rules and the LLM if it isn't ready in time.
            Ml.IntentNet.WarmUp();

            Text = Pl ? "Asystent NexPlay" : "NexPlay Assistant";
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Theme.PageBg;
            ForeColor = Theme.TextPrimary;
            Font = new Font("Segoe UI", 9.5F);
            ClientSize = new Size(420, 380);

            // Bottom-right, clear of the taskbar — the same corner the overlay lives in.
            var wa = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
            Location = new Point(wa.Right - Width - 24, wa.Bottom - Height - 24);

            _title.Text = Pl ? "ASYSTENT" : "ASSISTANT";
            _title.Dock = DockStyle.Top;
            _title.Height = 34;
            _title.ForeColor = Theme.Accent;
            _title.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            _title.TextAlign = ContentAlignment.MiddleLeft;
            _title.Padding = new Padding(12, 0, 0, 0);

            _status.Dock = DockStyle.Top;
            _status.Height = 22;
            _status.ForeColor = Theme.TextMuted;
            _status.TextAlign = ContentAlignment.MiddleLeft;
            _status.Padding = new Padding(12, 0, 0, 0);
            _status.Text = Pl ? "Ctrl+Shift+Spacja — mów" : "Ctrl+Shift+Space — talk";

            _log.Dock = DockStyle.Fill;
            _log.ReadOnly = true;
            _log.BorderStyle = BorderStyle.None;
            _log.BackColor = Theme.Surface;
            _log.ForeColor = Theme.TextPrimary;
            _log.Font = new Font("Segoe UI", 10F);
            _log.TabStop = false;

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 40, BackColor = Theme.PageBg };

            _input.Dock = DockStyle.Fill;
            _input.BorderStyle = BorderStyle.FixedSingle;
            _input.BackColor = Theme.SurfaceAlt;
            _input.ForeColor = Theme.TextPrimary;
            _input.Font = new Font("Segoe UI", 10F);
            _input.KeyDown += OnInputKeyDown;

            _talk.Dock = DockStyle.Right;
            _talk.Width = 96;
            _talk.Text = Pl ? "MÓW" : "TALK";
            _talk.FlatStyle = FlatStyle.Flat;
            _talk.FlatAppearance.BorderSize = 0;
            _talk.BackColor = Theme.Accent;
            _talk.ForeColor = Color.White;
            _talk.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _talk.Click += (s, e) => BeginListen();

            bottom.Controls.Add(_input);
            bottom.Controls.Add(_talk);
            bottom.Padding = new Padding(12, 6, 12, 6);

            Controls.Add(_log);
            Controls.Add(bottom);
            Controls.Add(_status);
            Controls.Add(_title);

            Say(Pl
                ? "Powiedz albo napisz, np. „jaki mam winrate”, „ostatni mecz”, „otwórz sesję”."
                : "Say or type something, e.g. \"what's my win rate\", \"last match\", \"open session\".",
                Theme.TextMuted);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // NoRepeat matters: holding the chord down would otherwise queue a listen per repeat.
            _hotkeyRegistered = RegisterHotKey(Handle, HotkeyId, ModControl | ModShift | ModNoRepeat, VkSpace);
            if (!_hotkeyRegistered)
                Say(Pl
                    ? "Skrót Ctrl+Shift+Spacja jest zajęty przez inny program — użyj przycisku MÓW."
                    : "Ctrl+Shift+Space is taken by another program — use the TALK button.",
                    Theme.TextMuted);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmHotkey && m.WParam.ToInt32() == HotkeyId)
            {
                BeginListen();
                return;
            }
            base.WndProc(ref m);
        }

        // Opening from the hotkey while a game is focused must not pull focus — that would
        // minimise the game mid-match, which is the one thing an in-game assistant cannot do.
        protected override bool ShowWithoutActivation => true;

        private void OnInputKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            var text = _input.Text.Trim();
            if (text.Length == 0) return;
            _input.Clear();
            _ = HandleAsync(text, spoken: false);
        }

        public void BeginListen()
        {
            if (_busy) return;
            _ = ListenAsync();
        }

        private async Task ListenAsync()
        {
            _busy = true;
            _voice.StopSpeaking();
            SetStatus(Pl ? "Słucham…" : "Listening…");
            try
            {
                _cts?.Cancel();
                _cts = new CancellationTokenSource();
                var heard = await _voice.ListenAsync(_cts.Token);
                if (string.IsNullOrWhiteSpace(heard))
                {
                    SetStatus(Pl ? "Nic nie usłyszałem." : "Didn't hear anything.");
                    _busy = false;
                    return;
                }
                _busy = false;
                await HandleAsync(heard, spoken: true);
                return;
            }
            catch (OperationCanceledException)
            {
                SetStatus(Pl ? "Przerwane." : "Cancelled.");
            }
            catch (VoiceUnavailableException ex)
            {
                Say(ex.Message, Theme.TextMuted);
                SetStatus(Pl ? "Mikrofon niedostępny." : "Microphone unavailable.");
            }
            catch (Exception ex)
            {
                Logger.Log($"Assistant listen failed: {ex}");
                SetStatus(Pl ? "Błąd mikrofonu." : "Microphone error.");
            }
            _busy = false;
        }

        private async Task HandleAsync(string utterance, bool spoken)
        {
            if (_busy) return;
            _busy = true;
            Say((Pl ? "Ty: " : "You: ") + utterance, Theme.TextSecondary);
            SetStatus(Pl ? "Myślę…" : "Thinking…");

            try
            {
                _cts?.Cancel();
                _cts = new CancellationTokenSource();
                var answer = await _brain.AskAsync(utterance, _cts.Token);
                Say(answer, Theme.TextPrimary);

                // Read back what it said only when the exchange started with speech, or the
                // window would start talking over a user who is quietly typing.
                if (spoken && new SettingsStore().Load().AssistantSpeak) _voice.Speak(answer);

                SetStatus(_brain.AwaitingConfirmation
                    ? (Pl ? "Powiedz tak albo nie." : "Say yes or no.")
                    : (Pl ? "Ctrl+Shift+Spacja — mów" : "Ctrl+Shift+Space — talk"));
            }
            catch (OperationCanceledException)
            {
                SetStatus(Pl ? "Przerwane." : "Cancelled.");
            }
            catch (Exception ex)
            {
                Logger.Log($"Assistant turn failed: {ex}");
                Say(Pl ? "Coś poszło nie tak." : "Something went wrong.", Theme.TextMuted);
                SetStatus("");
            }
            _busy = false;
        }

        private void SetStatus(string text)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { BeginInvoke(new Action(() => SetStatus(text))); return; }
            _status.Text = text;
        }

        private void Say(string text, Color color)
        {
            if (IsDisposed) return;
            if (InvokeRequired) { BeginInvoke(new Action(() => Say(text, color))); return; }
            _log.SelectionStart = _log.TextLength;
            _log.SelectionLength = 0;
            _log.SelectionColor = color;
            _log.AppendText(text + Environment.NewLine + Environment.NewLine);
            _log.SelectionColor = _log.ForeColor;
            _log.ScrollToCaret();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_hotkeyRegistered) UnregisterHotKey(Handle, HotkeyId);
            _cts?.Cancel();
            _voice.Dispose();
            _instance = null;
            base.OnFormClosed(e);
        }
    }
}
