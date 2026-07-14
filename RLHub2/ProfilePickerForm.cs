using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using RLHub2.Helpers;
using RLHub2.Models;
using RLHub2.Services;

namespace RLHub2
{
    // "Who's playing?" — a Steam Big Picture style profile picker over a blurred arena.
    // Shown at startup (when there is more than one account) and from Settings.
    public class ProfilePickerForm : Form
    {
        private const int Tile = 128;
        private const int Gap = 24;

        private readonly SettingsStore _store = new();
        private List<Account> _accounts;
        private readonly List<Rectangle> _hits = new();   // tiles + the trailing "add" tile
        private int _hover = -1;

        private Image? _blurred;

        public string? SelectedName { get; private set; }

        public ProfilePickerForm()
        {
            _accounts = _store.LoadAccounts();

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1000, 660);
            BackColor = Color.FromArgb(10, 12, 24);
            DoubleBuffered = true;
            KeyPreview = true;
            SetStyle(ControlStyles.ResizeRedraw, true);

            MouseMove += OnMove;
            MouseLeave += (s, e) => { _hover = -1; Invalidate(); };
            MouseClick += OnClick;
            KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RoundCorners(20);
        }

        // DPI scaling resizes the form after the handle exists, so the region has to follow —
        // otherwise the bottom strip of the form falls outside it and paints black.
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (IsHandleCreated) RoundCorners(20);
        }

        private void RoundCorners(int radius)
        {
            if (ClientSize.Width < 4 || ClientSize.Height < 4) return;
            using var path = new GraphicsPath();
            int d = radius * 2;
            var r = ClientRectangle;
            path.AddArc(r.Left, r.Top, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            path.CloseAllFigures();
            Region = new Region(path);
        }

        // The Big Picture look is really just a heavily out-of-focus wallpaper: downscale the
        // arena to a few dozen pixels and blow it back up — bilinear filtering does the blur.
        private Image? Blurred()
        {
            if (_blurred != null) return _blurred;
            var src = ArenaBackground.Load("stadion1.jpg");
            if (src == null) return null;

            const int small = 40;
            int h = Math.Max(1, small * src.Height / src.Width);
            using var tiny = new Bitmap(small, h);
            using (var g = Graphics.FromImage(tiny))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                g.DrawImage(src, 0, 0, small, h);
            }
            _blurred = new Bitmap(tiny, tiny.Width * 4, tiny.Height * 4);
            return _blurred;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBilinear;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            g.Clear(Color.FromArgb(10, 12, 24));

            var bg = Blurred();
            if (bg != null)
            {
                float ir = bg.Width / (float)bg.Height, cr = Width / (float)Height;
                Rectangle dst = ir > cr
                    ? new Rectangle((Width - (int)(Height * ir)) / 2, 0, (int)(Height * ir), Height)
                    : new Rectangle(0, (Height - (int)(Width / ir)) / 2, Width, (int)(Width / ir));
                g.DrawImage(bg, dst);
            }
            using (var dim = new SolidBrush(Color.FromArgb(170, 8, 10, 20)))
                g.FillRectangle(dim, 0, 0, Width, Height);

            // ===== header =====
            using var titleFont = new Font("Segoe UI", 26f, FontStyle.Bold);
            using var subFont = new Font("Segoe UI", 11.5f);
            using var white = new SolidBrush(Color.White);
            using var muted = new SolidBrush(Color.FromArgb(190, 200, 215));

            string title = Localization.IsPolish ? "Kto gra?" : "Who's playing?";
            string sub = Localization.IsPolish
                ? "Wybierz profil lub dodaj nowe konto"
                : "Pick a profile or add another account";

            var ts = g.MeasureString(title, titleFont);
            var ss = g.MeasureString(sub, subFont);
            float titleY = Height * 0.34f;
            g.DrawString(title, titleFont, white, (Width - ts.Width) / 2, titleY);
            g.DrawString(sub, subFont, muted, (Width - ss.Width) / 2, titleY + ts.Height + 6);

            // ===== tiles =====
            LayoutTiles(titleY + ts.Height + ss.Height + 34);

            string active = Accounts.ActiveName;
            for (int i = 0; i < _accounts.Count; i++)
                DrawProfile(g, _hits[i], _accounts[i], i == _hover, _accounts[i].Name == active);

            DrawAdd(g, _hits[^1], _hover == _accounts.Count);

            // hint at the bottom — drag & drop is not discoverable otherwise
            using var hintFont = new Font("Segoe UI", 9f);
            using var hintBrush = new SolidBrush(Color.FromArgb(170, 255, 255, 255));
            string hint = Localization.IsPolish
                ? "Przeciągnij obrazek na kafelek (lub kliknij prawym), by ustawić awatar   •   Esc — anuluj"
                : "Drop an image on a tile (or right-click it) to set an avatar   •   Esc — cancel";
            var hs = g.MeasureString(hint, hintFont);
            g.DrawString(hint, hintFont, hintBrush, (Width - hs.Width) / 2, Height - hs.Height - 18);
        }

        private void LayoutTiles(float top)
        {
            _hits.Clear();
            int count = _accounts.Count + 1;                 // +1 for the add tile
            int totalW = count * Tile + (count - 1) * Gap;
            int x = (Width - totalW) / 2;
            int y = (int)top;
            for (int i = 0; i < count; i++)
                _hits.Add(new Rectangle(x + i * (Tile + Gap), y, Tile, Tile));
        }

        private static void DrawProfile(Graphics g, Rectangle r, Account acc, bool hover, bool active)
        {
            var img = Avatars.Load(acc.Name);
            if (img != null)
            {
                g.DrawImage(img, r);
            }
            else
            {
                var c = Avatars.ColorFor(acc.Name);
                using var grad = new LinearGradientBrush(r, ControlPaint.Light(c, 0.15f), ControlPaint.Dark(c, 0.25f), 60f);
                g.FillRectangle(grad, r);

                using var f = new Font("Segoe UI", 44f, FontStyle.Bold);
                using var b = new SolidBrush(Color.FromArgb(235, 255, 255, 255));
                var sz = g.MeasureString(Avatars.Initials(acc.Name), f);
                g.DrawString(Avatars.Initials(acc.Name), f, b,
                    r.X + (r.Width - sz.Width) / 2, r.Y + (r.Height - sz.Height) / 2);
            }

            // selection ring: bright on hover, softer for the currently active profile
            if (hover || active)
            {
                using var pen = new Pen(hover ? Color.White : Color.FromArgb(150, 255, 255, 255), 3f);
                g.DrawRectangle(pen, Rectangle.Inflate(r, 2, 2));
            }

            // Steam can label only the focused tile because every profile has a picture. Ours may
            // not, and two accounts can share an initial — so always show the name, dimmed.
            bool lit = hover || active;
            using (var f = new Font("Segoe UI", 10.5f, lit ? FontStyle.Bold : FontStyle.Regular))
            using (var b = new SolidBrush(lit ? Color.White : Color.FromArgb(150, 165, 185)))
            {
                var sz = g.MeasureString(acc.Name, f);
                g.DrawString(acc.Name, f, b, r.X + (r.Width - sz.Width) / 2, r.Bottom + 12);
            }
        }

        private static void DrawAdd(Graphics g, Rectangle r, bool hover)
        {
            // Steam's add tile is smaller than a profile — it isn't a person.
            int inset = 16;
            var box = Rectangle.Inflate(r, -inset, -inset);

            using (var b = new SolidBrush(hover ? Color.FromArgb(35, 38, 52) : Color.FromArgb(18, 20, 30)))
                g.FillRectangle(b, box);

            using var pen = new Pen(hover ? Color.White : Color.FromArgb(210, 220, 235), 5f);
            int cx = box.X + box.Width / 2, cy = box.Y + box.Height / 2, arm = box.Width / 5;
            g.DrawLine(pen, cx - arm, cy, cx + arm, cy);
            g.DrawLine(pen, cx, cy - arm, cx, cy + arm);

            if (hover)
            {
                using var ring = new Pen(Color.White, 3f);
                g.DrawRectangle(ring, Rectangle.Inflate(box, 2, 2));
            }
        }

        private void OnMove(object? sender, MouseEventArgs e)
        {
            int h = _hits.FindIndex(r => r.Contains(e.Location));
            if (h == _hover) return;
            _hover = h;
            Cursor = h >= 0 ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }

        private void OnClick(object? sender, MouseEventArgs e)
        {
            int i = _hits.FindIndex(r => r.Contains(e.Location));
            if (i < 0) return;

            if (i == _accounts.Count) { AddAccount(); return; }

            if (e.Button == MouseButtons.Right) { PickAvatar(_accounts[i].Name); return; }

            SelectedName = _accounts[i].Name;
            Accounts.SetActive(SelectedName);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void PickAvatar(string account)
        {
            using var dlg = new OpenFileDialog
            {
                Title = Localization.IsPolish ? "Wybierz awatar" : "Pick an avatar",
                Filter = "Obrazy|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*",
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            Avatars.Set(account, dlg.FileName);
            Invalidate();
        }

        private void AddAccount()
        {
            using var dlg = new AccountDialog();
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            var name = dlg.AccountName;
            if (string.IsNullOrWhiteSpace(name)) return;
            if (_accounts.Any(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase))) return;

            _accounts.Add(new Account { Name = name, Aliases = dlg.Aliases });
            _store.SaveAccounts(_accounts);
            _accounts = _store.LoadAccounts();
            Invalidate();
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true
                ? DragDropEffects.Copy : DragDropEffects.None;
        }

        protected override void OnDragDrop(DragEventArgs e)
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0) return;
            var p = PointToClient(new Point(e.X, e.Y));
            int i = _hits.FindIndex(r => r.Contains(p));
            if (i < 0 || i >= _accounts.Count) return;
            Avatars.Set(_accounts[i].Name, files[0]);
            Invalidate();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            AllowDrop = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _blurred?.Dispose();
            base.Dispose(disposing);
        }
    }

    // Small modal for adding an account: current in-game name + any old names it was renamed from.
    public class AccountDialog : Form
    {
        private readonly TextBox _name = new();
        private readonly TextBox _aliases = new();

        public string AccountName => _name.Text.Trim();
        public List<string> Aliases => _aliases.Text
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        public AccountDialog()
        {
            Text = Localization.IsPolish ? "Nowe konto" : "New account";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = MinimizeBox = false;
            ClientSize = new Size(420, 200);
            BackColor = Color.FromArgb(16, 18, 30);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10f);

            Label L(string t, int y)
            {
                var l = new Label { Text = t, AutoSize = true, Location = new Point(20, y), ForeColor = Color.FromArgb(180, 190, 210) };
                Controls.Add(l);
                return l;
            }
            void Input(TextBox t, int y)
            {
                t.Location = new Point(20, y);
                t.Size = new Size(380, 26);
                t.BackColor = Color.FromArgb(28, 31, 46);
                t.ForeColor = Color.White;
                t.BorderStyle = BorderStyle.FixedSingle;
                Controls.Add(t);
            }

            L(Localization.IsPolish ? "Nazwa w grze" : "In-game name", 18);
            Input(_name, 40);
            L(Localization.IsPolish ? "Stare nazwy (po przecinku, opcjonalnie)" : "Old names (comma separated, optional)", 78);
            Input(_aliases, 100);

            var ok = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(290, 145),
                Size = new Size(110, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Accent,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
            };
            ok.FlatAppearance.BorderSize = 0;

            var cancel = new Button
            {
                Text = Localization.IsPolish ? "Anuluj" : "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(170, 145),
                Size = new Size(110, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 44, 60),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
            };
            cancel.FlatAppearance.BorderSize = 0;

            Controls.Add(ok);
            Controls.Add(cancel);
            AcceptButton = ok;
            CancelButton = cancel;
        }
    }
}
