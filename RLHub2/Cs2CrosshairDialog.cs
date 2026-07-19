using System;
using System.Drawing;
using System.Windows.Forms;
using RLHub2.Helpers;
using RLHub2.Models;

namespace RLHub2
{
    // Editor for a user's own crosshair: tweak the numbers, watch the preview, save.
    // Also parses a pasted block of cl_crosshair* commands, so a crosshair shared by a friend
    // (or copied out of your own config) comes straight in.
    public class Cs2CrosshairDialog : Form
    {
        public CrosshairDef Result { get; private set; } = new() { Custom = true };

        private readonly TextBox _name;
        private readonly ComboBox _style;
        private readonly NumericUpDown _size, _thick, _gap, _outline, _alpha;
        private readonly CheckBox _dot, _tstyle;
        private readonly Button _colour;
        private readonly Panel _preview;

        private bool _loading = true;

        public Cs2CrosshairDialog(CrosshairDef? existing = null)
        {
            bool pl = Localization.IsPolish;

            Text = existing == null
                ? (pl ? "Nowy celownik" : "New crosshair")
                : (pl ? "Edytuj celownik" : "Edit crosshair");
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(660, 452);
            BackColor = Theme.PageBg;
            ForeColor = Theme.TextPrimary;
            Font = new Font("Segoe UI", 9.5F);

            if (existing != null) Result = existing.Clone();
            Result.Custom = true;

            // ----- preview -----
            _preview = new Panel
            {
                Location = new Point(24, 60),
                Size = new Size(200, 200),
                BackColor = CrosshairRenderer.Backdrop,
            };
            _preview.Paint += (s, e) => CrosshairRenderer.Draw(e.Graphics, Result, 200);
            Controls.Add(_preview);
            Controls.Add(Header(pl ? "PODGLĄD" : "PREVIEW", 24, 32));

            // ----- fields -----
            const int lx = 260, fx = 420, fw = 200;
            int y = 60;
            int Row() { int r = y; y += 38; return r; }

            Controls.Add(Header(pl ? "USTAWIENIA" : "SETTINGS", lx, 32));

            int rName = Row();
            Controls.Add(Caption(pl ? "Nazwa" : "Name", lx, rName));
            _name = new TextBox
            {
                Location = new Point(fx, rName - 3),
                Width = fw,
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Text = Result.Name,
            };
            Controls.Add(_name);

            int rStyle = Row();
            Controls.Add(Caption(pl ? "Styl" : "Style", lx, rStyle));
            _style = new ComboBox
            {
                Location = new Point(fx, rStyle - 3),
                Width = fw,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                FlatStyle = FlatStyle.Flat,
            };
            _style.Items.AddRange(new object[]
            {
                pl ? "0 — domyślny" : "0 — default",
                pl ? "1 — domyślny statyczny" : "1 — default static",
                pl ? "2 — klasyczny" : "2 — classic",
                pl ? "3 — klasyczny dynamiczny" : "3 — classic dynamic",
                pl ? "4 — klasyczny statyczny (pro)" : "4 — classic static (pro)",
                pl ? "5 — klasyczny (lekki ruch)" : "5 — classic (light motion)",
            });
            _style.SelectedIndex = Math.Clamp(Result.Style, 0, 5);
            Controls.Add(_style);

            _size = Num(lx, fx, fw, Row(), pl ? "Rozmiar" : "Size", 0, 10, 1, (decimal)Result.Size);
            _thick = Num(lx, fx, fw, Row(), pl ? "Grubość" : "Thickness", 0, 3, 1, (decimal)Result.Thickness);
            _gap = Num(lx, fx, fw, Row(), pl ? "Odstęp" : "Gap", -5, 5, 0, Result.Gap);
            _outline = Num(lx, fx, fw, Row(), pl ? "Kontur" : "Outline", 0, 3, 0, Result.Outline);
            _alpha = Num(lx, fx, fw, Row(), pl ? "Krycie" : "Alpha", 0, 255, 0, Result.Alpha);

            int rCol = Row();
            Controls.Add(Caption(pl ? "Kolor" : "Colour", lx, rCol));
            _colour = new Button
            {
                Location = new Point(fx, rCol - 4),
                Size = new Size(fw, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(Result.R, Result.G, Result.B),
                Text = "",
                Cursor = Cursors.Hand,
            };
            _colour.FlatAppearance.BorderColor = Theme.TextMuted;
            _colour.Click += PickColour;
            Controls.Add(_colour);

            int rChecks = Row();
            _dot = Check(pl ? "Kropka" : "Centre dot", lx, rChecks, Result.Dot);
            _tstyle = Check(pl ? "Styl T" : "T style", lx + 130, rChecks, Result.T);

            // ----- buttons -----
            var paste = Flat(pl ? "WKLEJ ZE SCHOWKA" : "PASTE FROM CLIPBOARD", 24, 400, 190, Theme.Surface, Theme.TextPrimary);
            paste.Click += PasteFromClipboard;
            Controls.Add(paste);

            var cancel = Flat(pl ? "ANULUJ" : "CANCEL", 400, 400, 110, Theme.Surface, Theme.TextPrimary);
            cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancel);

            var save = Flat(pl ? "ZAPISZ" : "SAVE", 522, 400, 110, Theme.Accent, Color.Black);
            save.Click += Save;
            Controls.Add(save);

            CancelButton = cancel;

            _loading = false;
            Sync();
        }

