using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using RLHub2.Helpers;
using RLHub2.Services;

namespace RLHub2
{
    public partial class SettingsPage : Controls.ArenaControl
    {
        // Settings is shared by both games, so its backdrop follows whichever one is active —
        // a Rocket League stadium behind CS2 settings is the same mismatch as on the CS2 pages.
        protected override string ArenaFile
            => Games.Active == GameId.Cs2 ? "cs2_bg.png" : "rl_bg.png";

        private readonly SettingsStore _store = new();

        // Cards never grow past this — full-width text fields on a 1920px window would be
        // unreadable, and settings read better in a column.
        private const int MaxCardWidth = 900;

        public SettingsPage()
        {
            InitializeComponent();
            ApplyLanguage();

            flow.Resize += (s, e) => FitCards();
            FitCards();

            segLanguage.SetSelectedSilent(Localization.IsPolish ? 0 : 1);
            segTheme.SetSelectedSilent(Theme.IsDark ? 0 : 1);
            txtKey.Text = _store.LoadTrackerKey();
            txtBcKey.Text = _store.LoadBallchasingKey();

            // updates
            txtUpdRepo.Text = _store.LoadUpdateRepo();
            chkAutoUpd.Checked = _store.LoadAutoCheckUpdates();
            chkAutoUpd.CheckedChanged += (s, e) => _store.SaveAutoCheckUpdates(chkAutoUpd.Checked);
            btnCheckUpd.Click += async (s, e) => await CheckUpdates();
            btnInstallUpd.Click += async (s, e) => await InstallUpdate();

            // game switcher — opens the same Big-Picture-style picker as launch
            UpdateGameButton();
            btnGame.Click += (s, e) =>
            {
                using var picker = new GamePickerForm();
                picker.ShowDialog(FindForm());
                UpdateGameButton();
            };
            chkAskGame.Checked = _store.LoadAskGameOnStart();
            chkAskGame.CheckedChanged += (s, e) => _store.SaveAskGameOnStart(chkAskGame.Checked);

            // Profiles are a Rocket League idea — CS2 is whoever is signed into Steam.
            bool hasProfiles = Games.HasProfiles(Games.Active);
            keyPanel.Visible = hasProfiles;
            bcPanel.Visible = hasProfiles;

            // account switcher — opens the Big-Picture-style picker
            UpdateProfileButton();
            btnProfile.Click += (s, e) =>
            {
                using var picker = new ProfilePickerForm();
                picker.ShowDialog(FindForm());
                UpdateProfileButton();
            };
            Accounts.ActiveChanged += UpdateProfileButton;
            Disposed += (s, e) => Accounts.ActiveChanged -= UpdateProfileButton;

            chkAskProfile.Checked = _store.LoadAskProfileOnStart();
            chkAskProfile.CheckedChanged += (s, e) =>
                _store.SaveAskProfileOnStart(chkAskProfile.Checked);

            segLanguage.SelectedIndexChanged += (s, e) =>
            {
                var lang = segLanguage.SelectedIndex == 0 ? AppLanguage.Polish : AppLanguage.English;
                _store.SaveLanguage(lang);
                Localization.SetLanguage(lang);
            };

            segTheme.SelectedIndexChanged += (s, e) =>
            {
                var theme = segTheme.SelectedIndex == 1 ? AppTheme.Light : AppTheme.Dark;
                _store.SaveTheme(theme);
                Theme.SetTheme(theme);
            };

            accentPicker.AccentSelected += (s, e) => _store.SaveAccent(Theme.Accent);

            btnSaveKey.Click += (s, e) =>
            {
                _store.SaveTrackerKey(txtKey.Text);
                _store.SaveBallchasingKey(txtBcKey.Text);
                Toast.Show(this, Localization.T("settings_saved"), ToastKind.Success);
            };

            btnTestBc.Click += async (s, e) => await TestBallchasing();

            // auto-recycle replays that are already safely on ballchasing
            chkDeleteOld.Text = Localization.IsPolish
                ? "Usuwaj replaye starsze niż 7 dni (do Kosza, tylko te wgrane na Ballchasing)"
                : "Recycle replays older than 7 days (only ones already on Ballchasing)";
            chkDeleteOld.Checked = _store.LoadDeleteReplaysAfterDays() > 0;
            chkDeleteOld.CheckedChanged += (s, e) =>
                _store.SaveDeleteReplaysAfterDays(chkDeleteOld.Checked ? 7 : 0);
        }

        private UpdateInfo? _pending;

