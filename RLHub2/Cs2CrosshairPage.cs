using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using RLHub2.Helpers;
using RLHub2.Models;
using RLHub2.Services;

namespace RLHub2
{
    // A library of crosshairs to copy into CS2, plus your own.
    //
    // Each is copied as ONE line of semicolon-separated cl_crosshair* commands: the CS2 console
    // is a single-line input, so a pasted multi-line block only ever runs its first line. That's
    // why the earlier multi-line copy looked like it did nothing.
    //
    // Still no CSGO-xxxxx share codes — I can't verify that encoder against a real client, and a
    // wrong code is worse than no code. The commands are exact and I can preview them honestly.
    //
    // The pro presets use those players' publicly shared settings; nudge them to taste.
    public class Cs2CrosshairPage : UserControl
    {
        private static readonly CrosshairDef[] Presets =
        {
            new() { Name = "s1mple",  Author = "Oleksandr Kostyljev", Size = 2, Thickness = 0, Gap = -3, Outline = 1, R = 0,   G = 255, B = 0 },
            new() { Name = "ZywOo",   Author = "Mathieu Herbaut",     Size = 2, Thickness = 1, Gap = -2, Outline = 1, R = 0,   G = 255, B = 255 },
            new() { Name = "NiKo",    Author = "Nikola Kovač",        Size = 1, Thickness = 1, Gap = -3, Dot = true,  Outline = 1, R = 0, G = 255, B = 0 },
            new() { Name = "m0NESY",  Author = "Ilya Osipov",         Size = 3, Thickness = 1, Gap = -3, Outline = 1, R = 0,   G = 255, B = 0 },
            new() { Name = "donk",    Author = "Danil Kryšković",     Size = 1, Thickness = 1, Gap = -1, Outline = 1, R = 0,   G = 255, B = 255 },
            new() { Name = "dev1ce",  Author = "Nicolai Reedtz",      Size = 3, Thickness = 1, Gap = -2, Outline = 0, R = 0,   G = 255, B = 0 },
            new() { Name = "Kropka",  Author = "styl minimalny",      Size = 1, Thickness = 1, Gap = -4, Dot = true, Outline = 1, R = 0, G = 255, B = 0 },
            new() { Name = "T-style", Author = "bez górnej linii",    Size = 4, Thickness = 1, Gap = 0,  T = true, Outline = 1, R = 255, G = 200, B = 0 },
            new() { Name = "Klasyk",  Author = "duży, biały",         Size = 5, Thickness = 1, Gap = 1,  Outline = 1, R = 255, G = 255, B = 255 },
        };

        private readonly CrosshairStore _store = new();
        private readonly FlowLayoutPanel _flow;

        public Cs2CrosshairPage()
        {
            BackColor = Theme.PageBg;
            Dock = DockStyle.Fill;

            bool pl = Localization.IsPolish;

            var title = new Label
            {
                Text = pl ? "CELOWNIKI" : "CROSSHAIRS",
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(24, 8, 0, 0),
                ForeColor = Theme.TextPrimary,
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
            };
            var hint = new Label
            {
                Text = pl
                    ? "KOPIUJ wkleja się jedną linią do konsoli CS2 (~) — albo .CFG i w konsoli: exec crosshair"
                    : "COPY pastes as one line into the CS2 console (~) — or .CFG, then: exec crosshair",
                Dock = DockStyle.Top,
                Height = 26,
                Padding = new Padding(24, 0, 0, 0),
                ForeColor = Theme.TextMuted,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            };

            _flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 8, 20, 20),
                AutoScroll = true,
                BackColor = Theme.PageBg,
            };

            Controls.Add(_flow);
            Controls.Add(hint);
            Controls.Add(title);

