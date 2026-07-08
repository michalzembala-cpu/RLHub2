using System.Drawing;
using System.Windows.Forms;
using RLHub2.Helpers;

namespace RLHub2
{
    partial class TournamentsPage
    {
        private TableLayoutPanel rootLayout;
        private Label lblTitle;
        private FlowLayoutPanel filterPanel;
        private Button btnAll;
        private Button btnRlcs;
        private Button btnMajor;
        private Button btnRegional;
        private Button btnRefresh;
        private Panel listHost;
        private FlowLayoutPanel listPanel;
        private Label lblStatus;

        private void InitializeComponent()
        {
            rootLayout = new TableLayoutPanel();
            lblTitle = new Label();
            filterPanel = new FlowLayoutPanel();
            btnAll = new Button();
            btnRlcs = new Button();
            btnMajor = new Button();
            btnRegional = new Button();
            btnRefresh = new Button();
            listHost = new Panel();
            listPanel = new FlowLayoutPanel();
            lblStatus = new Label();

            var pageColor = Theme.PageBg;

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
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // ===== TITLE =====
            lblTitle.Text = "TOURNAMENTS";
            lblTitle.AutoSize = true;
            lblTitle.ForeColor = Theme.TextPrimary;
            lblTitle.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
            lblTitle.Margin = new Padding(2, 0, 0, 0);

            // ===== FILTER =====
            filterPanel.Dock = DockStyle.Fill;
            filterPanel.BackColor = pageColor;
            filterPanel.FlowDirection = FlowDirection.LeftToRight;
            filterPanel.WrapContents = false;
            filterPanel.Margin = new Padding(0);

            StyleFilterButton(btnAll, "ALL");
            StyleFilterButton(btnRlcs, "RLCS");
            StyleFilterButton(btnMajor, "MAJORS");
            StyleFilterButton(btnRegional, "REGIONAL");

            StyleFilterButton(btnRefresh, "↻ REFRESH");
            btnRefresh.Margin = new Padding(24, 6, 10, 6);

            filterPanel.Controls.Add(btnAll);
            filterPanel.Controls.Add(btnRlcs);
            filterPanel.Controls.Add(btnMajor);
            filterPanel.Controls.Add(btnRegional);
            filterPanel.Controls.Add(btnRefresh);

            // ===== LIST HOST (list + status overlay) =====
            listHost.Dock = DockStyle.Fill;
            listHost.BackColor = pageColor;
            listHost.Margin = new Padding(0, 8, 0, 0);

            listPanel.Dock = DockStyle.Fill;
            listPanel.BackColor = pageColor;
            listPanel.FlowDirection = FlowDirection.TopDown;
            listPanel.WrapContents = false;
            listPanel.AutoScroll = true;
            listPanel.Padding = new Padding(0, 0, 6, 0);

            lblStatus.Dock = DockStyle.Fill;
            lblStatus.Text = "Loading tournaments…";
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            lblStatus.ForeColor = Theme.TextMuted;
            lblStatus.Font = new Font("Segoe UI", 13F, FontStyle.Italic);

            listHost.Controls.Add(listPanel);
            listHost.Controls.Add(lblStatus);
            lblStatus.BringToFront();

            // ===== ASSEMBLE =====
            rootLayout.Controls.Add(lblTitle, 0, 0);
            rootLayout.Controls.Add(filterPanel, 0, 1);
            rootLayout.Controls.Add(listHost, 0, 2);

            this.Controls.Add(rootLayout);

            ResumeLayout(false);
            PerformLayout();
        }

        private void StyleFilterButton(Button b, string text)
        {
            b.Text = text;
            b.Size = new Size(110, 36);
            b.Margin = new Padding(0, 6, 10, 6);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.ForeColor = Theme.TextPrimary;
            b.BackColor = Theme.SurfaceAlt;
            b.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            b.Cursor = Cursors.Hand;
        }
    }
}
