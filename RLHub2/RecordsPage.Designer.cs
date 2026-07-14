using System.Drawing;
using System.Windows.Forms;
using RLHub2.Controls;
using RLHub2.Helpers;

namespace RLHub2
{
    partial class RecordsPage
    {
        private TableLayoutPanel rootLayout;
        private Label lblTitle;

        private TableLayoutPanel grid;
        private StatTile tileMmr;
        private StatTile tileStreak;
        private StatTile tileSession;
        private StatTile tileGain;
        private StatTile tileGoals;

        private Label lblHistory;
        private Panel historyPanel;

        private void InitializeComponent()
        {
            rootLayout = new TableLayoutPanel();
            lblTitle = new Label();
            grid = new TableLayoutPanel();
            tileMmr = new StatTile();
            tileStreak = new StatTile();
            tileSession = new StatTile();
            tileGain = new StatTile();
            tileGoals = new StatTile();
            lblHistory = new Label();
            historyPanel = new Panel();

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
            rootLayout.RowCount = 4;
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 264f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            lblTitle.Text = "RECORDS";
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.ForeColor = Theme.TextPrimary;
            lblTitle.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;

            // ===== RECORD TILES (3 x 2) =====
            grid.Dock = DockStyle.Fill;
            grid.BackColor = Theme.PageBg;
            grid.Margin = new Padding(0, 4, 0, 0);
            grid.ColumnCount = 3;
            grid.RowCount = 2;
            for (int i = 0; i < 3; i++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            SetupTile(tileMmr, "HIGHEST MMR", purple);
            SetupTile(tileStreak, "LONGEST WIN STREAK", green);
            SetupTile(tileGoals, "MOST GOALS (1 MATCH)", blue);
            SetupTile(tileGain, "BIGGEST MMR GAIN / DAY", purple);
            SetupTile(tileSession, "BEST SESSION", green);

            grid.Controls.Add(tileMmr, 0, 0);
            grid.Controls.Add(tileStreak, 1, 0);
            grid.Controls.Add(tileGoals, 2, 0);
            grid.Controls.Add(tileGain, 0, 1);
            grid.Controls.Add(tileSession, 1, 1);

            // ===== HISTORY =====
            lblHistory.Text = "SEASON HISTORY";
            lblHistory.Dock = DockStyle.Fill;
            lblHistory.ForeColor = Theme.AccentSoft;
            lblHistory.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblHistory.TextAlign = ContentAlignment.MiddleLeft;

            historyPanel.Dock = DockStyle.Fill;
            historyPanel.BackColor = Color.Transparent;
            historyPanel.Margin = new Padding(0, 4, 0, 0);

            rootLayout.Controls.Add(lblTitle, 0, 0);
            rootLayout.Controls.Add(grid, 0, 1);
            rootLayout.Controls.Add(lblHistory, 0, 2);
            rootLayout.Controls.Add(historyPanel, 0, 3);

            this.Controls.Add(rootLayout);

            ResumeLayout(false);
        }

        private static void SetupTile(StatTile t, string title, Color accent)
        {
            t.Dock = DockStyle.Fill;
            t.Margin = new Padding(0, 0, 12, 12);
            t.Accent = accent;
            t.Title = title;
            t.Value = "—";
            t.Subtitle = "";
            t.TitleFontSize = 9.5f;
            t.ValueFontSize = 20f;
            t.SubtitleFontSize = 9f;
        }
    }
}