        private async Task CheckUpdates()
        {
            _store.SaveUpdateRepo(txtUpdRepo.Text);
            btnCheckUpd.Enabled = false;
            btnInstallUpd.Visible = false;
            Status(Localization.IsPolish ? "Sprawdzam…" : "Checking…", Theme.TextMuted);

            var info = await new UpdateService().CheckAsync(txtUpdRepo.Text);
            btnCheckUpd.Enabled = true;

            // Each failure means something different to the user, so none of them collapse
            // into a generic "error".
            if (!info.Ok)
            {
                Status(info.Error switch
                {
                    "not-configured" => Localization.IsPolish
                        ? "Podaj repozytorium w formacie uzytkownik/repo"
                        : "Enter a repository as owner/name",
                    "no-release" => Localization.IsPolish
                        ? "To repozytorium nie ma jeszcze żadnego wydania"
                        : "That repository has no releases yet",
                    "bad-tag" => Localization.IsPolish
                        ? "Tag wydania nie wygląda jak wersja (np. v1.2.0)"
                        : "The release tag isn't a version (e.g. v1.2.0)",
                    _ => (Localization.IsPolish ? "Błąd: " : "Error: ") + info.Error,
                }, Color.FromArgb(230, 90, 90));
                return;
            }

            if (!info.Available)
            {
                Status(Localization.IsPolish
                    ? $"Masz najnowszą wersję ({UpdateService.CurrentVersionText})"
                    : $"You're up to date ({UpdateService.CurrentVersionText})", Color.FromArgb(46, 204, 113));
                return;
            }

            if (string.IsNullOrEmpty(info.DownloadUrl))
            {
                Status(Localization.IsPolish
                    ? $"Jest wersja {info.Version}, ale wydanie nie ma pliku do pobrania"
                    : $"Version {info.Version} exists but the release has no downloadable file",
                    Color.FromArgb(222, 130, 40));
                return;
            }

            _pending = info;
            Status(Localization.IsPolish
                ? $"Dostępna wersja {info.Version} (masz {UpdateService.CurrentVersionText})"
                : $"Version {info.Version} available (you have {UpdateService.CurrentVersionText})",
                Color.FromArgb(46, 204, 113));
            btnInstallUpd.Text = Localization.IsPolish ? $"POBIERZ {info.Version}" : $"DOWNLOAD {info.Version}";
            btnInstallUpd.Visible = true;
        }

        private async Task InstallUpdate()
        {
            if (_pending == null) return;
            btnInstallUpd.Enabled = false;

            var progress = new Progress<int>(p => Status(
                (Localization.IsPolish ? "Pobieram… " : "Downloading… ") + p + "%", Theme.TextSecondary));

            var path = await new UpdateService().DownloadAsync(_pending, progress);
            if (path == null)
            {
                btnInstallUpd.Enabled = true;
                Status(Localization.IsPolish ? "Pobieranie nie powiodło się" : "Download failed",
                    Color.FromArgb(230, 90, 90));
                return;
            }

            Status(Localization.IsPolish ? "Instaluję — aplikacja się zrestartuje…" : "Installing — the app will restart…",
                Theme.TextSecondary);

            if (UpdateService.ApplyAndRestart(path))
                Application.Exit();     // the swap can only happen once this process is gone
            else
            {
                btnInstallUpd.Enabled = true;
                Status(Localization.IsPolish ? "Nie udało się uruchomić instalacji" : "Couldn't start the install",
                    Color.FromArgb(230, 90, 90));
            }
        }

        private void Status(string text, Color color)
        {
            lblUpdStatus.Text = text;
            lblUpdStatus.ForeColor = color;
        }

        private void UpdateGameButton()
        {
            if (IsDisposed) return;
            btnGame.Text = "🎮  " + Games.Name(Games.Active) + "     "
                + (Localization.IsPolish ? "— zmień" : "— change");
        }

        private void UpdateProfileButton()
        {
            if (IsDisposed) return;
            var name = Accounts.ActiveName;
            if (string.IsNullOrWhiteSpace(name)) name = Localization.IsPolish ? "brak konta" : "no account";
            btnProfile.Text = "👤  " + name + "     " + (Localization.IsPolish ? "— zmień" : "— change");
        }

        private bool _fitting;

