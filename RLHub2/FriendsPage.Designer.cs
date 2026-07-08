using System.Drawing;
using System.Windows.Forms;
using RLHub2.Helpers;

namespace RLHub2
{
    partial class FriendsPage
    {
        private TableLayoutPanel rootLayout;
        private Label lblTitle;
        private TableLayoutPanel addLayout;
        private TextBox txtFriend;
        private Button btnAdd;
        private Panel gridHost;
        private DataGridView grid;
        private Label lblStatus;
        private Panel toolbar;
        private Button btnDelete;
        private Button btnRefresh;

        private void InitializeComponent()
        {
            rootLayout = new TableLayoutPanel();
            lblTitle = new Label();
            addLayout = new TableLayoutPanel();
            txtFriend = new TextBox();
            btnAdd = new Button();
            gridHost = new Panel();
            grid = new DataGridView();
            lblStatus = new Label();
            toolbar = new Panel();
            btnDelete = new Button();
            btnRefresh = new Button();

            var pageColor = Theme.PageBg;
            var panelColor = Theme.Surface;

            SuspendLayout();

            this.BackColor = pageColor;
            this.Dock = DockStyle.Fill;

            rootLayout.Dock = DockStyle.Fill;
            rootLayout.BackColor = pageColor;
            rootLayout.Padding = new Padding(20);
            rootLayout.ColumnCount = 1;
            rootLayout.RowCount = 4;
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));

            lblTitle.Text = "FRIENDS";
            lblTitle.AutoSize = true;
            lblTitle.ForeColor = Theme.TextPrimary;
            lblTitle.Font = new Font("Segoe UI", 22F, FontStyle.Bold);

            addLayout.Dock = DockStyle.Fill;
            addLayout.BackColor = pageColor;
            addLayout.Margin = new Padding(0, 6, 0, 0);
            addLayout.ColumnCount = 2;
            addLayout.RowCount = 1;
            addLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            addLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120f));
            addLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            txtFriend.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtFriend.Height = 32;
            txtFriend.Margin = new Padding(0, 12, 12, 0);
            txtFriend.BackColor = Theme.SurfaceAlt;
            txtFriend.ForeColor = Theme.TextPrimary;
            txtFriend.BorderStyle = BorderStyle.FixedSingle;
            txtFriend.Font = new Font("Segoe UI", 12F);

            btnAdd.Anchor = AnchorStyles.Right;
            btnAdd.Size = new Size(110, 36);
            btnAdd.Margin = new Padding(0, 10, 0, 0);
            btnAdd.Text = "ADD";
            btnAdd.FlatStyle = FlatStyle.Flat;
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.ForeColor = Color.White;
            btnAdd.BackColor = Theme.Accent;
            btnAdd.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnAdd.Cursor = Cursors.Hand;

            addLayout.Controls.Add(txtFriend, 0, 0);
            addLayout.Controls.Add(btnAdd, 1, 0);

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
            grid.ReadOnly = true;
            grid.MultiSelect = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.RowTemplate.Height = 34;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRank", HeaderText = "#", FillWeight = 12 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNick", HeaderText = "PLAYER", FillWeight = 58 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMmr", HeaderText = "MMR (2v2)", FillWeight = 30 });

            lblStatus.Dock = DockStyle.Fill;
            lblStatus.Text = "Add friend nicks to compare MMR";
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            lblStatus.ForeColor = Theme.TextMuted;
            lblStatus.Font = new Font("Segoe UI", 13F, FontStyle.Italic);

            gridHost.Controls.Add(grid);
            gridHost.Controls.Add(lblStatus);
            lblStatus.BringToFront();

            toolbar.Dock = DockStyle.Fill;
            toolbar.BackColor = pageColor;

            btnDelete.Location = new Point(0, 8);
            btnDelete.Size = new Size(120, 34);
            btnDelete.Text = "DELETE";
            btnDelete.FlatStyle = FlatStyle.Flat;
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.ForeColor = Color.White;
            btnDelete.BackColor = Color.FromArgb(220, 60, 70);
            btnDelete.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnDelete.Cursor = Cursors.Hand;

            btnRefresh.Location = new Point(130, 8);
            btnRefresh.Size = new Size(120, 34);
            btnRefresh.Text = "↻ REFRESH";
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.ForeColor = Theme.TextPrimary;
            btnRefresh.BackColor = Theme.SurfaceAlt;
            btnRefresh.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnRefresh.Cursor = Cursors.Hand;

            toolbar.Controls.Add(btnDelete);
            toolbar.Controls.Add(btnRefresh);

            rootLayout.Controls.Add(lblTitle, 0, 0);
            rootLayout.Controls.Add(addLayout, 0, 1);
            rootLayout.Controls.Add(gridHost, 0, 2);
            rootLayout.Controls.Add(toolbar, 0, 3);

            this.Controls.Add(rootLayout);

            ResumeLayout(false);
            PerformLayout();
        }
    }
}
