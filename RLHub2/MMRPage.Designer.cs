using System.Drawing;
using System.Windows.Forms;
using RLHub2.Controls;
using RLHub2.Helpers;

namespace RLHub2
{
    partial class MMRPage
    {
        // ===== SELECTION VIEW =====
        private TableLayoutPanel selectionView;
        private Label lblSelTitle;
        private Label lblSelSub;
        private TableLayoutPanel cardsLayout;
        private StatTile card1v1;
        private StatTile card2v2;
        private StatTile card3v3;
        private FlowLayoutPanel dataPanel;
        private Button btnFetch;
        private Button btnExport;
        private Button btnImport;
        private Button btnFolder;

        // ===== DETAIL VIEW =====
        private TableLayoutPanel detailView;
        private Panel headerPanel;
        private Button btnBack;
        private Label lblDetailTitle;
        private FlowLayoutPanel rangePanel;
        private Button btnWeek;
        private Button btnMonth;
        private Button btnSeason;
        private Button btnAll;

        private MmrChartControl chart;

        private TableLayoutPanel bottomLayout;
        private Panel historyPanel;
        private DataGridView grid;
        private Panel gridToolbar;
        private Button btnEdit;
        private Button btnDelete;
        private Button btnUndo;

        private Panel formPanel;
        private Label lblFormTitle;
        private Label lblMmr;
        private TextBox txtMmr;
        private Label lblHint;
        private Button btnCancelEdit;
        private Label lblStats;

