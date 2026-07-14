using System.Drawing;
using System.Windows.Forms;
using RLHub2.Controls;
using RLHub2.Helpers;

namespace RLHub2
{
    partial class SettingsPage
    {
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
        private ComboBox cmbAccount;
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
            cmbAccount = new ComboBox();
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
            this.Padding = new Padding(24);
            this.AutoScroll = true;

            lblTitle.Text = "SETTINGS";
            lblTitle.AutoSize = true;
            lblTitle.ForeColor = Theme.TextPrimary;
            lblTitle.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
            lblTitle.Location = new Point(24, 24);

            // ===== LANGUAGE CARD =====
            langPanel.Location = new Point(24, 84);
            langPanel.Size = new Size(460, 134);
            langPanel.BackColor = panelColor;

            lblLanguage.Text = "LANGUAGE";
            lblLanguage.AutoSize = true;
            lblLanguage.ForeColor = Theme.AccentSoft;
            lblLanguage.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblLanguage.Location = new Point(20, 16);

            lblLanguageHint.Text = "Language of the whole application";
            lblLanguageHint.AutoSize = true;
            lblLanguageHint.ForeColor = Theme.TextMuted;
            lblLanguageHint.Font = new Font("Segoe UI", 9.5F);
            lblLanguageHint.Location = new Point(20, 44);

            segLanguage.Location = new Point(20, 74);
            segLanguage.Size = new Size(280, 46);

            langPanel.Controls.Add(lblLanguage);
            langPanel.Controls.Add(lblLanguageHint);
            langPanel.Controls.Add(segLanguage);

            // ===== THEME CARD =====
            themePanel.Location = new Point(24, 234);
            themePanel.Size = new Size(460, 134);
            themePanel.BackColor = panelColor;

            lblTheme.Text = "THEME";
            lblTheme.AutoSize = true;
            lblTheme.ForeColor = Theme.AccentSoft;
            lblTheme.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblTheme.Location = new Point(20, 16);

            lblThemeHint.Text = "Light or dark appearance";
            lblThemeHint.AutoSize = true;
            lblThemeHint.ForeColor = Theme.TextMuted;
            lblThemeHint.Font = new Font("Segoe UI", 9.5F);
            lblThemeHint.Location = new Point(20, 44);

            segTheme.Location = new Point(20, 74);
            segTheme.Size = new Size(280, 46);

            themePanel.Controls.Add(lblTheme);
            themePanel.Controls.Add(lblThemeHint);
            themePanel.Controls.Add(segTheme);

            // ===== ACCENT CARD =====
            accentPanel.Location = new Point(24, 384);
            accentPanel.Size = new Size(460, 134);
            accentPanel.BackColor = panelColor;

            lblAccent.Text = "ACCENT COLOR";
            lblAccent.AutoSize = true;
            lblAccent.ForeColor = Theme.AccentSoft;
            lblAccent.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblAccent.Location = new Point(20, 16);

            lblAccentHint.Text = "Pick the highlight color";
            lblAccentHint.AutoSize = true;
            lblAccentHint.ForeColor = Theme.TextMuted;
            lblAccentHint.Font = new Font("Segoe UI", 9.5F);
            lblAccentHint.Location = new Point(20, 44);

            accentPicker.Location = new Point(18, 74);

            accentPanel.Controls.Add(lblAccent);
            accentPanel.Controls.Add(lblAccentHint);
            accentPanel.Controls.Add(accentPicker);

            // ===== TRACKER KEY CARD =====
            keyPanel.Location = new Point(24, 534);
            keyPanel.Size = new Size(460, 220);
            keyPanel.BackColor = panelColor;

            lblKey.Text = "TRACKER.GG API KEY";
            lblKey.AutoSize = true;
            lblKey.ForeColor = Theme.AccentSoft;
            lblKey.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblKey.Location = new Point(20, 16);

            lblKeyHint.Text = "Get a free key at tracker.gg/developers, then paste it here for real profiles";
            lblKeyHint.MaximumSize = new Size(420, 0);
            lblKeyHint.AutoSize = true;
            lblKeyHint.ForeColor = Theme.TextMuted;
            lblKeyHint.Font = new Font("Segoe UI", 9.5F);
            lblKeyHint.Location = new Point(20, 44);

            txtKey.Location = new Point(20, 90);
            txtKey.Width = 300;
            txtKey.BackColor = inputBg;
            txtKey.ForeColor = Theme.TextPrimary;
            txtKey.BorderStyle = BorderStyle.FixedSingle;
            txtKey.Font = new Font("Segoe UI", 11F);

