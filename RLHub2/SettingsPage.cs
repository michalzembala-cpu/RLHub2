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
        protected override string ArenaFile => "stadion2.jpg";

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
