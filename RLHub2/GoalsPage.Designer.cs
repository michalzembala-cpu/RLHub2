using System.Drawing;
using System.Windows.Forms;
using RLHub2.Helpers;

namespace RLHub2
{
    partial class GoalsPage
    {
        private TableLayoutPanel rootLayout;
        private Label lblTitle;
        private TableLayoutPanel addLayout;
        private TextBox txtGoal;
        private Button btnAdd;
        private Label lblProgress;
        private Panel gridHost;
        private DataGridView grid;
        private Panel gridToolbar;
        private Button btnDelete;

        private void InitializeComponent()
        {
            rootLayout = new TableLayoutPanel();
            lblTitle = new Label();
            addLayout = new TableLayoutPanel();
            txtGoal = new TextBox();
            btnAdd = new Button();
            lblProgress = new Label();
            gridHost = new Panel();
            grid = new DataGridView();
            gridToolbar = new Panel();
            btnDelete = new Button();

            var pageColor = Theme.PageBg;
            var panelColor = Theme.Surface;

            SuspendLayout();

            this.BackColor = pageColor;
            this.Dock = DockStyle.Fill;

            // ===== ROOT =====
            rootLayout.Dock = DockStyle.Fill;
            rootLayout.BackColor = pageColor;
            rootLayout.Padding = new Padding(20);
            rootLayout.ColumnCount = 1;
            rootLayout.RowCount = 4;
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // ===== TITLE =====
            lblTitle.Text = "GOALS";
            lblTitle.AutoSize = true;
            lblTitle.ForeColor = Theme.TextPrimary;
            lblTitle.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
            lblTitle.Margin = new Padding(2, 0, 0, 0);

            // ===== ADD ROW =====
            addLayout.Dock = DockStyle.Fill;
            addLayout.BackColor = pageColor;
            addLayout.Margin = new Padding(0, 6, 0, 0);
            addLayout.ColumnCount = 2;
            addLayout.RowCount = 1;
            addLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            addLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130f));
            addLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            txtGoal.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtGoal.Height = 32;
            txtGoal.Margin = new Padding(0, 12, 12, 0);
            txtGoal.BackColor = Theme.SurfaceAlt;
            txtGoal.ForeColor = Theme.TextPrimary;
            txtGoal.BorderStyle = BorderStyle.FixedSingle;
            txtGoal.Font = new Font("Segoe UI", 12F);

            btnAdd.Anchor = AnchorStyles.Right;
            btnAdd.Size = new Size(120, 36);
            btnAdd.Margin = new Padding(0, 10, 0, 0);
            btnAdd.Text = "ADD";
            btnAdd.FlatStyle = FlatStyle.Flat;
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.ForeColor = Color.White;
            btnAdd.BackColor = Theme.Accent;
            btnAdd.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnAdd.Cursor = Cursors.Hand;

            addLayout.Controls.Add(txtGoal, 0, 0);
            addLayout.Controls.Add(btnAdd, 1, 0);

            // ===== PROGRESS =====
            lblProgress.Dock = DockStyle.Fill;
            lblProgress.TextAlign = ContentAlignment.MiddleLeft;
            lblProgress.ForeColor = Theme.AccentSoft;
            lblProgress.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblProgress.Margin = new Padding(2, 0, 0, 0);

            // ===== GRID HOST =====
            gridHost.Dock = DockStyle.Fill;
            gridHost.BackColor = panelColor;
            gridHost.Margin = new Padding(0, 6, 0, 0);
            gridHost.Padding = new Padding(2);

            grid.Dock = DockStyle.Fill;
            grid.BackgroundColor = panelColor;
            grid.BorderStyle = BorderStyle.None;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = 34;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.GridHeaderBg;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.TextPrimary;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.DefaultCellStyle.BackColor = Theme.GridRowBg;
            grid.DefaultCellStyle.ForeColor = Theme.TextPrimary;
            grid.DefaultCellStyle.SelectionBackColor = Theme.Accent;
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 11F);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Theme.GridAltBg;
            grid.GridColor = Theme.GridLines;
            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.MultiSelect = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.RowTemplate.Height = 34;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "colDone", HeaderText = "", FillWeight = 10, Resizable = DataGridViewTriState.False });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colGoal", HeaderText = "GOAL", FillWeight = 90, ReadOnly = true });

            gridToolbar.Dock = DockStyle.Bottom;
            gridToolbar.Height = 50;
            gridToolbar.BackColor = panelColor;

            btnDelete.Location = new Point(6, 8);
            btnDelete.Size = new Size(120, 34);
            btnDelete.Text = "DELETE";
            btnDelete.FlatStyle = FlatStyle.Flat;
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.ForeColor = Color.White;
            btnDelete.BackColor = Color.FromArgb(220, 60, 70);
            btnDelete.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnDelete.Cursor = Cursors.Hand;

            gridToolbar.Controls.Add(btnDelete);
            gridHost.Controls.Add(grid);
            gridHost.Controls.Add(gridToolbar);

            // ===== ASSEMBLE =====
            rootLayout.Controls.Add(lblTitle, 0, 0);
            rootLayout.Controls.Add(addLayout, 0, 1);
            rootLayout.Controls.Add(lblProgress, 0, 2);
            rootLayout.Controls.Add(gridHost, 0, 3);

            this.Controls.Add(rootLayout);

            ResumeLayout(false);
            PerformLayout();
        }
    }
}