            lblNick.Text = "PLAYER NICK";
            lblNick.AutoSize = true;
            lblNick.ForeColor = Theme.TextMuted;
            lblNick.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblNick.Location = new Point(20, 126);

            cmbAccount.Location = new Point(20, 148);
            cmbAccount.Width = 300;
            cmbAccount.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbAccount.FlatStyle = FlatStyle.Flat;
            cmbAccount.BackColor = inputBg;
            cmbAccount.ForeColor = Theme.TextPrimary;
            cmbAccount.Font = new Font("Segoe UI", 11F);

            btnSaveKey.Location = new Point(330, 147);
            btnSaveKey.Size = new Size(110, 28);
            btnSaveKey.Text = "SAVE";
            btnSaveKey.FlatStyle = FlatStyle.Flat;
            btnSaveKey.FlatAppearance.BorderSize = 0;
            btnSaveKey.ForeColor = Color.White;
            btnSaveKey.BackColor = Theme.Accent;
            btnSaveKey.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnSaveKey.Cursor = Cursors.Hand;

            keyPanel.Controls.Add(lblKey);
            keyPanel.Controls.Add(lblKeyHint);
            keyPanel.Controls.Add(txtKey);
            keyPanel.Controls.Add(lblNick);
            keyPanel.Controls.Add(cmbAccount);
            keyPanel.Controls.Add(btnSaveKey);

            // ===== BALLCHASING KEY CARD =====
            bcPanel.Location = new Point(24, 774);
            bcPanel.Size = new Size(460, 224);
            bcPanel.BackColor = panelColor;

            lblBc.Text = "BALLCHASING API KEY";
            lblBc.AutoSize = true;
            lblBc.ForeColor = Theme.AccentSoft;
            lblBc.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblBc.Location = new Point(20, 16);

            lblBcHint.Text = "Free key at ballchasing.com/upload → Settings. Enables real matches, ranks and auto MMR.";
            lblBcHint.MaximumSize = new Size(420, 0);
            lblBcHint.AutoSize = true;
            lblBcHint.ForeColor = Theme.TextMuted;
            lblBcHint.Font = new Font("Segoe UI", 9.5F);
            lblBcHint.Location = new Point(20, 44);

            txtBcKey.Location = new Point(20, 96);
            txtBcKey.Width = 300;
            txtBcKey.BackColor = inputBg;
            txtBcKey.ForeColor = Theme.TextPrimary;
            txtBcKey.BorderStyle = BorderStyle.FixedSingle;
            txtBcKey.Font = new Font("Segoe UI", 11F);
            txtBcKey.UseSystemPasswordChar = true;

            btnTestBc.Location = new Point(330, 95);
            btnTestBc.Size = new Size(110, 28);
            btnTestBc.Text = "TEST";
            btnTestBc.FlatStyle = FlatStyle.Flat;
            btnTestBc.FlatAppearance.BorderSize = 0;
            btnTestBc.ForeColor = Color.White;
            btnTestBc.BackColor = Theme.Accent;
            btnTestBc.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnTestBc.Cursor = Cursors.Hand;

            lblBcStatus.Text = "";
            lblBcStatus.AutoSize = true;
            lblBcStatus.MaximumSize = new Size(420, 0);
            lblBcStatus.ForeColor = Theme.TextSecondary;
            lblBcStatus.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblBcStatus.Location = new Point(20, 134);

            chkDeleteOld.Location = new Point(20, 162);
            chkDeleteOld.AutoSize = true;
            chkDeleteOld.MaximumSize = new Size(420, 0);
            chkDeleteOld.ForeColor = Theme.TextSecondary;
            chkDeleteOld.Font = new Font("Segoe UI", 9.5F);
            chkDeleteOld.Cursor = Cursors.Hand;

            bcPanel.Controls.Add(lblBc);
            bcPanel.Controls.Add(lblBcHint);
            bcPanel.Controls.Add(txtBcKey);
            bcPanel.Controls.Add(btnTestBc);
            bcPanel.Controls.Add(lblBcStatus);
            bcPanel.Controls.Add(chkDeleteOld);

            this.Controls.Add(bcPanel);
            this.Controls.Add(keyPanel);
            this.Controls.Add(accentPanel);
            this.Controls.Add(themePanel);
            this.Controls.Add(langPanel);
            this.Controls.Add(lblTitle);

            ResumeLayout(false);
            PerformLayout();
        }
    }
}
