using System;
using System.Drawing;
using System.Windows.Forms;
using RLHub2.Helpers;

namespace RLHub2
{
    // Create a new account: the current in-game name, plus any previous names (aliases) so
    // replays and matches from before a rename still get attributed to it.
    public class AddProfileDialog : Form
    {
        public string ProfileName { get; private set; } = "";
        public string[] Aliases { get; private set; } = Array.Empty<string>();

        private readonly TextBox _name;
        private readonly TextBox _aliases;

        public AddProfileDialog()
        {
            bool pl = Localization.IsPolish;

            Text = pl ? "Dodaj profil" : "Add profile";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(440, 250);
            BackColor = Theme.PageBg;
            ForeColor = Theme.TextPrimary;
            Font = new Font("Segoe UI", 9.5F);

            Controls.Add(Caption(pl ? "Nazwa w grze" : "In-game name", 24, 22));
            _name = Input(24, 46, 392);

            Controls.Add(Hint(pl
                ? "Dokładnie tak, jak wyświetlasz się w grze."
                : "Exactly as you appear in-game.", 24, 78));

            Controls.Add(Caption(pl ? "Poprzednie nazwy (opcjonalnie)" : "Previous names (optional)", 24, 112));
            _aliases = Input(24, 136, 392);

            Controls.Add(Hint(pl
                ? "Po przecinku — mecze sprzed zmiany nazwy nadal się dopasują."
                : "Comma-separated — matches from before a rename still count.", 24, 168));

            var cancel = Flat(pl ? "ANULUJ" : "CANCEL", 210, 202, 100, Theme.Surface, Theme.TextPrimary);
            cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancel);

            var ok = Flat(pl ? "DODAJ" : "ADD", 320, 202, 100, Theme.Accent, Color.Black);
            ok.Click += OnAdd;
            Controls.Add(ok);

            AcceptButton = ok;
            CancelButton = cancel;
            ActiveControl = _name;
        }

        private void OnAdd(object? sender, EventArgs e)
        {
            var name = _name.Text.Trim();
            if (name.Length == 0)
            {
                Toast.Show(this, Localization.IsPolish ? "Podaj nazwę" : "Enter a name", ToastKind.Info);
                _name.Focus();
                return;
            }
            ProfileName = name;
            Aliases = _aliases.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            DialogResult = DialogResult.OK;
            Close();
        }

        private static Label Caption(string text, int x, int y) => new()
        {
            Text = text, Location = new Point(x, y), AutoSize = true,
            ForeColor = Theme.TextSecondary, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
        };

        private static Label Hint(string text, int x, int y) => new()
        {
            Text = text, Location = new Point(x, y), AutoSize = true,
            ForeColor = Theme.TextMuted, Font = new Font("Segoe UI", 8.5F),
        };

        private TextBox Input(int x, int y, int w)
        {
            var t = new TextBox
            {
                Location = new Point(x, y), Width = w,
                BackColor = Theme.Surface, ForeColor = Theme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 11F),
            };
            Controls.Add(t);
            return t;
        }

        private static Button Flat(string text, int x, int y, int w, Color back, Color fore)
        {
            var b = new Button
            {
                Text = text, Location = new Point(x, y), Size = new Size(w, 32),
                FlatStyle = FlatStyle.Flat, BackColor = back, ForeColor = fore,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand,
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
    }
}
