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
            btnBack = new Button();
            lblDetailTitle = new Label();
            rangePanel = new FlowLayoutPanel();
            btnWeek = new Button();
            btnMonth = new Button();
            btnSeason = new Button();
            btnAll = new Button();

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

            var pageColor = Theme.PageBg;
            var panelColor = Theme.Surface;

            SuspendLayout();

            // ===== PAGE =====
            this.BackColor = pageColor;
            this.Dock = DockStyle.Fill;

            // =====================================================
            //  SELECTION VIEW
            // =====================================================
            selectionView.Dock = DockStyle.Fill;
            selectionView.BackColor = pageColor;
            selectionView.Padding = new Padding(24);
            selectionView.ColumnCount = 1;
            selectionView.RowCount = 4;
            selectionView.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            selectionView.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
            selectionView.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
            selectionView.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            selectionView.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f));

            lblSelTitle.Text = "MMR TRACKER";
            lblSelTitle.AutoSize = true;
            lblSelTitle.ForeColor = Theme.TextPrimary;
            lblSelTitle.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
            lblSelTitle.Margin = new Padding(4, 0, 0, 0);

            lblSelSub.Text = "Choose a playlist";
            lblSelSub.AutoSize = true;
            lblSelSub.ForeColor = Theme.TextMuted;
            lblSelSub.Font = new Font("Segoe UI", 11F);
            lblSelSub.Margin = new Padding(6, 0, 0, 0);

            cardsLayout.Dock = DockStyle.Fill;
            cardsLayout.BackColor = pageColor;
            cardsLayout.Margin = new Padding(0, 10, 0, 0);
            cardsLayout.ColumnCount = 3;
            cardsLayout.RowCount = 1;
            cardsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            cardsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            cardsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            cardsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            ConfigureCard(card1v1, "1V1", "Duel  •  500 – 900", Color.FromArgb(0, 140, 255));
            ConfigureCard(card2v2, "2V2", "Doubles  •  700 – 1100", Color.FromArgb(150, 90, 255));
            ConfigureCard(card3v3, "3V3", "Standard  •  500 – 900", Color.FromArgb(0, 200, 180));

            cardsLayout.Controls.Add(card1v1, 0, 0);
            cardsLayout.Controls.Add(card2v2, 1, 0);
            cardsLayout.Controls.Add(card3v3, 2, 0);

            // data toolbar (export / import / open folder)
            dataPanel.Dock = DockStyle.Fill;
            dataPanel.BackColor = pageColor;
            dataPanel.Margin = new Padding(0, 8, 0, 0);
            dataPanel.FlowDirection = FlowDirection.LeftToRight;
            dataPanel.WrapContents = false;

            StyleDataButton(btnFetch, "⭳ FETCH MMR");
            btnFetch.BackColor = Theme.Accent;
            btnFetch.ForeColor = Color.White;
            btnFetch.Width = 150;
            StyleDataButton(btnExport, "⭳ EXPORT");
            StyleDataButton(btnImport, "⭱ IMPORT");
            StyleDataButton(btnFolder, "📁 FOLDER");
            dataPanel.Controls.Add(btnFetch);
            dataPanel.Controls.Add(btnExport);
            dataPanel.Controls.Add(btnImport);
            dataPanel.Controls.Add(btnFolder);

            selectionView.Controls.Add(lblSelTitle, 0, 0);
            selectionView.Controls.Add(lblSelSub, 0, 1);
            selectionView.Controls.Add(cardsLayout, 0, 2);
            selectionView.Controls.Add(dataPanel, 0, 3);

            // =====================================================
            //  DETAIL VIEW
            // =====================================================
            detailView.Dock = DockStyle.Fill;
            detailView.BackColor = pageColor;
            detailView.Padding = new Padding(20);
            detailView.Visible = false;
            detailView.ColumnCount = 1;
            detailView.RowCount = 3;
            detailView.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            detailView.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
            detailView.RowStyles.Add(new RowStyle(SizeType.Percent, 62f));
            detailView.RowStyles.Add(new RowStyle(SizeType.Percent, 38f));

            // header
            headerPanel.Dock = DockStyle.Fill;
            headerPanel.BackColor = pageColor;
            headerPanel.Margin = new Padding(0);

            StyleActionButton(btnBack, "← BACK", Color.FromArgb(60, 60, 90));
            btnBack.Dock = DockStyle.Left;
            btnBack.Width = 96;
            btnBack.Margin = new Padding(0);

            lblDetailTitle.Text = "MMR — 2V2";
            lblDetailTitle.Dock = DockStyle.Left;
            lblDetailTitle.Width = 320;
            lblDetailTitle.TextAlign = ContentAlignment.MiddleLeft;
            lblDetailTitle.ForeColor = Theme.TextPrimary;
            lblDetailTitle.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            lblDetailTitle.Padding = new Padding(16, 0, 0, 0);

            rangePanel.Dock = DockStyle.Right;
            rangePanel.AutoSize = true;
            rangePanel.FlowDirection = FlowDirection.LeftToRight;
            rangePanel.WrapContents = false;
            rangePanel.BackColor = pageColor;

            StyleRangeButton(btnWeek, "WEEK");
            StyleRangeButton(btnMonth, "MONTH");
            StyleRangeButton(btnSeason, "SEASON");
            StyleRangeButton(btnAll, "ALL");

            rangePanel.Controls.Add(btnWeek);
            rangePanel.Controls.Add(btnMonth);
            rangePanel.Controls.Add(btnSeason);
            rangePanel.Controls.Add(btnAll);

            // header child order: back (left) first, title (left) second, range (right)
            headerPanel.Controls.Add(rangePanel);
            headerPanel.Controls.Add(lblDetailTitle);
            headerPanel.Controls.Add(btnBack);

            // chart
            chart.Dock = DockStyle.Fill;
            chart.Margin = new Padding(0, 8, 0, 10);

            // bottom (history + form)
            bottomLayout.Dock = DockStyle.Fill;
            bottomLayout.BackColor = pageColor;
            bottomLayout.Margin = new Padding(0);
            bottomLayout.ColumnCount = 2;
            bottomLayout.RowCount = 1;
            bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64f));
            bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f));
            bottomLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            historyPanel.Dock = DockStyle.Fill;
            historyPanel.BackColor = panelColor;
            historyPanel.Margin = new Padding(0, 0, 12, 0);
            historyPanel.Padding = new Padding(2);

            grid.Dock = DockStyle.Fill;
            grid.BackgroundColor = panelColor;
            grid.BorderStyle = BorderStyle.None;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = 36;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.GridHeaderBg;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.TextPrimary;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.DefaultCellStyle.BackColor = Theme.GridRowBg;
            grid.DefaultCellStyle.ForeColor = Theme.TextPrimary;
            grid.DefaultCellStyle.SelectionBackColor = Theme.Accent;
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 10F);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Theme.GridAltBg;
            grid.GridColor = Theme.GridLines;
            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.ReadOnly = true;
            grid.MultiSelect = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.RowTemplate.Height = 30;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate", HeaderText = "DATE", FillWeight = 60 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMmr", HeaderText = "MMR", FillWeight = 40 });

            gridToolbar.Dock = DockStyle.Bottom;
            gridToolbar.Height = 50;
            gridToolbar.BackColor = panelColor;

            StyleActionButton(btnEdit, "EDIT SELECTED", Color.FromArgb(0, 140, 255));
            btnEdit.Location = new Point(6, 8);
            btnEdit.Size = new Size(150, 34);

            StyleActionButton(btnDelete, "DELETE", Color.FromArgb(220, 60, 70));
            btnDelete.Location = new Point(164, 8);
            btnDelete.Size = new Size(104, 34);

            StyleActionButton(btnUndo, "↶ UNDO", Color.FromArgb(80, 80, 110));
            btnUndo.Location = new Point(276, 8);
            btnUndo.Size = new Size(96, 34);

            gridToolbar.Controls.Add(btnEdit);
            gridToolbar.Controls.Add(btnDelete);
            gridToolbar.Controls.Add(btnUndo);

            historyPanel.Controls.Add(grid);
            historyPanel.Controls.Add(gridToolbar);

            // form
            formPanel.Dock = DockStyle.Fill;
            formPanel.BackColor = panelColor;
            formPanel.Margin = new Padding(0);
            formPanel.Padding = new Padding(20);

            lblFormTitle.Text = "ADD ENTRY";
            lblFormTitle.Location = new Point(20, 12);
            lblFormTitle.AutoSize = true;
            lblFormTitle.ForeColor = Theme.AccentSoft;
            lblFormTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);

            lblMmr.Text = "MMR";
            lblMmr.Location = new Point(20, 42);
            lblMmr.AutoSize = true;
            lblMmr.ForeColor = Theme.TextSecondary;
            lblMmr.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);

            txtMmr.Location = new Point(20, 64);
            txtMmr.Width = 200;
            txtMmr.MaxLength = 4;
            txtMmr.Text = "800";
            txtMmr.BackColor = Theme.SurfaceAlt;
            txtMmr.ForeColor = Theme.TextPrimary;
            txtMmr.BorderStyle = BorderStyle.FixedSingle;
            txtMmr.Font = new Font("Segoe UI", 12F);

            lblHint.Text = "↵  Press Enter to add";
            lblHint.Location = new Point(20, 96);
            lblHint.AutoSize = true;
            lblHint.ForeColor = Theme.TextMuted;
            lblHint.Font = new Font("Segoe UI", 9.5F, FontStyle.Italic);

            StyleActionButton(btnCancelEdit, "CANCEL EDIT", Color.FromArgb(60, 60, 90));
            btnCancelEdit.Location = new Point(20, 122);
            btnCancelEdit.Size = new Size(200, 32);
            btnCancelEdit.Visible = false;

            lblStats.Location = new Point(20, 124);
            lblStats.AutoSize = true;
            lblStats.ForeColor = Theme.TextSecondary;
            lblStats.Font = new Font("Segoe UI", 10F);

            formPanel.Controls.Add(lblFormTitle);
            formPanel.Controls.Add(lblMmr);
            formPanel.Controls.Add(txtMmr);
            formPanel.Controls.Add(lblHint);
            formPanel.Controls.Add(btnCancelEdit);
            formPanel.Controls.Add(lblStats);

            bottomLayout.Controls.Add(historyPanel, 0, 0);
            bottomLayout.Controls.Add(formPanel, 1, 0);

            detailView.Controls.Add(headerPanel, 0, 0);
            detailView.Controls.Add(chart, 0, 1);
            detailView.Controls.Add(bottomLayout, 0, 2);

            // ===== ASSEMBLE =====
            this.Controls.Add(detailView);
            this.Controls.Add(selectionView);

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
            b.Size = new Size(86, 34);
            b.Margin = new Padding(6, 8, 0, 8);
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
    }
}