        private void InitializeComponent()
        {
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            selectionView = new TableLayoutPanel();
            lblSelTitle = new Label();
            lblSelSub = new Label();
            cardsLayout = new TableLayoutPanel();
            card1v1 = new StatTile();
            card2v2 = new StatTile();
            card3v3 = new StatTile();
            dataPanel = new FlowLayoutPanel();
            btnFetch = new Button();
            btnExport = new Button();
            btnImport = new Button();
            btnFolder = new Button();
            detailView = new TableLayoutPanel();
            headerPanel = new Panel();
            rangePanel = new FlowLayoutPanel();
            btnWeek = new Button();
            btnMonth = new Button();
            btnSeason = new Button();
            btnAll = new Button();
            lblDetailTitle = new Label();
            btnBack = new Button();
            chart = new MmrChartControl();
            bottomLayout = new TableLayoutPanel();
            historyPanel = new Panel();
            grid = new DataGridView();
            gridToolbar = new Panel();
            btnEdit = new Button();
            btnDelete = new Button();
            btnUndo = new Button();
            formPanel = new Panel();
            lblFormTitle = new Label();
            lblMmr = new Label();
            txtMmr = new TextBox();
            lblHint = new Label();
            btnCancelEdit = new Button();
            lblStats = new Label();
            dataGridViewTextBoxColumn1 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn2 = new DataGridViewTextBoxColumn();
            selectionView.SuspendLayout();
            cardsLayout.SuspendLayout();
            dataPanel.SuspendLayout();
            detailView.SuspendLayout();
            headerPanel.SuspendLayout();
            rangePanel.SuspendLayout();
            bottomLayout.SuspendLayout();
            historyPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)grid).BeginInit();
            gridToolbar.SuspendLayout();
            formPanel.SuspendLayout();
            SuspendLayout();
            // 
            // selectionView
            // 
            selectionView.BackColor = Theme.PageBg;
            selectionView.ColumnCount = 1;
            selectionView.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            selectionView.Controls.Add(lblSelTitle, 0, 0);
            selectionView.Controls.Add(lblSelSub, 0, 1);
            selectionView.Controls.Add(cardsLayout, 0, 2);
            selectionView.Controls.Add(dataPanel, 0, 3);
            selectionView.Dock = DockStyle.Fill;
            selectionView.Location = new Point(0, 0);
            selectionView.Name = "selectionView";
            selectionView.Padding = new Padding(24);
            selectionView.RowCount = 4;
            selectionView.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            selectionView.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            selectionView.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            selectionView.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            selectionView.Size = new Size(645, 609);
            selectionView.TabIndex = 1;
            // 
            // lblSelTitle
            // 
            lblSelTitle.AutoSize = true;
            lblSelTitle.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
            lblSelTitle.ForeColor = Theme.TextPrimary;
            lblSelTitle.Location = new Point(28, 24);
            lblSelTitle.Margin = new Padding(4, 0, 0, 0);
            lblSelTitle.Name = "lblSelTitle";
            lblSelTitle.Size = new Size(237, 41);
            lblSelTitle.TabIndex = 0;
            lblSelTitle.Text = "MMR TRACKER";
            // 
            // lblSelSub
            // 
            lblSelSub.AutoSize = true;
            lblSelSub.Font = new Font("Segoe UI", 11F);
            lblSelSub.ForeColor = Theme.TextMuted;
            lblSelSub.Location = new Point(30, 74);
            lblSelSub.Margin = new Padding(6, 0, 0, 0);
            lblSelSub.Name = "lblSelSub";
            lblSelSub.Size = new Size(121, 20);
            lblSelSub.TabIndex = 1;
            lblSelSub.Text = "Choose a playlist";
            // 
            // cardsLayout
            // 
            cardsLayout.BackColor = Theme.PageBg;
            cardsLayout.ColumnCount = 3;
            cardsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
            cardsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            cardsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            cardsLayout.Controls.Add(card1v1, 0, 0);
            cardsLayout.Controls.Add(card2v2, 1, 0);
            cardsLayout.Controls.Add(card3v3, 2, 0);
            cardsLayout.Dock = DockStyle.Fill;
            cardsLayout.Location = new Point(24, 118);
            cardsLayout.Margin = new Padding(0, 10, 0, 0);
            cardsLayout.Name = "cardsLayout";
            cardsLayout.RowCount = 1;
            cardsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            cardsLayout.Size = new Size(597, 411);
            cardsLayout.TabIndex = 2;
            // 
            // card1v1
            // 
            card1v1.Accent = Color.FromArgb(0, 140, 255);
            card1v1.BackColor = Color.Transparent;
            card1v1.CornerRadius = 18;
            card1v1.Icon = null;
            card1v1.Dock = DockStyle.Fill;
            card1v1.Margin = new Padding(8);
            card1v1.Name = "card1v1";
            card1v1.Subtitle = "";
            card1v1.SubtitleFontSize = 11F;
            card1v1.TabIndex = 0;
            card1v1.Title = "TITLE";
            card1v1.TitleFontSize = 13F;
            card1v1.Value = "--";
            card1v1.ValueFontSize = 40F;
            // 
            // card2v2
            // 
            card2v2.Accent = Color.FromArgb(0, 140, 255);
            card2v2.BackColor = Color.Transparent;
            card2v2.CornerRadius = 18;
            card2v2.Icon = null;
            card2v2.Dock = DockStyle.Fill;
            card2v2.Margin = new Padding(8);
            card2v2.Name = "card2v2";
            card2v2.Subtitle = "";
            card2v2.SubtitleFontSize = 11F;
            card2v2.TabIndex = 1;
            card2v2.Title = "TITLE";
            card2v2.TitleFontSize = 13F;
            card2v2.Value = "--";
            card2v2.ValueFontSize = 40F;
            // 
            // card3v3
            // 
            card3v3.Accent = Color.FromArgb(0, 140, 255);
            card3v3.BackColor = Color.Transparent;
            card3v3.CornerRadius = 18;
            card3v3.Icon = null;
            card3v3.Dock = DockStyle.Fill;
            card3v3.Margin = new Padding(8);
            card3v3.Name = "card3v3";
            card3v3.Subtitle = "";
            card3v3.SubtitleFontSize = 11F;
            card3v3.TabIndex = 2;
            card3v3.Title = "TITLE";
            card3v3.TitleFontSize = 13F;
            card3v3.Value = "--";
            card3v3.ValueFontSize = 40F;
            // 
            // dataPanel
            // 
            dataPanel.BackColor = Theme.PageBg;
            dataPanel.Controls.Add(btnFetch);
            dataPanel.Controls.Add(btnExport);
            dataPanel.Controls.Add(btnImport);
            dataPanel.Controls.Add(btnFolder);
            dataPanel.Dock = DockStyle.Fill;
            dataPanel.Location = new Point(24, 537);
            dataPanel.Margin = new Padding(0, 8, 0, 0);
            dataPanel.Name = "dataPanel";
            dataPanel.Size = new Size(597, 48);
            dataPanel.TabIndex = 3;
            dataPanel.WrapContents = false;
            // 
            // btnFetch
            // 
            btnFetch.BackColor = Color.FromArgb(120, 60, 255);
            btnFetch.ForeColor = Color.White;
            btnFetch.Location = new Point(3, 3);
            btnFetch.Name = "btnFetch";
            btnFetch.Size = new Size(170, 34);
            btnFetch.TabIndex = 0;
            btnFetch.UseVisualStyleBackColor = false;
            // 
            // btnExport
            // 
            btnExport.Location = new Point(159, 3);
            btnExport.Name = "btnExport";
            btnExport.Size = new Size(120, 34);
            btnExport.TabIndex = 1;
            // 
            // btnImport
            // 
            btnImport.Location = new Point(240, 3);
            btnImport.Name = "btnImport";
            btnImport.Size = new Size(120, 34);
            btnImport.TabIndex = 2;
            // 
            // btnFolder
            // 
            btnFolder.Location = new Point(321, 3);
            btnFolder.Name = "btnFolder";
            btnFolder.Size = new Size(120, 34);
            btnFolder.TabIndex = 3;
            // 
            // detailView
            // 
            detailView.BackColor = Theme.PageBg;
            detailView.ColumnCount = 1;
            detailView.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            detailView.Controls.Add(headerPanel, 0, 0);
            detailView.Controls.Add(chart, 0, 1);
            detailView.Controls.Add(bottomLayout, 0, 2);
            detailView.Dock = DockStyle.Fill;
            detailView.Location = new Point(0, 0);
            detailView.Name = "detailView";
            detailView.Padding = new Padding(20);
            detailView.RowCount = 3;
            detailView.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            detailView.RowStyles.Add(new RowStyle(SizeType.Percent, 62F));
            detailView.RowStyles.Add(new RowStyle(SizeType.Percent, 38F));
            detailView.Size = new Size(645, 609);
            detailView.TabIndex = 0;
            detailView.Visible = false;
            // 
            // headerPanel
            // 
            headerPanel.BackColor = Theme.PageBg;
            headerPanel.Controls.Add(rangePanel);
            headerPanel.Controls.Add(lblDetailTitle);
            headerPanel.Controls.Add(btnBack);
            headerPanel.Dock = DockStyle.Fill;
            headerPanel.Location = new Point(20, 20);
            headerPanel.Margin = new Padding(0);
            headerPanel.Name = "headerPanel";
            headerPanel.Size = new Size(605, 52);
            headerPanel.TabIndex = 0;
            // 
            // rangePanel
            // 
            rangePanel.AutoSize = true;
            rangePanel.BackColor = Theme.PageBg;
            rangePanel.Controls.Add(btnWeek);
            rangePanel.Controls.Add(btnMonth);
            rangePanel.Controls.Add(btnSeason);
            rangePanel.Controls.Add(btnAll);
            rangePanel.Dock = DockStyle.Right;
            rangePanel.Location = new Point(281, 0);
            rangePanel.Name = "rangePanel";
            rangePanel.Size = new Size(324, 52);
            rangePanel.TabIndex = 0;
            rangePanel.WrapContents = false;
            // 
            // btnWeek
            // 
            btnWeek.Location = new Point(3, 3);
            btnWeek.Name = "btnWeek";
            btnWeek.Size = new Size(75, 23);
            btnWeek.TabIndex = 0;
            // 
            // btnMonth
            // 
            btnMonth.Location = new Point(84, 3);
            btnMonth.Name = "btnMonth";
            btnMonth.Size = new Size(75, 23);
            btnMonth.TabIndex = 1;
            // 
            // btnSeason
            // 
            btnSeason.Location = new Point(165, 3);
            btnSeason.Name = "btnSeason";
            btnSeason.Size = new Size(75, 23);
            btnSeason.TabIndex = 2;
            // 
            // btnAll
            // 
            btnAll.Location = new Point(246, 3);
            btnAll.Name = "btnAll";
            btnAll.Size = new Size(75, 23);
            btnAll.TabIndex = 3;
            // 
            // lblDetailTitle
            // 
            lblDetailTitle.Dock = DockStyle.Left;
            lblDetailTitle.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            lblDetailTitle.ForeColor = Theme.TextPrimary;
            lblDetailTitle.Location = new Point(96, 0);
            lblDetailTitle.Name = "lblDetailTitle";
            lblDetailTitle.Padding = new Padding(16, 0, 0, 0);
            lblDetailTitle.Size = new Size(320, 52);
            lblDetailTitle.TabIndex = 1;
            lblDetailTitle.Text = "MMR — 2V2";
            lblDetailTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnBack
            // 
            btnBack.Dock = DockStyle.Left;
            btnBack.Location = new Point(0, 0);
            btnBack.Margin = new Padding(0);
            btnBack.Name = "btnBack";
            btnBack.Size = new Size(96, 52);
            btnBack.TabIndex = 2;
            // 
            // chart
            // 
            chart.Accent = Color.FromArgb(120, 60, 255);
            chart.BackColor = Color.Transparent;
            chart.CornerRadius = 18;
            chart.Dock = DockStyle.Fill;
            chart.EmptySub = "Add your first MMR entry below";
            chart.EmptyTitle = "No data yet";
            chart.Location = new Point(20, 80);
            chart.Margin = new Padding(0, 8, 0, 10);
            chart.Name = "chart";
            chart.Size = new Size(605, 302);
            chart.TabIndex = 1;
            chart.YMax = 900;
            chart.YMin = 500;
            // 
            // bottomLayout
            // 
            bottomLayout.BackColor = Theme.PageBg;
            bottomLayout.ColumnCount = 2;
            bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64F));
            bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
            bottomLayout.Controls.Add(historyPanel, 0, 0);
            bottomLayout.Controls.Add(formPanel, 1, 0);
            bottomLayout.Dock = DockStyle.Fill;
            bottomLayout.Location = new Point(20, 392);
            bottomLayout.Margin = new Padding(0);
            bottomLayout.Name = "bottomLayout";
            bottomLayout.RowCount = 1;
            bottomLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            bottomLayout.Size = new Size(605, 197);
            bottomLayout.TabIndex = 2;
            // 
            // historyPanel
            // 
            historyPanel.BackColor = Theme.Surface;
            historyPanel.Controls.Add(grid);
            historyPanel.Controls.Add(gridToolbar);
            historyPanel.Dock = DockStyle.Fill;
            historyPanel.Location = new Point(0, 0);
            historyPanel.Margin = new Padding(0, 0, 12, 0);
            historyPanel.Name = "historyPanel";
            historyPanel.Padding = new Padding(2);
            historyPanel.Size = new Size(375, 197);
            historyPanel.TabIndex = 0;
            // 
            // grid
            // 
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.BackColor = Color.FromArgb(22, 22, 44);
            grid.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.BackgroundColor = Theme.Surface;
            grid.BorderStyle = BorderStyle.None;
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = Color.FromArgb(26, 26, 52);
            dataGridViewCellStyle2.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dataGridViewCellStyle2.ForeColor = Theme.TextPrimary;
            dataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.True;
            grid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            grid.ColumnHeadersHeight = 36;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.Columns.AddRange(new DataGridViewColumn[] { dataGridViewTextBoxColumn1, dataGridViewTextBoxColumn2 });
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = Theme.Surface;
            dataGridViewCellStyle3.Font = new Font("Segoe UI", 10F);
            dataGridViewCellStyle3.ForeColor = Theme.TextPrimary;
            dataGridViewCellStyle3.SelectionBackColor = Color.FromArgb(120, 60, 255);
            dataGridViewCellStyle3.SelectionForeColor = Color.White;
            dataGridViewCellStyle3.WrapMode = DataGridViewTriState.False;
            grid.DefaultCellStyle = dataGridViewCellStyle3;
            grid.Dock = DockStyle.Fill;
            grid.EnableHeadersVisualStyles = false;
            grid.GridColor = Color.FromArgb(40, 40, 72);
            grid.Location = new Point(2, 2);
            grid.MultiSelect = false;
            grid.Name = "grid";
            grid.ReadOnly = true;
            grid.RowHeadersVisible = false;
            grid.RowTemplate.Height = 30;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.Size = new Size(371, 143);
            grid.TabIndex = 0;
            // 
            // gridToolbar
            // 
            gridToolbar.BackColor = Theme.Surface;
            gridToolbar.Controls.Add(btnEdit);
            gridToolbar.Controls.Add(btnDelete);
            gridToolbar.Controls.Add(btnUndo);
            gridToolbar.Dock = DockStyle.Bottom;
            gridToolbar.Location = new Point(2, 145);
            gridToolbar.Name = "gridToolbar";
            gridToolbar.Size = new Size(371, 50);
            gridToolbar.TabIndex = 1;
            // 
            // btnEdit
            // 
            btnEdit.Location = new Point(6, 8);
            btnEdit.Name = "btnEdit";
            btnEdit.Size = new Size(150, 34);
            btnEdit.TabIndex = 0;
            // 
            // btnDelete
            // 
            btnDelete.Location = new Point(164, 8);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(104, 34);
            btnDelete.TabIndex = 1;
            // 
            // btnUndo
            // 
            btnUndo.Location = new Point(276, 8);
            btnUndo.Name = "btnUndo";
            btnUndo.Size = new Size(96, 34);
            btnUndo.TabIndex = 2;
            // 
            // formPanel
            // 
            formPanel.BackColor = Theme.Surface;
            formPanel.Controls.Add(lblFormTitle);
            formPanel.Controls.Add(lblMmr);
            formPanel.Controls.Add(txtMmr);
            formPanel.Controls.Add(lblHint);
            formPanel.Controls.Add(btnCancelEdit);
            formPanel.Controls.Add(lblStats);
            formPanel.Dock = DockStyle.Fill;
            formPanel.Location = new Point(387, 0);
            formPanel.Margin = new Padding(0);
            formPanel.Name = "formPanel";
            formPanel.Padding = new Padding(20);
            formPanel.Size = new Size(218, 197);
            formPanel.TabIndex = 1;
            // 
            // lblFormTitle
            // 
            lblFormTitle.AutoSize = true;
            lblFormTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblFormTitle.ForeColor = Color.FromArgb(160, 118, 255);
            lblFormTitle.Location = new Point(20, 12);
            lblFormTitle.Name = "lblFormTitle";
            lblFormTitle.Size = new Size(100, 21);
            lblFormTitle.TabIndex = 0;
            lblFormTitle.Text = "ADD ENTRY";
            // 
            // lblMmr
            // 
            lblMmr.AutoSize = true;
            lblMmr.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblMmr.ForeColor = Theme.TextSecondary;
            lblMmr.Location = new Point(20, 42);
            lblMmr.Name = "lblMmr";
            lblMmr.Size = new Size(40, 17);
            lblMmr.TabIndex = 1;
            lblMmr.Text = "MMR";
            // 
            // txtMmr
            // 
            txtMmr.BackColor = Theme.SurfaceAlt;
            txtMmr.BorderStyle = BorderStyle.FixedSingle;
            txtMmr.Font = new Font("Segoe UI", 12F);
            txtMmr.ForeColor = Theme.TextPrimary;
            txtMmr.Location = new Point(20, 64);
            txtMmr.MaxLength = 4;
            txtMmr.Name = "txtMmr";
            txtMmr.Size = new Size(200, 29);
            txtMmr.TabIndex = 2;
            txtMmr.Text = "800";
            // 
            // lblHint
            // 
            lblHint.AutoSize = true;
            lblHint.Font = new Font("Segoe UI", 9.5F, FontStyle.Italic);
            lblHint.ForeColor = Theme.TextMuted;
            lblHint.Location = new Point(20, 96);
            lblHint.Name = "lblHint";
            lblHint.Size = new Size(123, 17);
            lblHint.TabIndex = 3;
            lblHint.Text = "↵  Press Enter to add";
            // 
            // btnCancelEdit
            // 
            btnCancelEdit.Location = new Point(20, 122);
            btnCancelEdit.Name = "btnCancelEdit";
            btnCancelEdit.Size = new Size(200, 32);
            btnCancelEdit.TabIndex = 4;
            btnCancelEdit.Visible = false;
            // 
            // lblStats
            // 
            lblStats.AutoSize = true;
            lblStats.Font = new Font("Segoe UI", 10F);
            lblStats.ForeColor = Theme.TextSecondary;
            lblStats.Location = new Point(20, 124);
            lblStats.Name = "lblStats";
            lblStats.Size = new Size(0, 19);
            lblStats.TabIndex = 5;
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.Name = "colDate";
            dataGridViewTextBoxColumn1.ReadOnly = true;
            //
            // dataGridViewTextBoxColumn2
            //
            dataGridViewTextBoxColumn2.Name = "colMmr";
            dataGridViewTextBoxColumn2.ReadOnly = true;
            // 
            // MMRPage
            // 
            BackColor = Theme.PageBg;
            Controls.Add(detailView);
            Controls.Add(selectionView);
            Name = "MMRPage";
            Size = new Size(645, 609);
            selectionView.ResumeLayout(false);
            selectionView.PerformLayout();
            cardsLayout.ResumeLayout(false);
            dataPanel.ResumeLayout(false);
            detailView.ResumeLayout(false);
            headerPanel.ResumeLayout(false);
            headerPanel.PerformLayout();
            rangePanel.ResumeLayout(false);
            bottomLayout.ResumeLayout(false);
            historyPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)grid).EndInit();
            gridToolbar.ResumeLayout(false);
            formPanel.ResumeLayout(false);
            formPanel.PerformLayout();
            ResumeLayout(false);
        }

        private void StyleDataButton(Button b, string text)
        {
            b.Text = text;
            b.Size = new Size(120, 38);
            b.Margin = new Padding(0, 8, 12, 0);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.ForeColor = Theme.TextPrimary;
            b.BackColor = Theme.SurfaceAlt;
            b.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            b.Cursor = Cursors.Hand;
        }

        private void ConfigureCard(StatTile card, string mode, string subtitle, Color accent)
        {
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(10);
            card.Cursor = Cursors.Hand;
            card.Accent = accent;
            card.Title = mode;
            card.Value = "—";
            card.Subtitle = subtitle;
            card.TitleFontSize = 24f;
            card.ValueFontSize = 60f;
            card.SubtitleFontSize = 15f;
        }

        private void StyleRangeButton(Button b, string text)
        {
            b.Text = text;
            // same pill style as the News/Tournaments filters: auto-sized to the label, 38 tall
            b.AutoSize = true;
            b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            b.MinimumSize = new Size(0, 38);
            b.Padding = new Padding(16, 0, 16, 0);
            b.Margin = new Padding(0, 6, 8, 6);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.ForeColor = Theme.TextPrimary;
            b.BackColor = Theme.SurfaceAlt;
            b.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            b.Cursor = Cursors.Hand;
        }

        private void StyleActionButton(Button b, string text, Color color)
        {
            b.Text = text;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.ForeColor = Color.White;
            b.BackColor = color;
            b.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            b.Cursor = Cursors.Hand;
        }

        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
    }
}
