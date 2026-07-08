using System.Drawing;
using System.Windows.Forms;
using RLHub2.Controls;
using RLHub2.Helpers;

namespace RLHub2
{
    partial class ProfilePage
    {
        private TableLayoutPanel rootLayout;
        private Label lblTitle;

        private Panel searchPanel;
        private TextBox txtNick;
        private Button btnSearch;

        private Panel resultsPanel;
        private Label lblPrompt;
        private TableLayoutPanel contentLayout;

        private Label lblRanks;
        private TableLayoutPanel ranksLayout;
        private StatTile cardR1;
        private StatTile cardR2;
        private StatTile cardR3;

        private Label lblStats;
        private TableLayoutPanel statsLayout;
        private StatTile cardWins;
        private StatTile cardMatches;
        private StatTile cardGoals;
        private StatTile cardAssists;

        private Label lblSeasons;
        private DataGridView seasonsGrid;

        private void InitializeComponent()
        {
            rootLayout = new TableLayoutPanel();
            lblTitle = new Label();
            searchPanel = new Panel();
            txtNick = new TextBox();
            btnSearch = new Button();
            resultsPanel = new Panel();
            lblPrompt = new Label();
            contentLayout = new TableLayoutPanel();
            lblRanks = new Label();
            ranksLayout = new TableLayoutPanel();
            cardR1 = new StatTile();
            cardR2 = new StatTile();
            cardR3 = new StatTile();
            lblStats = new Label();
            statsLayout = new TableLayoutPanel();
            cardWins = new StatTile();
            cardMatches = new StatTile();
            cardGoals = new StatTile();
            cardAssists = new StatTile();
            lblSeasons = new Label();
            seasonsGrid = new DataGridView();

            var pageColor = Theme.PageBg;
            var panelColor = Theme.Surface;

            SuspendLayout();

            // ===== PAGE =====
            this.BackColor = pageColor;
            this.Dock = DockStyle.Fill;

            // ===== ROOT =====
            rootLayout.Dock = DockStyle.Fill;
            rootLayout.BackColor = pageColor;
            rootLayout.Padding = new Padding(20);
            rootLayout.ColumnCount = 1;
            rootLayout.RowCount = 3;
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // ===== TITLE =====
            lblTitle.Text = "PROFILE";
            lblTitle.AutoSize = true;
            lblTitle.ForeColor = Theme.TextPrimary;
            lblTitle.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
            lblTitle.Margin = new Padding(2, 0, 0, 0);

            // ===== SEARCH =====
            searchPanel.Dock = DockStyle.Fill;
            searchPanel.BackColor = pageColor;
            searchPanel.Margin = new Padding(0);

            txtNick.Location = new Point(2, 8);
            txtNick.Size = new Size(320, 34);
            txtNick.BackColor = Theme.SurfaceAlt;
            txtNick.ForeColor = Theme.TextPrimary;
            txtNick.BorderStyle = BorderStyle.FixedSingle;
            txtNick.Font = new Font("Segoe UI", 12F);

            btnSearch.Location = new Point(332, 8);
            btnSearch.Size = new Size(130, 34);
            btnSearch.Text = "SEARCH";
            btnSearch.FlatStyle = FlatStyle.Flat;
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.ForeColor = Color.White;
            btnSearch.BackColor = Color.FromArgb(120, 60, 255);
            btnSearch.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnSearch.Cursor = Cursors.Hand;

            searchPanel.Controls.Add(txtNick);
            searchPanel.Controls.Add(btnSearch);

            // ===== RESULTS =====
            resultsPanel.Dock = DockStyle.Fill;
            resultsPanel.BackColor = pageColor;
            resultsPanel.Margin = new Padding(0, 8, 0, 0);

            lblPrompt.Dock = DockStyle.Fill;
            lblPrompt.Text = "Search for a player to see their stats";
            lblPrompt.TextAlign = ContentAlignment.MiddleCenter;
            lblPrompt.ForeColor = Theme.TextMuted;
            lblPrompt.Font = new Font("Segoe UI", 13F, FontStyle.Italic);

            // content layout (hidden until first search)
            contentLayout.Dock = DockStyle.Fill;
            contentLayout.BackColor = pageColor;
            contentLayout.Visible = false;
            contentLayout.ColumnCount = 1;
            contentLayout.RowCount = 6;
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));   // ranks header
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120f));  // ranks
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));   // stats header
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 110f));  // stats
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));   // seasons header
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // seasons grid

            lblRanks.Text = "RANKS";
            lblRanks.AutoSize = true;
            lblRanks.ForeColor = Theme.AccentSoft;
            lblRanks.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblRanks.Margin = new Padding(2, 6, 0, 4);

            ranksLayout.Dock = DockStyle.Fill;
            ranksLayout.BackColor = pageColor;
            ranksLayout.Margin = new Padding(0);
            ranksLayout.ColumnCount = 3;
            ranksLayout.RowCount = 1;
            ranksLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            ranksLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            ranksLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            ranksLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            ConfigureCard(cardR1, "1V1", Color.FromArgb(0, 140, 255), 16f);
            ConfigureCard(cardR2, "2V2", Color.FromArgb(150, 90, 255), 16f);
            ConfigureCard(cardR3, "3V3", Color.FromArgb(0, 200, 180), 16f);

            ranksLayout.Controls.Add(cardR1, 0, 0);
            ranksLayout.Controls.Add(cardR2, 1, 0);
            ranksLayout.Controls.Add(cardR3, 2, 0);

            lblStats.Text = "STATISTICS";
            lblStats.AutoSize = true;
            lblStats.ForeColor = Theme.AccentSoft;
            lblStats.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblStats.Margin = new Padding(2, 8, 0, 4);

            statsLayout.Dock = DockStyle.Fill;
            statsLayout.BackColor = pageColor;
            statsLayout.Margin = new Padding(0);
            statsLayout.ColumnCount = 4;
            statsLayout.RowCount = 1;
            for (int i = 0; i < 4; i++)
                statsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            statsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            ConfigureCard(cardWins, "WINS", Color.FromArgb(46, 204, 113), 26f);
            ConfigureCard(cardMatches, "MATCHES", Color.FromArgb(0, 140, 255), 26f);
            ConfigureCard(cardGoals, "GOALS", Color.FromArgb(255, 140, 0), 26f);
            ConfigureCard(cardAssists, "ASSISTS", Color.FromArgb(150, 90, 255), 26f);

            statsLayout.Controls.Add(cardWins, 0, 0);
            statsLayout.Controls.Add(cardMatches, 1, 0);
            statsLayout.Controls.Add(cardGoals, 2, 0);
            statsLayout.Controls.Add(cardAssists, 3, 0);

            lblSeasons.Text = "SEASON HISTORY";
            lblSeasons.AutoSize = true;
            lblSeasons.ForeColor = Theme.AccentSoft;
            lblSeasons.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblSeasons.Margin = new Padding(2, 8, 0, 4);

            seasonsGrid.Dock = DockStyle.Fill;
            seasonsGrid.Margin = new Padding(0, 0, 0, 4);
            seasonsGrid.BackgroundColor = panelColor;
            seasonsGrid.BorderStyle = BorderStyle.None;
            seasonsGrid.EnableHeadersVisualStyles = false;
            seasonsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            seasonsGrid.ColumnHeadersHeight = 34;
            seasonsGrid.ColumnHeadersDefaultCellStyle.BackColor = Theme.GridHeaderBg;
            seasonsGrid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.TextPrimary;
            seasonsGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            seasonsGrid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            seasonsGrid.DefaultCellStyle.BackColor = Theme.GridRowBg;
            seasonsGrid.DefaultCellStyle.ForeColor = Theme.TextPrimary;
            seasonsGrid.DefaultCellStyle.SelectionBackColor = Theme.Accent;
            seasonsGrid.DefaultCellStyle.SelectionForeColor = Color.White;
            seasonsGrid.DefaultCellStyle.Font = new Font("Segoe UI", 10F);
            seasonsGrid.AlternatingRowsDefaultCellStyle.BackColor = Theme.GridAltBg;
            seasonsGrid.GridColor = Theme.GridLines;
            seasonsGrid.RowHeadersVisible = false;
            seasonsGrid.AllowUserToAddRows = false;
            seasonsGrid.AllowUserToDeleteRows = false;
            seasonsGrid.AllowUserToResizeRows = false;
            seasonsGrid.ReadOnly = true;
            seasonsGrid.MultiSelect = false;
            seasonsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            seasonsGrid.RowTemplate.Height = 30;
            seasonsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            seasonsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSeason", HeaderText = "SEASON", FillWeight = 34 });
            seasonsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRank", HeaderText = "PEAK RANK", FillWeight = 40 });
            seasonsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMmr", HeaderText = "PEAK MMR", FillWeight = 26 });

            contentLayout.Controls.Add(lblRanks, 0, 0);
            contentLayout.Controls.Add(ranksLayout, 0, 1);
            contentLayout.Controls.Add(lblStats, 0, 2);
            contentLayout.Controls.Add(statsLayout, 0, 3);
            contentLayout.Controls.Add(lblSeasons, 0, 4);
            contentLayout.Controls.Add(seasonsGrid, 0, 5);

            resultsPanel.Controls.Add(contentLayout);
            resultsPanel.Controls.Add(lblPrompt);

            // ===== ASSEMBLE =====
            rootLayout.Controls.Add(lblTitle, 0, 0);
            rootLayout.Controls.Add(searchPanel, 0, 1);
            rootLayout.Controls.Add(resultsPanel, 0, 2);

            this.Controls.Add(rootLayout);

            ResumeLayout(false);
        }

        private void ConfigureCard(StatTile tile, string title, Color accent, float valueFontSize)
        {
            tile.Dock = DockStyle.Fill;
            tile.Margin = new Padding(8);
            tile.Title = title;
            tile.Accent = accent;
            tile.Value = "—";
            tile.ValueFontSize = valueFontSize;
        }
    }
}