        // ---------- small builders ----------

        private Label Header(string text, int x, int y) => new()
        {
            Text = text,
            Location = new Point(x, y),
            AutoSize = true,
            ForeColor = Theme.AccentSoft,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
        };

        private Label Caption(string text, int x, int y) => new()
        {
            Text = text,
            Location = new Point(x, y),
            AutoSize = true,
            ForeColor = Theme.TextSecondary,
        };

        private NumericUpDown Num(int lx, int fx, int fw, int y, string caption,
                                  decimal min, decimal max, int decimals, decimal value)
        {
            Controls.Add(Caption(caption, lx, y));
            var n = new NumericUpDown
            {
                Location = new Point(fx, y - 3),
                Width = fw,
                Minimum = min,
                Maximum = max,
                DecimalPlaces = decimals,
                Increment = decimals > 0 ? 0.1m : 1m,
                Value = Math.Clamp(value, min, max),
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
            };
            n.ValueChanged += (s, e) => Sync();
            Controls.Add(n);
            return n;
        }

        private CheckBox Check(string text, int x, int y, bool value)
        {
            var c = new CheckBox
            {
                Text = text,
                Location = new Point(x, y - 2),
                AutoSize = true,
                ForeColor = Theme.TextSecondary,
                Checked = value,
                Cursor = Cursors.Hand,
            };
            c.CheckedChanged += (s, e) => Sync();
            Controls.Add(c);
            return c;
        }

        private static Button Flat(string text, int x, int y, int w, Color back, Color fore)
        {
            var b = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = back,
                ForeColor = fore,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        // ---------- behaviour ----------

        // Pull every field into Result, then repaint. One direction only, so the preview can
        // never disagree with what will be saved.
        private void Sync()
        {
            if (_loading) return;

            Result.Name = _name.Text.Trim();
            Result.Style = _style.SelectedIndex;
            Result.Size = (float)_size.Value;
            Result.Thickness = (float)_thick.Value;
            Result.Gap = (int)_gap.Value;
            Result.Outline = (int)_outline.Value;
            Result.Alpha = (int)_alpha.Value;
            Result.Dot = _dot.Checked;
            Result.T = _tstyle.Checked;
            Result.R = _colour.BackColor.R;
            Result.G = _colour.BackColor.G;
            Result.B = _colour.BackColor.B;

            _preview.Invalidate();
        }

        // Push Result back out to the fields — used after a paste, where the values arrive
        // from outside instead of from the user.
        private void LoadFromResult()
        {
            _loading = true;
            _style.SelectedIndex = Math.Clamp(Result.Style, 0, 5);
            _size.Value = Math.Clamp((decimal)Result.Size, _size.Minimum, _size.Maximum);
            _thick.Value = Math.Clamp((decimal)Result.Thickness, _thick.Minimum, _thick.Maximum);
            _gap.Value = Math.Clamp(Result.Gap, _gap.Minimum, _gap.Maximum);
            _outline.Value = Math.Clamp(Result.Outline, _outline.Minimum, _outline.Maximum);
            _alpha.Value = Math.Clamp(Result.Alpha, _alpha.Minimum, _alpha.Maximum);
            _dot.Checked = Result.Dot;
            _tstyle.Checked = Result.T;
            _colour.BackColor = Color.FromArgb(Result.R, Result.G, Result.B);
            _loading = false;
            Sync();
        }

        private void PickColour(object? sender, EventArgs e)
        {
            using var dlg = new ColorDialog
            {
                Color = _colour.BackColor,
                FullOpen = true,
                AnyColor = true,
            };
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _colour.BackColor = dlg.Color;
                Sync();
            }
        }

        private void PasteFromClipboard(object? sender, EventArgs e)
        {
            bool pl = Localization.IsPolish;
            string text;
            try { text = Clipboard.GetText(); }
            catch { text = ""; }

            var parsed = CrosshairDef.Parse(text);
            if (parsed == null)
            {
                Toast.Show(this, pl
                    ? "W schowku nie ma komend cl_crosshair"
                    : "No cl_crosshair commands in the clipboard", ToastKind.Info);
                return;
            }

            // Keep whatever name the user already typed; the paste only carries settings.
            parsed.Name = Result.Name;
            parsed.Author = Result.Author;
            Result = parsed;
            LoadFromResult();

            Toast.Show(this, pl ? "Wczytano celownik ze schowka" : "Crosshair loaded from clipboard",
                       ToastKind.Success);
        }

        private void Save(object? sender, EventArgs e)
        {
            Sync();

            if (Result.Name.Length == 0)
            {
                Toast.Show(this, Localization.IsPolish ? "Podaj nazwę" : "Enter a name", ToastKind.Info);
                _name.Focus();
                return;
            }

            if (Result.Author.Length == 0)
                Result.Author = Localization.IsPolish ? "własny" : "custom";

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
