using System.Drawing;
using System.Windows.Forms;
using RLHub2.Controls;
using RLHub2.Helpers;

namespace RLHub2
{
    partial class SessionPage
    {
        private TableLayoutPanel rootLayout;
        private Label lblTitle;
        private Label lblStatus;

        private TableLayoutPanel tilesRow;
        private StatTile tileRecord;
        private StatTile tileWinRate;
        private StatTile tileStreak;
        private StatTile tileMatches;

        private TableLayoutPanel avgRow;
        private StatTile tileGoals;
        private StatTile tileSaves;
        private StatTile tileAssists;

        private Panel recentBar;
        private Label lblRecent;
        private Button btnReset;
        private Button btnOverlay;

        private Panel recentPanel;

        private void InitializeComponent()
        {
            rootLayout = new TableLayoutPanel();
            lblTitle = new Label();
            lblStatus = new Label();
            tilesRow = new TableLayoutPanel();
            tileRecord = new StatTile();
            tileWinRate = new StatTile();
            tileStreak = new StatTile();
            tileMatches = new StatTile();
            avgRow = new TableLayoutPanel();
            tileGoals = new StatTile();
            tileSaves = new StatTile();
            tileAssists = new StatTile();
            recentBar = new Panel();
            lblRecent = new Label();
            btnReset = new Button();
            btnOverlay = new Button();
            recentPanel = new Panel();

            var pageColor = Theme.PageBg;
            var purple = Color.FromArgb(120, 60, 255);
            var blue = Color.FromArgb(0, 140, 255);
            var green = Color.FromArgb(46, 204, 113);

            SuspendLayout();

            this.BackColor = pageColor;
            this.Dock = DockStyle.Fill;

            rootLayout.Dock = DockStyle.Fill;
            rootLayout.BackColor = pageColor;
            rootLayout.Padding = new Padding(20);
            rootLayout.ColumnCount = 1;
            rootLayout.RowCount = 6;
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // ===== TITLE + STATUS =====
            lblTitle.Text = "SESSION TRACKER";
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.ForeColor = Theme.TextPrimary;
            lblTitle.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;

            lblStatus.Text = "";
            lblStatus.Dock = DockStyle.Fill;
            lblStatus.ForeColor = Theme.TextSecondary;
            lblStatus.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;

            // ===== PRIMARY TILES =====
            ConfigureTilesRow(tilesRow, 4);
            SetupTile(tileRecord, "W – L", purple);
            SetupTile(tileWinRate, "WIN RATE", blue);
            SetupTile(tileStreak, "STREAK", green);
            SetupTile(tileMatches, "MATCHES", purple);
            tilesRow.Controls.Add(tileRecord, 0, 0);
            tilesRow.Controls.Add(tileWinRate, 1, 0);
            tilesRow.Controls.Add(tileStreak, 2, 0);
            tilesRow.Controls.Add(tileMatches, 3, 0);

            // ===== AVERAGE TILES =====
            ConfigureTilesRow(avgRow, 3);
            SetupTile(tileGoals, "GOALS/GAME", blue);
            SetupTile(tileSaves, "SAVES/GAME", purple);
            SetupTile(tileAssists, "ASSISTS/GAME", green);
            avgRow.Controls.Add(tileGoals, 0, 0);
            avgRow.Controls.Add(tileSaves, 1, 0);
            avgRow.Controls.Add(tileAssists, 2, 0);

            // ===== RECENT BAR (label + reset) =====
            recentBar.Dock = DockStyle.Fill;
            recentBar.BackColor = Color.Transparent;
            recentBar.Margin = new Padding(0);

            lblRecent.Text = "RECENT MATCHES";
            lblRecent.AutoSize = true;
            lblRecent.Location = new Point(2, 6);
            lblRecent.ForeColor = Theme.AccentSoft;
            lblRecent.Font = new Font("Segoe UI", 12F, FontStyle.Bold);

            btnReset.Text = "RESET";
            btnReset.Size = new Size(96, 26);
            btnReset.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnReset.FlatStyle = FlatStyle.Flat;
            btnReset.FlatAppearance.BorderColor = Color.FromArgb(90, 90, 120);
            btnReset.FlatAppearance.BorderSize = 1;
            btnReset.ForeColor = Theme.TextSecondary;
            btnReset.BackColor = Color.Transparent;
            btnReset.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            btnReset.Cursor = Cursors.Hand;

            btnOverlay.Text = "OVERLAY";
            btnOverlay.Size = new Size(120, 26);
            btnOverlay.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOverlay.FlatStyle = FlatStyle.Flat;
            btnOverlay.FlatAppearance.BorderSize = 0;
            btnOverlay.ForeColor = Color.White;
            btnOverlay.BackColor = Color.FromArgb(120, 60, 255);
            btnOverlay.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            btnOverlay.Cursor = Cursors.Hand;

            recentBar.Controls.Add(lblRecent);
            recentBar.Controls.Add(btnReset);
            recentBar.Controls.Add(btnOverlay);
            recentBar.Resize += (s, e) =>
            {
                btnReset.Location = new Point(recentBar.Width - btnReset.Width - 2, 2);
                btnOverlay.Location = new Point(btnReset.Left - btnOverlay.Width - 8, 2);
            };

            // ===== RECENT PANEL (custom-painted list / setup hint) =====
            recentPanel.Dock = DockStyle.Fill;
            recentPanel.BackColor = Color.Transparent;
            recentPanel.Margin = new Padding(0, 4, 0, 0);

            rootLayout.Controls.Add(lblTitle, 0, 0);
            rootLayout.Controls.Add(lblStatus, 0, 1);
            rootLayout.Controls.Add(tilesRow, 0, 2);
            rootLayout.Controls.Add(avgRow, 0, 3);
            rootLayout.Controls.Add(recentBar, 0, 4);
            rootLayout.Controls.Add(recentPanel, 0, 5);

            this.Controls.Add(rootLayout);

            ResumeLayout(false);
        }

        private static void ConfigureTilesRow(TableLayoutPanel row, int cols)
        {
            row.Dock = DockStyle.Fill;
            row.BackColor = Theme.PageBg;
            row.Margin = new Padding(0, 6, 0, 0);
            row.ColumnCount = cols;
            row.RowCount = 1;
            for (int i = 0; i < cols; i++)
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / cols));
            row.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        }

        private static void SetupTile(StatTile t, string title, Color accent)
        {
            t.Dock = DockStyle.Fill;
            t.Margin = new Padding(0, 0, 12, 0);
            t.Accent = accent;
            t.Title = title;
            t.Value = "—";
            t.Subtitle = "";
            t.TitleFontSize = 9.5f;
            t.ValueFontSize = 21f;
            t.SubtitleFontSize = 9.5f;
        }
    }
}
