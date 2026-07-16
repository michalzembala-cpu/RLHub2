using System.Drawing;
using System.Windows.Forms;
using RLHub2.Controls;

namespace RLHub2
{
    partial class DashboardShell
    {
        private Panel sidebar;
        private Panel header;
        private FlowLayoutPanel navPanel;
        private Label lblSecMain;
        private Label lblSecSocial;
        private Panel panelContent;

        private Label lblLogo;
        private Button btnToggle;

        private NavButton btnHome;
        private NavButton btnMMR;
        private NavButton btnRoad;
        private NavButton btnCoach;
        private NavButton btnSession;
        private NavButton btnCs2;
        private NavButton btnNews;
        private NavButton btnProfile;
        private NavButton btnRecords;
        private NavButton btnTournaments;
        private NavButton btnSeasons;
        private NavButton btnSettings;

        private void InitializeComponent()
        {
            sidebar = new Panel();
            header = new Panel();
            navPanel = new FlowLayoutPanel();
            lblSecMain = new Label();
            lblSecSocial = new Label();
            panelContent = new Panel();

            lblLogo = new Label();
            btnToggle = new Button();

            btnHome = new NavButton();
            btnMMR = new NavButton();
            btnRoad = new NavButton();
            btnCoach = new NavButton();
            btnSession = new NavButton();
            btnCs2 = new NavButton();
            btnNews = new NavButton();
            btnProfile = new NavButton();
            btnRecords = new NavButton();
            btnTournaments = new NavButton();
            btnSeasons = new NavButton();
            btnSettings = new NavButton();

            var sidebarColor = Color.FromArgb(18, 18, 38);

            SuspendLayout();

            // ===== FORM =====
            this.BackColor = Color.FromArgb(12, 12, 25);
            this.ClientSize = new Size(1100, 650);
            this.MinimumSize = new Size(900, 560);
            this.Text = "RL Hub 2";
            this.StartPosition = FormStartPosition.CenterScreen;

            // ===== SIDEBAR =====
            sidebar.Dock = DockStyle.Left;
            sidebar.Width = 220;
            sidebar.BackColor = sidebarColor;

            // ===== HEADER (logo + toggle) =====
            header.Dock = DockStyle.Top;
            header.Height = 72;
            header.BackColor = sidebarColor;

            btnToggle.Text = "☰"; // ☰
            btnToggle.Location = new Point(14, 18);
            btnToggle.Size = new Size(38, 38);
            btnToggle.FlatStyle = FlatStyle.Flat;
            btnToggle.FlatAppearance.BorderSize = 0;
            btnToggle.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 40, 70);
            btnToggle.ForeColor = Color.White;
            btnToggle.BackColor = sidebarColor;
            btnToggle.Font = new Font("Segoe UI Symbol", 14F, FontStyle.Bold);
            btnToggle.Cursor = Cursors.Hand;

            lblLogo.Text = "RL HUB";
            lblLogo.Location = new Point(62, 22);
            lblLogo.AutoSize = true;
            lblLogo.ForeColor = Color.FromArgb(150, 110, 255);
            lblLogo.Font = new Font("Segoe UI", 15F, FontStyle.Bold);

            header.Controls.Add(lblLogo);
            header.Controls.Add(btnToggle);

            // ===== NAV PANEL (grouped sections, scrollable) =====
            navPanel.Dock = DockStyle.Fill;
            navPanel.BackColor = sidebarColor;
            navPanel.Padding = new Padding(0, 6, 0, 6);
            navPanel.FlowDirection = FlowDirection.TopDown;
            navPanel.WrapContents = false;
            navPanel.AutoScroll = true;

            ConfigureNav(btnHome, "Home", "\U0001F3E0");              // 🏠
            ConfigureNav(btnMMR, "MMR Tracker", "\U0001F4C8");        // 📈
            ConfigureNav(btnRoad, "Road to SSL", "\U0001F680");       // 🚀
            ConfigureNav(btnCoach, "AI Coach", "\U0001F916");         // 🤖
            ConfigureNav(btnSession, "Session", "\U0001F534");       // 🔴
            ConfigureNav(btnCs2, "CS2", "\U0001F52B");               // 🔫
            ConfigureNav(btnProfile, "Profile", "\U0001F464");       // 👤
            ConfigureNav(btnRecords, "Records", "\U0001F3C5");       // 🏅
            ConfigureNav(btnTournaments, "Tournaments", "\U0001F3C6");// 🏆
            ConfigureNav(btnNews, "News", "\U0001F4F0");             // 📰
            ConfigureNav(btnSeasons, "Seasons", "\U0001F4C5");       // 📅
            ConfigureNav(btnSettings, "Settings", "⚙");         // ⚙

            ConfigureSection(lblSecMain, "MAIN");
            ConfigureSection(lblSecSocial, "COMMUNITY");

            navPanel.Controls.Add(lblSecMain);
            navPanel.Controls.Add(btnHome);
            navPanel.Controls.Add(btnMMR);
            navPanel.Controls.Add(btnRoad);
            navPanel.Controls.Add(btnCoach);
            navPanel.Controls.Add(btnSession);
            navPanel.Controls.Add(btnCs2);
            navPanel.Controls.Add(btnProfile);
            navPanel.Controls.Add(btnRecords);
            navPanel.Controls.Add(lblSecSocial);
            navPanel.Controls.Add(btnTournaments);
            navPanel.Controls.Add(btnNews);
            navPanel.Controls.Add(btnSeasons);
            navPanel.Controls.Add(btnSettings);

            // Header on top, nav fills the rest (add Fill control last).
            sidebar.Controls.Add(navPanel);
            sidebar.Controls.Add(header);

            // ===== CONTENT =====
            panelContent.Dock = DockStyle.Fill;
            panelContent.BackColor = Color.FromArgb(15, 15, 30);

            // ===== ASSEMBLE (sidebar docked left, content fills) =====
            this.Controls.Add(panelContent);
            this.Controls.Add(sidebar);

            ResumeLayout(false);
        }

        private void ConfigureNav(NavButton b, string text, string glyph)
        {
            b.Text = text;
            b.Glyph = glyph;
            b.Accent = Color.FromArgb(120, 60, 255);
            b.Height = 37;
            b.Margin = new Padding(8, 1, 8, 1);
        }

        private void ConfigureSection(Label l, string text)
        {
            l.Text = text;
            l.AutoSize = false;
            l.Height = 22;
            l.Margin = new Padding(18, 6, 8, 0);
            l.TextAlign = ContentAlignment.MiddleLeft;
            l.ForeColor = Color.FromArgb(120, 130, 165);
            l.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        }
    }
}
