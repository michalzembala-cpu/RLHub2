using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using RLHub2.Helpers;
using RLHub2.Models;

namespace RLHub2
{
    // Confirms an Overwatch rank read. The screen OCR pre-fills the dropdowns; the user corrects
    // anything wrong and saves — so what we store is always user-confirmed, never a blind guess.
    public class OwRankDialog : Form
    {
        private static readonly string[] TierList =
            { "—", "Bronze", "Silver", "Gold", "Platinum", "Diamond", "Master", "Grandmaster", "Champion" };
        private static readonly string[] DivList = { "—", "5", "4", "3", "2", "1" };

        private readonly (ComboBox tier, ComboBox div)[] _rows = new (ComboBox, ComboBox)[3];
        private static readonly string[] RoleKeys = { "tank", "damage", "support" };

        public OwRankSnapshot Result { get; private set; } = new();

        public OwRankDialog(Dictionary<string, string> ocr)
        {
            bool pl = Localization.IsPolish;
            Text = pl ? "Odczyt rangi" : "Rank read";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            BackColor = Theme.PageBg; ForeColor = Theme.TextPrimary;
            Font = new Font("Segoe UI", 9.5F);
            ClientSize = new Size(380, 300);

            var title = new Label
            {
                Text = pl ? "Twoja ranga na role" : "Your rank per role",
                Location = new Point(24, 20), AutoSize = true,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            };
            var hint = new Label
            {
                Text = pl ? "OCR podpowiedział — popraw, jeśli trzeba." : "OCR pre-filled — fix anything wrong.",
                Location = new Point(24, 50), AutoSize = true, ForeColor = Theme.TextMuted,
            };
            Controls.Add(title); Controls.Add(hint);

            string[] labels = { "TANK", "DPS", "SUPPORT" };
            int y = 86;
            for (int i = 0; i < 3; i++)
            {
                Controls.Add(new Label
                {
                    Text = labels[i], Location = new Point(24, y + 4), Size = new Size(80, 22),
                    ForeColor = Theme.TextSecondary, Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                });
                var tier = Combo(TierList, new Point(104, y), 150);
                var div = Combo(DivList, new Point(262, y), 60);
                _rows[i] = (tier, div);
                Controls.Add(tier); Controls.Add(div);

                if (ocr.TryGetValue(RoleKeys[i], out var val)) Prefill(tier, div, val);
                y += 44;
            }

            var save = Flat(pl ? "ZAPISZ" : "SAVE", 262, 254, Theme.Accent, Color.Black);
            save.Click += (s, e) => { Result = Build(); DialogResult = DialogResult.OK; Close(); };
            var cancel = Flat(pl ? "ANULUJ" : "CANCEL", 154, 254, Theme.Surface, Theme.TextPrimary);
            cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(save); Controls.Add(cancel);
            AcceptButton = save; CancelButton = cancel;
        }

        private static void Prefill(ComboBox tier, ComboBox div, string val)
        {
            if (string.IsNullOrWhiteSpace(val)) return;
            var parts = val.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 1) tier.SelectedItem = Array.Find(TierList, t => string.Equals(t, parts[0], StringComparison.OrdinalIgnoreCase)) ?? "—";
            if (parts.Length >= 2) div.SelectedItem = Array.IndexOf(DivList, parts[1]) >= 0 ? parts[1] : "—";
        }

        private string RankOf(int i)
        {
            var (tier, div) = _rows[i];
            string t = tier.SelectedItem as string ?? "—";
            if (t == "—") return "";
            string d = div.SelectedItem as string ?? "—";
            return d == "—" ? t : $"{t} {d}";
        }

        private OwRankSnapshot Build() => new()
        {
            Time = DateTime.Now,
            Tank = RankOf(0),
            Damage = RankOf(1),
            Support = RankOf(2),
        };

        private static ComboBox Combo(string[] items, Point at, int w)
        {
            var c = new ComboBox
            {
                Location = at, Size = new Size(w, 28), DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.Surface, ForeColor = Theme.TextPrimary, FlatStyle = FlatStyle.Flat,
            };
            c.Items.AddRange(items);
            c.SelectedIndex = 0;
            return c;
        }

        private static Button Flat(string text, int x, int y, Color back, Color fore)
        {
            var b = new Button
            {
                Text = text, Location = new Point(x, y), Size = new Size(100, 32), FlatStyle = FlatStyle.Flat,
                BackColor = back, ForeColor = fore, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand,
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
    }
}
