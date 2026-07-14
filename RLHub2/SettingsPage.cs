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

        public SettingsPage()
        {
            InitializeComponent();
            ApplyLanguage();

            segLanguage.SetSelectedSilent(Localization.IsPolish ? 0 : 1);
            segTheme.SetSelectedSilent(Theme.IsDark ? 0 : 1);
            txtKey.Text = _store.LoadTrackerKey();
            txtBcKey.Text = _store.LoadBallchasingKey();

            // account switcher
            cmbAccount.Items.Clear();
            foreach (var a in Accounts.All) cmbAccount.Items.Add(a.Name);
            cmbAccount.SelectedItem = Accounts.ActiveName;
            cmbAccount.SelectedIndexChanged += (s, e) =>
            {
                if (cmbAccount.SelectedItem is string name) Accounts.SetActive(name);
            };

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
            btnSaveKey.Text = Localization.T("settings_save");

            segLanguage.SetOptions(new[] { "Polski", "English" });
            segTheme.SetOptions(new[] { Localization.T("theme_dark"), Localization.T("theme_light") });
            segLanguage.SetSelectedSilent(Localization.IsPolish ? 0 : 1);
            segTheme.SetSelectedSilent(Theme.IsDark ? 0 : 1);
        }
    }
}
