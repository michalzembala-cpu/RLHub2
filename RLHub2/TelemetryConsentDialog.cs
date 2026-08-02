using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using RLHub2.Helpers;

namespace RLHub2
{
    // First-run consent for anonymous usage stats. Opt-in: closing the window counts as "no".
    // Result is read from DialogResult (Yes = opted in, anything else = declined).
    public class TelemetryConsentDialog : Form
    {
        public bool Consent { get; private set; }

        public TelemetryConsentDialog()
        {
            bool pl = Localization.IsPolish;

            Text = pl ? "Pomóż ulepszać NexPlay" : "Help improve NexPlay";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(460, 260);
            BackColor = Theme.PageBg;
            ForeColor = Theme.TextPrimary;
            Font = new Font("Segoe UI", 9.5F);

            var title = new Label
            {
                Text = pl ? "Anonimowe statystyki" : "Anonymous usage stats",
                Location = new Point(26, 24),
                AutoSize = true,
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
            };

            var body = new Label
            {
                Location = new Point(26, 62),
                Size = new Size(408, 110),
                ForeColor = Theme.TextSecondary,
                Font = new Font("Segoe UI", 10F),
                Text = pl
                    ? "Zgadzasz się wysyłać anonimowe statystyki, które pomagają ulepszać aplikację?\n\n" +
                      "• Wysyłane: uruchomienie, otwierane zakładki, typ błędu, wersja i system.\n" +
                      "• NIE wysyłane: nick, adres IP, ścieżki plików, żadne dane osobowe.\n\n" +
                      "Możesz to zmienić w każdej chwili w Ustawieniach."
                    : "Would you send anonymous usage stats that help improve the app?\n\n" +
                      "• Sent: app start, tabs you open, error type, version and OS.\n" +
                      "• Never sent: your nick, IP address, file paths, any personal data.\n\n" +
                      "You can change this any time in Settings.",
            };

            var no = Flat(pl ? "NIE, DZIĘKUJĘ" : "NO THANKS", 210, 210, 110, Theme.Surface, Theme.TextPrimary);
            no.Click += (s, e) => { Consent = false; DialogResult = DialogResult.No; Close(); };

            var yes = Flat(pl ? "ZGADZAM SIĘ" : "I AGREE", 330, 210, 104, Theme.Accent, Color.Black);
            yes.Click += (s, e) => { Consent = true; DialogResult = DialogResult.Yes; Close(); };

            Controls.Add(title);
            Controls.Add(body);
            Controls.Add(no);
            Controls.Add(yes);

            AcceptButton = yes;
            CancelButton = no;   // Esc / close = decline
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
