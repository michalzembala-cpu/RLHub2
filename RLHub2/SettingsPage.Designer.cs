using System.Drawing;
using System.Windows.Forms;
using RLHub2.Controls;
using RLHub2.Helpers;

namespace RLHub2
{
    partial class SettingsPage
    {
        private FlowLayoutPanel flow;
        private Label lblTitle;

        private Panel langPanel;
        private Label lblLanguage;
        private Label lblLanguageHint;
        private SegmentedControl segLanguage;

        private Panel themePanel;
        private Label lblTheme;
        private Label lblThemeHint;
        private SegmentedControl segTheme;

        private Panel accentPanel;
        private Label lblAccent;
        private Label lblAccentHint;
        private AccentPicker accentPicker;

        private Panel keyPanel;
        private Label lblKey;
        private Label lblKeyHint;
        private TextBox txtKey;
        private Label lblNick;
        private Button btnProfile;
        private CheckBox chkAskProfile;
        private Button btnSaveKey;

        private Panel bcPanel;
        private Label lblBc;
        private Label lblBcHint;
        private TextBox txtBcKey;
        private Button btnTestBc;
        private Label lblBcStatus;
        private CheckBox chkDeleteOld;

        private void InitializeComponent()
        {
            flow = new FlowLayoutPanel();
            lblTitle = new Label();
            langPanel = new Panel();
            lblLanguage = new Label();
            lblLanguageHint = new Label();
            segLanguage = new SegmentedControl();
            themePanel = new Panel();
            lblTheme = new Label();
            lblThemeHint = new Label();
            segTheme = new SegmentedControl();
            accentPanel = new Panel();
            lblAccent = new Label();
            lblAccentHint = new Label();
            accentPicker = new AccentPicker();
            keyPanel = new Panel();
            lblKey = new Label();
            lblKeyHint = new Label();
            txtKey = new TextBox();
            lblNick = new Label();
            btnProfile = new Button();
            chkAskProfile = new CheckBox();
            btnSaveKey = new Button();
            bcPanel = new Panel();
            lblBc = new Label();
            lblBcHint = new Label();
            txtBcKey = new TextBox();
            btnTestBc = new Button();
            lblBcStatus = new Label();
            chkDeleteOld = new CheckBox();

            var pageColor = Theme.PageBg;
            var panelColor = Theme.Surface;
            var inputBg = Theme.SurfaceAlt;

            SuspendLayout();

            this.BackColor = pageColor;
            this.Dock = DockStyle.Fill;

            // Cards stack vertically and are stretched to the page width at runtime
            // (see SettingsPage.FitCards), so nothing is pinned to a fixed 460px column.
            flow.Dock = DockStyle.Fill;
            flow.BackColor = pageColor;
            flow.Padding = new Padding(24, 20, 24, 24);
            flow.FlowDirection = FlowDirection.TopDown;
            flow.WrapContents = false;
            flow.AutoScroll = true;

            lblTitle.Text = "SETTINGS";
            lblTitle.AutoSize = true;
            lblTitle.ForeColor = Theme.TextPrimary;
            lblTitle.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
            lblTitle.Margin = new Padding(2, 0, 0, 16);

            // ===== LANGUAGE =====
            Card(langPanel, panelColor, 134);
            SectionTitle(lblLanguage, "LANGUAGE");
            Hint(lblLanguageHint, "Language of the whole application");
            segLanguage.Location = new Point(20, 74);
            segLanguage.Size = new Size(280, 46);
            langPanel.Controls.AddRange(new Control[] { lblLanguage, lblLanguageHint, segLanguage });

            // ===== THEME =====
            Card(themePanel, panelColor, 134);
            SectionTitle(lblTheme, "THEME");
            Hint(lblThemeHint, "Light or dark appearance");
            segTheme.Location = new Point(20, 74);
            segTheme.Size = new Size(280, 46);
            themePanel.Controls.AddRange(new Control[] { lblTheme, lblThemeHint, segTheme });

            // ===== ACCENT =====
            Card(accentPanel, panelColor, 134);
            SectionTitle(lblAccent, "ACCENT COLOR");
            Hint(lblAccentHint, "Pick the highlight color");
            accentPicker.Location = new Point(18, 74);
            accentPanel.Controls.AddRange(new Control[] { lblAccent, lblAccentHint, accentPicker });

            // ===== TRACKER KEY + ACTIVE ACCOUNT =====
            Card(keyPanel, panelColor, 224);
            SectionTitle(lblKey, "TRACKER.GG API KEY");
            Hint(lblKeyHint, "Get a free key at tracker.gg/developers, then paste it here for real profiles");

            Input(txtKey, inputBg);
            txtKey.Location = new Point(20, 90);
            txtKey.Size = new Size(CardW - 40, 26);
            txtKey.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            lblNick.Text = "ACTIVE ACCOUNT";
            lblNick.AutoSize = true;
            lblNick.ForeColor = Theme.TextMuted;
            lblNick.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblNick.Location = new Point(20, 126);

            // The account is chosen on the Big-Picture-style picker, not in a combo box.
            btnProfile.Text = "PROFILE";
            btnProfile.Size = new Size(CardW - 20 - 130 - 10, 30);
            btnProfile.Location = new Point(20, 146);
            btnProfile.FlatStyle = FlatStyle.Flat;
            btnProfile.FlatAppearance.BorderSize = 1;
            btnProfile.FlatAppearance.BorderColor = Theme.Accent;
            btnProfile.BackColor = inputBg;
            btnProfile.ForeColor = Theme.TextPrimary;
            btnProfile.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnProfile.TextAlign = ContentAlignment.MiddleLeft;
            btnProfile.Padding = new Padding(10, 0, 0, 0);
            btnProfile.Cursor = Cursors.Hand;
            btnProfile.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            chkAskProfile.Location = new Point(20, 186);
            chkAskProfile.AutoSize = true;
            chkAskProfile.ForeColor = Theme.TextSecondary;
            chkAskProfile.Font = new Font("Segoe UI", 9.5F);
            chkAskProfile.Cursor = Cursors.Hand;

            AccentButton(btnSaveKey, "SAVE", 110);
            btnSaveKey.Location = new Point(CardW - 130, 146);
            btnSaveKey.Size = new Size(110, 30);
            btnSaveKey.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            keyPanel.Controls.AddRange(new Control[] { lblKey, lblKeyHint, txtKey, lblNick, btnProfile, chkAskProfile, btnSaveKey });

            // ===== BALLCHASING =====
            Card(bcPanel, panelColor, 224);
            SectionTitle(lblBc, "BALLCHASING API KEY");
            Hint(lblBcHint, "Free key at ballchasing.com/upload → Settings.");

            Input(txtBcKey, inputBg);
            txtBcKey.Location = new Point(20, 96);
            txtBcKey.Size = new Size(CardW - 20 - 130 - 10, 26);
            txtBcKey.UseSystemPasswordChar = true;
            txtBcKey.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            AccentButton(btnTestBc, "TEST", 110);
            btnTestBc.Location = new Point(CardW - 130, 95);
            btnTestBc.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            lblBcStatus.Text = "";
            lblBcStatus.AutoSize = true;
            lblBcStatus.ForeColor = Theme.TextSecondary;
            lblBcStatus.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblBcStatus.Location = new Point(20, 134);

            chkDeleteOld.Location = new Point(20, 162);
            chkDeleteOld.AutoSize = true;
            chkDeleteOld.ForeColor = Theme.TextSecondary;
            chkDeleteOld.Font = new Font("Segoe UI", 9.5F);
            chkDeleteOld.Cursor = Cursors.Hand;

            bcPanel.Controls.AddRange(new Control[] { lblBc, lblBcHint, txtBcKey, btnTestBc, lblBcStatus, chkDeleteOld });

            flow.Controls.AddRange(new Control[] { lblTitle, langPanel, themePanel, accentPanel, keyPanel, bcPanel });
            this.Controls.Add(flow);

            ResumeLayout(false);
        }

