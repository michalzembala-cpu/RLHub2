using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using RLHub2.Helpers;
using RLHub2.Services;

namespace RLHub2
{
    // Konfiguracja mostu NexHub — łączy NexPlay z NexDrone (Android).
    // URL Cloudflare Workera + token; auto-sync co 5 min w tle.
    public class NexHubDialog : Form
    {
        private readonly SettingsStore _store = new();
        private readonly TextBox _url;
        private readonly TextBox _token;
        private readonly CheckBox _autoSync;
        private readonly Label _status;
        private readonly Button _newProfile;
        private readonly Button _testConn;
        private readonly Button _syncNow;

        public NexHubDialog()
        {
            bool pl = Localization.IsPolish;
            Text = pl ? "NexHub — most do NexDrone" : "NexHub — bridge to NexDrone";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(500, 400);
            BackColor = Theme.PageBg;
            ForeColor = Theme.TextPrimary;
            Font = new Font("Segoe UI", 9.5F);

            var cfg = _store.Load();
            int y = 20;

            Controls.Add(Hint(pl
                ? "NexHub to Twój darmowy Cloudflare Worker. Push'uje MMR/rangę do NexDrone — Jarvis w apce widzi Twoje osiągnięcia z RL."
                : "NexHub is your free Cloudflare Worker. Pushes MMR/rank to NexDrone — Jarvis knows your RL stats.",
                20, y, 460, 40));
            y += 45;

            Controls.Add(Caption(pl ? "URL backendu" : "Backend URL", 20, y));
            _url = Input(20, y + 22, 460);
            _url.Text = cfg.HubUrl;
            y += 60;

            Controls.Add(Caption(pl ? "Token profilu" : "Profile token", 20, y));
            _token = Input(20, y + 22, 460);
            _token.Text = cfg.HubToken;
            _token.PasswordChar = '•';
            y += 60;

            _autoSync = new CheckBox
            {
                Left = 20, Top = y, Width = 460,
                Text = pl ? "Auto-sync co 5 min w tle" : "Auto-sync every 5 min",
                Checked = cfg.HubAutoSync,
                ForeColor = Theme.TextPrimary,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9.5F),
            };
            Controls.Add(_autoSync);
            y += 32;

            _newProfile = Flat(pl ? "NOWY PROFIL" : "NEW PROFILE", 20, y, 145, Theme.Surface, Theme.TextPrimary);
            _newProfile.Click += async (s, e) => await OnCreateAsync();
            Controls.Add(_newProfile);

            _testConn = Flat(pl ? "TEST" : "TEST", 175, y, 90, Theme.Surface, Theme.TextPrimary);
            _testConn.Click += async (s, e) => await OnTestAsync();
            Controls.Add(_testConn);

            _syncNow = Flat(pl ? "SYNC TERAZ" : "SYNC NOW", 275, y, 120, Theme.Surface, Theme.TextPrimary);
            _syncNow.Click += async (s, e) => await OnSyncAsync();
            Controls.Add(_syncNow);

            var save = Flat(pl ? "ZAPISZ" : "SAVE", 395, y, 85, Theme.Accent, Color.Black);
            save.Click += (s, e) => { SaveAndClose(); };
            Controls.Add(save);

            y += 40;

            _status = new Label
            {
                Left = 20, Top = y, Width = 460, Height = 60,
                ForeColor = Theme.TextSecondary,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8.5F),
                Text = "",
            };
            Controls.Add(_status);
        }

        private void SaveAndClose()
        {
            var cfg = _store.Load();
            cfg.HubUrl = _url.Text.Trim().TrimEnd('/');
            cfg.HubToken = _token.Text.Trim();
            cfg.HubAutoSync = _autoSync.Checked;
            _store.Save(cfg);
            DialogResult = DialogResult.OK;
            Close();
        }

        private async Task OnCreateAsync()
        {
            if (string.IsNullOrWhiteSpace(_url.Text))
            {
                _status.Text = "✗ Wpisz najpierw URL backendu";
                return;
            }
            _status.Text = "Tworzę profil…";
            var token = await NexHubClient.CreateProfileAsync(_url.Text.Trim().TrimEnd('/'));
            if (token != null)
            {
                _token.Text = token;
                _status.Text = $"✓ Nowy profil utworzony:\n{token}\n\nZapisz ten token — wpiszesz go też w NexDrone!";
            }
            else _status.Text = "✗ Nie udało się utworzyć profilu";
        }

        private async Task OnTestAsync()
        {
            if (string.IsNullOrWhiteSpace(_url.Text) || string.IsNullOrWhiteSpace(_token.Text))
            {
                _status.Text = "✗ Wpisz URL + token";
                return;
            }
            _status.Text = "Sprawdzam…";
            var client = new NexHubClient(_url.Text.Trim().TrimEnd('/'), _token.Text.Trim());
            var ok = await client.PutMetaAsync(pilotName: Helpers.Accounts.ActiveName);
            _status.Text = ok ? "✓ Połączenie OK" : "✗ Backend nie odpowiada — sprawdź URL/token";
        }

        private async Task OnSyncAsync()
        {
            // Zapisz aktualną konfigurację przed synchronizacją
            var cfg = _store.Load();
            cfg.HubUrl = _url.Text.Trim().TrimEnd('/');
            cfg.HubToken = _token.Text.Trim();
            _store.Save(cfg);

            _status.Text = "Synchronizuję…";
            var svc = new NexHubSyncService();
            var ok = await svc.SyncOnceAsync();
            _status.Text = ok
                ? "✓ MMR wysłany do NexHub — Jarvis w NexDrone już wie"
                : "✗ Brak danych MMR albo błąd sieci";
        }

        // --- helpers (skopiowane ze wzorca AddProfileDialog) ---
        private static Label Caption(string text, int x, int y) => new Label
        {
            Left = x, Top = y, AutoSize = true,
            Text = text,
            ForeColor = Theme.TextPrimary,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
        };
        private static Label Hint(string text, int x, int y, int w, int h) => new Label
        {
            Left = x, Top = y, Width = w, Height = h,
            Text = text,
            ForeColor = Theme.TextSecondary,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 8.5F),
        };
        private static TextBox Input(int x, int y, int w) => new TextBox
        {
            Left = x, Top = y, Width = w,
            BackColor = Theme.Surface,
            ForeColor = Theme.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
        };
        private static Button Flat(string text, int x, int y, int w, Color bg, Color fg) => new Button
        {
            Left = x, Top = y, Width = w, Height = 34,
            Text = text,
            BackColor = bg,
            ForeColor = fg,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
        };
    }
}
