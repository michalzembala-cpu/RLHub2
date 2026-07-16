using System.Drawing;
using System.Windows.Forms;
using RLHub2.Controls;
using RLHub2.Helpers;

namespace RLHub2
{
    partial class Cs2Page
    {
        private TableLayoutPanel rootLayout;
        private Label lblTitle;
        private Label lblStatus;
        private SegmentedControl segMode;

        private TableLayoutPanel tilesRow;
        private StatTile tileRecord;
        private StatTile tileWinRate;
        private StatTile tileKd;
        private StatTile tileMatches;

        private TableLayoutPanel avgRow;
        private StatTile tileKills;
        private StatTile tileDeaths;
        private StatTile tileMvp;

        private Panel recentBar;
        private Label lblRecent;
        private Button btnReset;

        private Panel recentPanel;

        private void InitializeComponent()
        {
            rootLayout = new TableLayoutPanel();
            lblTitle = new Label();
            lblStatus = new Label();
            segMode = new SegmentedControl();
            tilesRow = new TableLayoutPanel();
            tileRecord = new StatTile();
            tileWinRate = new StatTile();
            tileKd = new StatTile();
            tileMatches = new StatTile();
            avgRow = new TableLayoutPanel();
            tileKills = new StatTile();
            tileDeaths = new StatTile();
            tileMvp = new StatTile();
            recentBar = new Panel();
            lblRecent = new Label();
            btnReset = new Button();
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
            rootLayout.RowCount = 7;
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            lblTitle.Text = "CS2";
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.ForeColor = Theme.TextPrimary;
            lblTitle.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;

            lblStatus.Text = "";
            lblStatus.Dock = DockStyle.Fill;
            lblStatus.ForeColor = Theme.TextSecondary;
            lblStatus.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;

            // Casual K/D and Premier K/D are not the same statistic; averaging them together
            // tells you nothing. Left-anchored so it keeps its size on a wide page.
            segMode.Anchor = AnchorStyles.Left;
            segMode.Size = new Size(460, 40);
            segMode.Margin = new Padding(0, 4, 0, 6);

            ConfigureTilesRow(tilesRow, 4);
            SetupTile(tileRecord, "W – L", purple);
            SetupTile(tileWinRate, "WIN RATE", blue);
            SetupTile(tileKd, "K/D", green);
            SetupTile(tileMatches, "MATCHES", purple);
            tilesRow.Controls.Add(tileRecord, 0, 0);
            tilesRow.Controls.Add(tileWinRate, 1, 0);
            tilesRow.Controls.Add(tileKd, 2, 0);
            tilesRow.Controls.Add(tileMatches, 3, 0);

            ConfigureTilesRow(avgRow, 3);
            SetupTile(tileKills, "KILLS/GAME", blue);
            SetupTile(tileDeaths, "DEATHS/GAME", purple);
            SetupTile(tileMvp, "MVP", green);
            avgRow.Controls.Add(tileKills, 0, 0);
            avgRow.Controls.Add(tileDeaths, 1, 0);
            avgRow.Controls.Add(tileMvp, 2, 0);

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

            recentBar.Controls.Add(lblRecent);
            recentBar.Controls.Add(btnReset);
            recentBar.Resize += (s, e) =>
                btnReset.Location = new Point(recentBar.Width - btnReset.Width - 2, 2);

            recentPanel.Dock = DockStyle.Fill;
            recentPanel.BackColor = Color.Transparent;
            recentPanel.Margin = new Padding(0, 4, 0, 0);

            rootLayout.Controls.Add(lblTitle, 0, 0);
            rootLayout.Controls.Add(lblStatus, 0, 1);
            rootLayout.Controls.Add(segMode, 0, 2);
            rootLayout.Controls.Add(tilesRow, 0, 3);
            rootLayout.Controls.Add(avgRow, 0, 4);
            rootLayout.Controls.Add(recentBar, 0, 5);
            rootLayout.Controls.Add(recentPanel, 0, 6);

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
