using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using RLHub2.Helpers;

namespace RLHub2
{
    // Friendly crash window: shows what went wrong, lets the user copy the details
    // or open the log, and (for non-fatal errors) keep using the app.
    public class ErrorDialog : Form
    {
        private readonly string _details;

        public ErrorDialog(string details, bool fatal)
        {
            _details = details;
            bool pl = Localization.IsPolish;

            Text = "RL Hub 2";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(680, 460);
            BackColor = Theme.PageBg;
            ShowInTaskbar = true;
            DoubleBuffered = true;

            var lblTitle = new Label
            {
                Text = pl ? "⚠  Coś poszło nie tak" : "⚠  Something went wrong",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(24, 20)
            };

            var lblHint = new Label
            {
                Text = fatal
                    ? (pl ? "Aplikacja musi się zamknąć. Skopiuj szczegóły i wyślij je autorowi."
                          : "The app has to close. Copy the details and send them to the author.")
                    : (pl ? "Aplikacja może działać dalej. Skopiuj szczegóły i wyślij je autorowi."
                          : "You can keep using the app. Copy the details and send them to the author."),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Theme.TextSecondary,
                AutoSize = true,
                MaximumSize = new Size(620, 0),
                Location = new Point(24, 56)
            };

            var box = new TextBox
            {
                Text = details,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                WordWrap = false,
                BackColor = Theme.SurfaceAlt,
                ForeColor = Theme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9F),
                Location = new Point(24, 96),
                Size = new Size(632, 280)
            };

            var btnCopy = MakeButton(pl ? "KOPIUJ SZCZEGÓŁY" : "COPY DETAILS", Theme.Accent, 170);
            btnCopy.Location = new Point(24, 392);
            btnCopy.Click += (s, e) =>
            {
                try { Clipboard.SetText(_details); } catch { }
                btnCopy.Text = pl ? "SKOPIOWANO ✓" : "COPIED ✓";
            };

            var btnLog = MakeButton(pl ? "OTWÓRZ LOG" : "OPEN LOG", Theme.SurfaceAlt, 140);
            btnLog.ForeColor = Theme.TextPrimary;
            btnLog.Location = new Point(204, 392);
            btnLog.Click += (s, e) => ErrorReporter.OpenLog();

            var btnClose = MakeButton(
                fatal ? (pl ? "ZAMKNIJ APLIKACJĘ" : "CLOSE APP") : (pl ? "KONTYNUUJ" : "CONTINUE"),
                Theme.SurfaceAlt, 170);
            btnClose.ForeColor = Theme.TextPrimary;
            btnClose.Location = new Point(486, 392);
            btnClose.Click += (s, e) => Close();

            Controls.AddRange(new Control[] { lblTitle, lblHint, box, btnCopy, btnLog, btnClose });
            AcceptButton = btnClose;
        }

        private static Button MakeButton(string text, Color back, int width)
        {
            var b = new Button
            {
                Text = text,
                Size = new Size(width, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = back,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
    }
}
