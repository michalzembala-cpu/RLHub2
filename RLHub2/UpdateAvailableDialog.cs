using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using RLHub2.Helpers;
using RLHub2.Services;

namespace RLHub2
{
    // Shown at startup when a newer release exists — update right here, no trip to Settings.
    // "Update now" downloads and launches the installer; "Later" just closes.
    public class UpdateAvailableDialog : Form
    {
        private readonly UpdateInfo _info;
        private readonly Button _update;
        private readonly Button _later;
        private readonly Label _status;

        public UpdateAvailableDialog(UpdateInfo info)
        {
            _info = info;
            bool pl = Localization.IsPolish;

            Text = pl ? "Dostępna aktualizacja" : "Update available";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Theme.PageBg;
            ForeColor = Theme.TextPrimary;
            Font = new Font("Segoe UI", 9.5F);
            ClientSize = new Size(440, 268);

            var title = new Label
            {
                Text = pl ? $"Nowa wersja {info.Version}" : $"New version {info.Version}",
                Location = new Point(26, 22), AutoSize = true,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold), ForeColor = Theme.TextPrimary,
            };
            var sub = new Label
            {
                Text = pl ? $"Masz {UpdateService.CurrentVersionText}. Zaktualizować teraz?"
                          : $"You have {UpdateService.CurrentVersionText}. Update now?",
                Location = new Point(26, 54), AutoSize = true, ForeColor = Theme.TextMuted,
            };

            var notes = new TextBox
            {
                Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, TabStop = false,
                Location = new Point(26, 84), Size = new Size(388, 100),
                BackColor = Theme.Surface, ForeColor = Theme.TextSecondary, BorderStyle = BorderStyle.FixedSingle,
                Text = string.IsNullOrWhiteSpace(info.Notes)
                    ? (pl ? "Poprawki i usprawnienia." : "Fixes and improvements.")
                    : info.Notes.Trim(),
            };

            _status = new Label { Location = new Point(26, 192), Size = new Size(388, 20), ForeColor = Theme.TextMuted };

            _later = Flat(pl ? "PÓŹNIEJ" : "LATER", 206, 222, 100, Theme.Surface, Theme.TextPrimary);
            _later.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            _update = Flat(pl ? "AKTUALIZUJ" : "UPDATE NOW", 314, 222, 100, Theme.Accent, Color.Black);
            _update.Click += async (s, e) => await DoUpdate();

            Controls.AddRange(new Control[] { title, sub, notes, _status, _later, _update });
            AcceptButton = _update;
            CancelButton = _later;
        }

        private async Task DoUpdate()
        {
            bool pl = Localization.IsPolish;
            _update.Enabled = false;
            _later.Enabled = false;
            _status.ForeColor = Theme.TextMuted;

            var progress = new Progress<int>(p =>
                _status.Text = (pl ? "Pobieram… " : "Downloading… ") + p + "%");
            try
            {
                var path = await new UpdateService().DownloadAsync(_info, progress);
                if (path == null) { Fail(); return; }

                _status.Text = pl ? "Instaluję — aplikacja się zrestartuje…" : "Installing — the app will restart…";
                if (UpdateService.ApplyAndRestart(path)) { Application.Exit(); return; }
                Fail();
            }
            catch { Fail(); }
        }

        private void Fail()
        {
            _update.Enabled = true;
            _later.Enabled = true;
            _status.ForeColor = Color.FromArgb(230, 90, 90);
            _status.Text = Localization.IsPolish
                ? "Nie udało się pobrać. Spróbuj w Ustawieniach."
                : "Update failed. Try again from Settings.";
        }

        private static Button Flat(string text, int x, int y, int w, Color back, Color fore)
        {
            var b = new Button
            {
                Text = text, Location = new Point(x, y), Size = new Size(w, 34),
                FlatStyle = FlatStyle.Flat, BackColor = back, ForeColor = fore,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand,
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
    }
}
