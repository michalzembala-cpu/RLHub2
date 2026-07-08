using System.Drawing;
using System.Windows.Forms;
using RLHub2.Helpers;

namespace RLHub2
{
    partial class NewsPage
    {
        private TableLayoutPanel rootLayout;
        private Label lblTitle;
        private FlowLayoutPanel catPanel;
        private Button btnAll;
        private Button btnGeneral;
        private Button btnEsports;
        private Button btnUpdates;
        private Button btnRefresh;
        private Panel listHost;
        private FlowLayoutPanel listPanel;
        private Label lblStatus;

        private void InitializeComponent()
        {
            rootLayout = new TableLayoutPanel();
            lblTitle = new Label();
            catPanel = new FlowLayoutPanel();
            btnAll = new Button();
            btnGeneral = new Button();
            btnEsports = new Button();
            btnUpdates = new Button();
            btnRefresh = new Button();
            listHost = new Panel();
            listPanel = new FlowLayoutPanel();
            lblStatus = new Label();
            rootLayout.SuspendLayout();
            catPanel.SuspendLayout();
            listHost.SuspendLayout();
            SuspendLayout();
            // 
            // rootLayout
            // 
            rootLayout.BackColor = Color.FromArgb(12, 12, 26);
            rootLayout.ColumnCount = 1;
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootLayout.Controls.Add(lblTitle, 0, 0);
            rootLayout.Controls.Add(catPanel, 0, 1);
            rootLayout.Controls.Add(listHost, 0, 2);
            rootLayout.Dock = DockStyle.Fill;
            rootLayout.Location = new Point(0, 0);
            rootLayout.Name = "rootLayout";
            rootLayout.Padding = new Padding(20);
            rootLayout.RowCount = 3;
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootLayout.Size = new Size(1106, 625);
            rootLayout.TabIndex = 0;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(255, 255, 255);
            lblTitle.Location = new Point(22, 20);
            lblTitle.Margin = new Padding(2, 0, 0, 0);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(106, 41);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "NEWS";
            // 
            // catPanel
            // 
            catPanel.BackColor = Color.FromArgb(12, 12, 26);
            catPanel.Controls.Add(btnAll);
            catPanel.Controls.Add(btnGeneral);
            catPanel.Controls.Add(btnEsports);
            catPanel.Controls.Add(btnUpdates);
            catPanel.Controls.Add(btnRefresh);
            catPanel.Dock = DockStyle.Fill;
            catPanel.Location = new Point(20, 68);
            catPanel.Margin = new Padding(0);
            catPanel.Name = "catPanel";
            catPanel.Size = new Size(1066, 52);
            catPanel.TabIndex = 1;
            catPanel.WrapContents = false;
            // 
            // btnAll
            // 
            btnAll.Location = new Point(3, 3);
            btnAll.Name = "btnAll";
            btnAll.Size = new Size(75, 23);
            btnAll.TabIndex = 0;
            // 
            // btnGeneral
            // 
            btnGeneral.Location = new Point(84, 3);
            btnGeneral.Name = "btnGeneral";
            btnGeneral.Size = new Size(75, 23);
            btnGeneral.TabIndex = 1;
            // 
            // btnEsports
            // 
            btnEsports.Location = new Point(165, 3);
            btnEsports.Name = "btnEsports";
            btnEsports.Size = new Size(75, 23);
            btnEsports.TabIndex = 2;
            // 
            // btnUpdates
            // 
            btnUpdates.Location = new Point(246, 3);
            btnUpdates.Name = "btnUpdates";
            btnUpdates.Size = new Size(75, 23);
            btnUpdates.TabIndex = 3;
            // 
            // btnRefresh
            // 
            btnRefresh.Location = new Point(348, 6);
            btnRefresh.Margin = new Padding(24, 6, 10, 6);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(75, 23);
            btnRefresh.TabIndex = 4;
            // 
            // listHost
            // 
            listHost.BackColor = Color.FromArgb(12, 12, 26);
            listHost.Controls.Add(listPanel);
            listHost.Controls.Add(lblStatus);
            listHost.Dock = DockStyle.Fill;
            listHost.Location = new Point(20, 128);
            listHost.Margin = new Padding(0, 8, 0, 0);
            listHost.Name = "listHost";
            listHost.Size = new Size(1066, 477);
            listHost.TabIndex = 2;
            // 
            // listPanel
            // 
            listPanel.AutoScroll = true;
            listPanel.BackColor = Color.FromArgb(12, 12, 26);
            listPanel.Dock = DockStyle.Fill;
            listPanel.FlowDirection = FlowDirection.TopDown;
            listPanel.Location = new Point(0, 0);
            listPanel.Name = "listPanel";
            listPanel.Padding = new Padding(0, 0, 6, 0);
            listPanel.Size = new Size(1066, 477);
            listPanel.TabIndex = 0;
            listPanel.WrapContents = false;
            // 
            // lblStatus
            // 
            lblStatus.Dock = DockStyle.Fill;
            lblStatus.Font = new Font("Segoe UI", 13F, FontStyle.Italic);
            lblStatus.ForeColor = Color.FromArgb(140, 160, 200);
            lblStatus.Location = new Point(0, 0);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(1066, 477);
            lblStatus.TabIndex = 1;
            lblStatus.Text = "Loading news…";
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // NewsPage
            // 
            BackColor = Color.FromArgb(12, 12, 26);
            Controls.Add(rootLayout);
            Name = "NewsPage";
            Size = new Size(1106, 625);
            rootLayout.ResumeLayout(false);
            rootLayout.PerformLayout();
            catPanel.ResumeLayout(false);
            listHost.ResumeLayout(false);
            ResumeLayout(false);
        }

        private void StyleCatButton(Button b, string text)
        {
            b.Text = text;
            b.Size = new Size(104, 36);
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