            Rebuild();
        }

        // ---------- library ----------

        private void Rebuild()
        {
            _flow.SuspendLayout();
            foreach (Control c in _flow.Controls.Cast<Control>().ToList()) c.Dispose();
            _flow.Controls.Clear();

            _flow.Controls.Add(new AddCard(AddNew));

            // The user's own first — they're the ones being worked on.
            var mine = _store.Load();
            for (int i = 0; i < mine.Count; i++)
            {
                int index = i;   // capture: the store edits by position
                _flow.Controls.Add(new CrosshairCard(mine[i],
                    onEdit: () => EditExisting(index),
                    onDelete: () => DeleteExisting(index)));
            }

            foreach (var p in Presets)
                _flow.Controls.Add(new CrosshairCard(p, null, null));

            _flow.ResumeLayout();
        }

        private void AddNew()
        {
            using var dlg = new Cs2CrosshairDialog();
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
            _store.Add(dlg.Result);
            Rebuild();
        }

        private void EditExisting(int index)
        {
            var mine = _store.Load();
            if (index < 0 || index >= mine.Count) return;

            using var dlg = new Cs2CrosshairDialog(mine[index]);
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
            _store.Replace(index, dlg.Result);
            Rebuild();
        }

        private void DeleteExisting(int index)
        {
            var mine = _store.Load();
            if (index < 0 || index >= mine.Count) return;

            bool pl = Localization.IsPolish;
            var ok = MessageBox.Show(FindForm()!,
                pl ? $"Usunąć celownik „{mine[index].Name}”?" : $"Delete the crosshair \"{mine[index].Name}\"?",
                pl ? "Usuwanie" : "Delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (ok != DialogResult.Yes) return;

            _store.RemoveAt(index);
            Rebuild();
        }

        // ---------- "add your own" tile ----------

        private sealed class AddCard : Panel
        {
            private readonly Action _onClick;
            private bool _hot;

            public AddCard(Action onClick)
            {
                _onClick = onClick;
                Size = new Size(330, 160);
                Margin = new Padding(0, 0, 16, 16);
                DoubleBuffered = true;
                BackColor = Color.Transparent;
                Cursor = Cursors.Hand;

                Click += (s, e) => _onClick();
                MouseEnter += (s, e) => { _hot = true; Invalidate(); };
                MouseLeave += (s, e) => { _hot = false; Invalidate(); };
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var rect = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var p = Round(rect, 16))
                {
                    using var bg = new SolidBrush(Color.FromArgb(_hot ? 40 : 24, Theme.Accent));
                    g.FillPath(bg, p);
                    using var pen = new Pen(Color.FromArgb(_hot ? 180 : 90, Theme.Accent), 1.6f)
                    { DashStyle = DashStyle.Dash };
                    g.DrawPath(pen, p);
                }

                using var center = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                };
                using (var pf = new Font("Segoe UI", 34F, FontStyle.Bold))
                using (var pb = new SolidBrush(Theme.Accent))
                    g.DrawString("+", pf, pb, new RectangleF(0, 24, Width, 60), center);
                using (var tf = new Font("Segoe UI", 11F, FontStyle.Bold))
                using (var tb = new SolidBrush(Theme.TextPrimary))
                    g.DrawString(Localization.IsPolish ? "WŁASNY CELOWNIK" : "YOUR OWN CROSSHAIR",
                                 tf, tb, new RectangleF(0, 86, Width, 24), center);
                using (var sf = new Font("Segoe UI", 8.5F))
                using (var sb = new SolidBrush(Theme.TextMuted))
                    g.DrawString(Localization.IsPolish ? "ustaw ręcznie albo wklej ze schowka" : "set it by hand or paste from clipboard",
                                 sf, sb, new RectangleF(0, 110, Width, 22), center);
            }

            private static GraphicsPath Round(Rectangle r, int radius)
            {
                int d = radius * 2;
                var p = new GraphicsPath();
                p.AddArc(r.X, r.Y, d, d, 180, 90);
                p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
                p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
                p.CloseAllFigures();
                return p;
            }
        }

        // ---------- one crosshair card ----------

        private sealed class CrosshairCard : Panel
        {
            private readonly Bitmap _preview;
            private readonly CrosshairDef _def;

            public CrosshairCard(CrosshairDef def, Action? onEdit, Action? onDelete)
            {
                _def = def;
                _preview = CrosshairRenderer.Render(def);

                Size = new Size(330, 160);
                Margin = new Padding(0, 0, 16, 16);
                DoubleBuffered = true;
                BackColor = Color.Transparent;

                bool pl = Localization.IsPolish;

                // Button row along the bottom, right-aligned.
                int bx = Width - 16;
                var copy = Flat(pl ? "KOPIUJ" : "COPY", 96, Theme.Accent, Color.Black);
                bx -= 96; copy.Location = new Point(bx, Height - 30 - 16);
                copy.Click += (s, e) => CopyCommands();
                Controls.Add(copy);

                var cfg = Flat(".CFG", 70, Theme.Surface, Theme.TextPrimary);
                bx -= 8 + 70; cfg.Location = new Point(bx, Height - 30 - 16);
                cfg.Click += (s, e) => WriteCfg();
                Controls.Add(cfg);

                if (onEdit != null)
                {
                    var edit = Flat(pl ? "EDYTUJ" : "EDIT", 84, Theme.Surface, Theme.TextPrimary);
                    bx -= 8 + 84; edit.Location = new Point(bx, Height - 30 - 16);
                    edit.Click += (s, e) => onEdit();
                    Controls.Add(edit);
                }

                if (onDelete != null)
                {
                    var del = new Button
                    {
                        Text = "✕",
                        Size = new Size(26, 26),
                        Location = new Point(Width - 26 - 12, 12),
                        FlatStyle = FlatStyle.Flat,
                        BackColor = Theme.Surface,
                        ForeColor = Theme.TextMuted,
                        Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                        Cursor = Cursors.Hand,
                    };
                    del.FlatAppearance.BorderSize = 0;
                    del.Click += (s, e) => onDelete();
                    Controls.Add(del);
                }
            }

            private static Button Flat(string text, int w, Color back, Color fore)
            {
                var b = new Button
                {
                    Text = text,
                    Size = new Size(w, 30),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = back,
                    ForeColor = fore,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                };
                b.FlatAppearance.BorderSize = 0;
                return b;
            }

            private void CopyCommands()
            {
                bool pl = Localization.IsPolish;
                try
                {
                    Clipboard.SetText(_def.Commands());
                    Toast.Show(FindForm() is Control c ? c : this,
                        pl ? $"Skopiowano: {_def.Name} — wklej w konsoli i Enter"
                           : $"Copied {_def.Name} — paste into the console and hit Enter",
                        ToastKind.Success);
                }
                catch
                {
                    Toast.Show(this, pl ? "Nie udało się skopiować" : "Copy failed", ToastKind.Info);
                }
            }

            // The guaranteed route: a .cfg in CS2's own cfg folder, run with `exec crosshair`.
            // No console line-length or paste behaviour to worry about.
            private void WriteCfg()
            {
                bool pl = Localization.IsPolish;
                var host = FindForm() is Control c ? c : this;

                var csgo = Cs2Install.CsgoDir();
                if (csgo == null)
                {
                    Toast.Show(host, pl ? "Nie znaleziono instalacji CS2" : "CS2 install not found", ToastKind.Info);
                    return;
                }

                try
                {
                    var dir = Path.Combine(csgo, "cfg");
                    Directory.CreateDirectory(dir);
                    File.WriteAllText(Path.Combine(dir, "crosshair.cfg"), _def.ConfigFile());
                    Toast.Show(host, pl ? "Zapisano — w konsoli: exec crosshair"
                                        : "Saved — in the console: exec crosshair", ToastKind.Success);
                }
                catch (Exception ex)
                {
                    Toast.Show(host, (pl ? "Zapis nieudany: " : "Save failed: ") + ex.Message, ToastKind.Info);
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var rect = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var p = Round(rect, 16))
                {
                    using var bg = new LinearGradientBrush(rect, Theme.CardTop, Theme.CardBottom, 90f);
                    g.FillPath(bg, p);
                    using var pen = new Pen(Color.FromArgb(60, Theme.Accent), 1f);
                    g.DrawPath(pen, p);
                }

                var pv = new Rectangle(16, 16, 96, 96);
                using (var pp = Round(pv, 10))
                using (var pb = new SolidBrush(CrosshairRenderer.Backdrop))
                    g.FillPath(pb, pp);
                g.DrawImage(_preview, pv);

                using var nameFont = new Font("Segoe UI", 13f, FontStyle.Bold);
                using var authFont = new Font("Segoe UI", 8.5f);
                using var sumFont = new Font("Segoe UI", 8.5f);
                using var tp = new SolidBrush(Theme.TextPrimary);
                using var tm = new SolidBrush(Theme.TextMuted);
                using var ts = new SolidBrush(Theme.TextSecondary);

                // Leave room for the ✕ so a long name never runs under it.
                int tx = pv.Right + 16;
                int tw = Width - tx - (_def.Custom ? 48 : 16);
                using var clip = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };

                g.DrawString(_def.Name, nameFont, tp, new RectangleF(tx, 18, tw, 24), clip);
                g.DrawString(_def.Author, authFont, tm, new RectangleF(tx, 44, tw, 18), clip);
                g.DrawString(_def.Summary(), sumFont, ts, new RectangleF(tx, 64, tw, 18), clip);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) _preview.Dispose();
                base.Dispose(disposing);
            }

            private static GraphicsPath Round(Rectangle r, int radius)
            {
                int d = radius * 2;
                var p = new GraphicsPath();
                p.AddArc(r.X, r.Y, d, d, 180, 90);
                p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
                p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
                p.CloseAllFigures();
                return p;
            }
        }
    }
}