        // ===== small builders so every card looks the same =====

        // Design width; FitCards() stretches this to the real page width at runtime and the
        // anchored children keep their gaps to the card edges.
        private const int CardW = 600;

        private static void Card(Panel p, Color back, int height)
        {
            p.BackColor = back;
            p.Size = new Size(CardW, height);
            p.Margin = new Padding(0, 0, 0, 16);
        }

        private static void SectionTitle(Label l, string text)
        {
            l.Text = text;
            l.AutoSize = true;
            l.ForeColor = Theme.AccentSoft;
            l.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            l.Location = new Point(20, 16);
        }

        private static void Hint(Label l, string text)
        {
            l.Text = text;
            l.AutoSize = true;
            l.ForeColor = Theme.TextMuted;
            l.Font = new Font("Segoe UI", 9.5F);
            l.Location = new Point(20, 44);
        }

        private static void Input(TextBox t, Color back)
        {
            t.BackColor = back;
            t.ForeColor = Theme.TextPrimary;
            t.BorderStyle = BorderStyle.FixedSingle;
            t.Font = new Font("Segoe UI", 11F);
        }

        private static void AccentButton(Button b, string text, int width)
        {
            b.Text = text;
            b.Size = new Size(width, 28);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.ForeColor = Color.White;
            b.BackColor = Theme.Accent;
            b.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            b.Cursor = Cursors.Hand;
        }
    }
}
