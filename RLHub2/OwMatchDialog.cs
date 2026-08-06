using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using RLHub2.Controls;
using RLHub2.Helpers;
using RLHub2.Models;

namespace RLHub2
{
    // Confirms an end-of-match read. The scoreboard OCR pre-fills the stat fields; the user picks
    // the result/role and corrects any misread number, so the saved match is user-confirmed.
    public class OwMatchDialog : Form
    {
        private readonly SegmentedControl _result;
        private readonly SegmentedControl _role;
        private readonly TextBox _elim, _assist, _death, _dmg, _heal;

        public OwMatch Result { get; private set; } = new();

        public OwMatchDialog(List<int> ocr)
        {
            bool pl = Localization.IsPolish;
            Text = pl ? "Mecz — statystyki" : "Match — stats";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            BackColor = Theme.PageBg; ForeColor = Theme.TextPrimary;
            Font = new Font("Segoe UI", 9.5F);
            ClientSize = new Size(400, 400);

            Controls.Add(new Label
            {
                Text = pl ? "Wynik" : "Result", Location = new Point(24, 20), AutoSize = true,
                ForeColor = Theme.TextSecondary, Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            });
            _result = new SegmentedControl { Location = new Point(24, 44), Size = new Size(352, 40) };
            _result.SetOptions(new[] { pl ? "Wygrana" : "Win", pl ? "Remis" : "Draw", pl ? "Przegrana" : "Loss" });
            _result.SetSelectedSilent(0);
            Controls.Add(_result);

            Controls.Add(new Label
            {
                Text = pl ? "Rola" : "Role", Location = new Point(24, 96), AutoSize = true,
                ForeColor = Theme.TextSecondary, Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            });
            _role = new SegmentedControl { Location = new Point(24, 120), Size = new Size(352, 40) };
            _role.SetOptions(new[] { pl ? "Dowolna" : "Any", "Tank", "DPS", "Support" });
            _role.SetSelectedSilent(0);
            Controls.Add(_role);

            // Pre-fill from the row's numbers in OW's column order.
            int G(int i) => ocr != null && i < ocr.Count ? ocr[i] : 0;
            _elim = StatBox(pl ? "Eliminacje" : "Eliminations", 176, G(0));
            _assist = StatBox(pl ? "Asysty" : "Assists", 210, G(1));
            _death = StatBox(pl ? "Śmierci" : "Deaths", 244, G(2));
            _dmg = StatBox(pl ? "Obrażenia" : "Damage", 278, G(3));
            _heal = StatBox(pl ? "Leczenie" : "Healing", 312, G(4));

            var hint = new Label
            {
                Text = pl ? "OCR podpowiedział — popraw liczby, jeśli trzeba." : "OCR pre-filled — fix any number.",
                Location = new Point(24, 348), AutoSize = true, ForeColor = Theme.TextMuted,
            };
            Controls.Add(hint);

            var save = Flat(pl ? "ZAPISZ" : "SAVE", 276, 366, Theme.Accent, Color.Black);
            save.Click += (s, e) => { Result = Build(); DialogResult = DialogResult.OK; Close(); };
            var cancel = Flat(pl ? "ANULUJ" : "CANCEL", 168, 366, Theme.Surface, Theme.TextPrimary);
            cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(save); Controls.Add(cancel);
            AcceptButton = save; CancelButton = cancel;
        }

        private TextBox StatBox(string label, int y, int value)
        {
            Controls.Add(new Label
            {
                Text = label, Location = new Point(24, y + 4), Size = new Size(180, 22), ForeColor = Theme.TextPrimary,
            });
            var box = new TextBox
            {
                Location = new Point(210, y), Size = new Size(166, 26),
                BackColor = Theme.Surface, ForeColor = Theme.TextPrimary, BorderStyle = BorderStyle.FixedSingle,
                Text = value > 0 ? value.ToString() : "",
            };
            box.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };
            Controls.Add(box);
            return box;
        }

        private static int Parse(TextBox b) => int.TryParse(b.Text.Trim(), out int v) ? v : 0;

        private OwMatch Build() => new()
        {
            Time = DateTime.Now,
            Result = _result.SelectedIndex switch { 1 => "D", 2 => "L", _ => "W" },
            Role = _role.SelectedIndex switch { 1 => "tank", 2 => "damage", 3 => "support", _ => "" },
            Eliminations = Parse(_elim),
            Assists = Parse(_assist),
            Deaths = Parse(_death),
            Damage = Parse(_dmg),
            Healing = Parse(_heal),
        };

        private static Button Flat(string text, int x, int y, Color back, Color fore)
        {
            var b = new Button
            {
                Text = text, Location = new Point(x, y), Size = new Size(100, 30), FlatStyle = FlatStyle.Flat,
                BackColor = back, ForeColor = fore, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand,
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
    }
}