        // Stretch every card to the available width (capped), and re-wrap the hint labels.
        // Two passes: widening the cards can make the vertical scrollbar appear, which shrinks
        // ClientSize — without the second pass the cards would then be a scrollbar too wide and
        // WinForms would add a horizontal scrollbar.
        private void FitCards()
        {
            if (_fitting) return;
            _fitting = true;
            try
            {
                for (int pass = 0; pass < 2; pass++)
                {
                    int avail = flow.ClientSize.Width - flow.Padding.Horizontal;
                    if (avail < 200) return;
                    int w = Math.Min(MaxCardWidth, avail);

                    foreach (Control c in flow.Controls)
                        if (c is Panel card)
                            card.Width = w;

                    lblKeyHint.MaximumSize = new Size(w - 40, 0);
                    lblBcHint.MaximumSize = new Size(w - 40, 0);
                    lblBcStatus.MaximumSize = new Size(w - 40, 0);
                    chkDeleteOld.MaximumSize = new Size(w - 40, 0);
                    chkAskProfile.MaximumSize = new Size(w - 40, 0);
                    chkAskGame.MaximumSize = new Size(w - 40, 0);
                    lblGameHint.MaximumSize = new Size(w - 40, 0);
                    lblUpdHint.MaximumSize = new Size(w - 40, 0);
                    lblUpdStatus.MaximumSize = new Size(w - 40, 0);
                    chkAutoUpd.MaximumSize = new Size(w - 40, 0);

                    flow.PerformLayout();
                    if (!flow.HorizontalScroll.Visible) break;
                }
            }
            finally { _fitting = false; }
        }

        private async Task TestBallchasing()
        {
            _store.SaveBallchasingKey(txtBcKey.Text);
            btnTestBc.Enabled = false;
            lblBcStatus.ForeColor = Theme.TextMuted;
            lblBcStatus.Text = Localization.IsPolish ? "Sprawdzam…" : "Checking…";
            try
            {
                var (ok, msg) = await new BallchasingService().ValidateAsync(txtBcKey.Text.Trim());
                if (ok)
                {
                    lblBcStatus.ForeColor = Color.FromArgb(46, 204, 113);
                    lblBcStatus.Text = (Localization.IsPolish ? "✓ Połączono jako " : "✓ Connected as ") + msg;
                }
                else
                {
                    lblBcStatus.ForeColor = Color.FromArgb(230, 90, 90);
                    lblBcStatus.Text = (Localization.IsPolish ? "✗ Błąd: " : "✗ Error: ") + msg;
                }
            }
            catch (Exception ex)
            {
                lblBcStatus.ForeColor = Color.FromArgb(230, 90, 90);
                lblBcStatus.Text = "✗ " + ex.Message;
            }
            finally { btnTestBc.Enabled = true; }
        }

        private void ApplyLanguage()
        {
            lblTitle.Text = Localization.T("settings_title");
            lblLanguage.Text = Localization.T("settings_language");
            lblLanguageHint.Text = Localization.T("settings_language_hint");
            lblTheme.Text = Localization.T("settings_theme");
            lblThemeHint.Text = Localization.T("settings_theme_hint");
            lblAccent.Text = Localization.T("settings_accent");
            lblAccentHint.Text = Localization.T("settings_accent_hint");
            lblKey.Text = Localization.T("settings_key");
            lblKeyHint.Text = Localization.T("settings_key_hint");
            lblNick.Text = Localization.IsPolish ? "AKTYWNE KONTO" : "ACTIVE ACCOUNT";
            chkAskProfile.Text = Localization.IsPolish
                ? "Pytaj o profil przy starcie aplikacji"
                : "Ask which profile to use on startup";
            lblGame.Text = Localization.IsPolish ? "GRA" : "GAME";
            lblGameHint.Text = Localization.IsPolish
                ? "Którą grę pokazuje aplikacja"
                : "Which game the app shows";
            chkAskGame.Text = Localization.IsPolish
                ? "Pytaj o grę przy starcie aplikacji"
                : "Ask which game to use on startup";
            lblUpd.Text = Localization.IsPolish ? "AKTUALIZACJE" : "UPDATES";
            lblUpdHint.Text = Localization.IsPolish
                ? $"Masz wersję {UpdateService.CurrentVersionText}. Podaj repozytorium GitHub z wydaniami."
                : $"You have version {UpdateService.CurrentVersionText}. Point this at a GitHub repo with releases.";
            btnCheckUpd.Text = Localization.IsPolish ? "SPRAWDŹ" : "CHECK";
            chkAutoUpd.Text = Localization.IsPolish
                ? "Sprawdzaj aktualizacje przy starcie"
                : "Check for updates on startup";
            lblBc.Text = Localization.IsPolish ? "KLUCZ API BALLCHASING" : "BALLCHASING API KEY";
            lblBcHint.Text = Localization.IsPolish
                ? "Darmowy klucz na ballchasing.com/upload → Settings. Daje prawdziwe mecze, rangi i automatyczne MMR."
                : "Free key at ballchasing.com/upload → Settings. Enables real matches, ranks and auto MMR.";
            btnSaveKey.Text = Localization.T("settings_save");

            segLanguage.SetOptions(new[] { "Polski", "English" });
            segTheme.SetOptions(new[] { Localization.T("theme_dark"), Localization.T("theme_light") });
            segLanguage.SetSelectedSilent(Localization.IsPolish ? 0 : 1);
            segTheme.SetSelectedSilent(Theme.IsDark ? 0 : 1);
        }
    }
}
