using System.Drawing;
using System.Windows.Forms;
using RLHub2.Controls;
using RLHub2.Helpers;

namespace RLHub2
{
    partial class RoadPage
    {
        private TableLayoutPanel rootLayout;
        private Label lblTitle;
        private SegmentedControl modeSeg;
        private Label lblStats;
        private Panel ladderPanel;

        private void InitializeComponent()
        {
            rootLayout = new TableLayoutPanel();
            lblTitle = new Label();
            modeSeg = new SegmentedControl();
            lblStats = new Label();
            ladderPanel = new Panel();

            var pageColor = Theme.PageBg;

            SuspendLayout();

            this.BackColor = pageColor;
            this.Dock = DockStyle.Fill;

            rootLayout.Dock = DockStyle.Fill;
            rootLayout.BackColor = pageColor;
            rootLayout.Padding = new Padding(20);
            rootLayout.ColumnCount = 1;
            rootLayout.RowCount = 4;
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            lblTitle.Text = "ROAD TO SSL";
            lblTitle.AutoSize = true;
            lblTitle.ForeColor = Theme.TextPrimary;
            lblTitle.Font = new Font("Segoe UI", 27F, FontStyle.Bold);

            modeSeg.Anchor = AnchorStyles.Left;
            modeSeg.Size = new Size(320, 52);
            modeSeg.Margin = new Padding(0, 6, 0, 0);

            lblStats.Dock = DockStyle.Fill;
            lblStats.TextAlign = ContentAlignment.MiddleLeft;
            lblStats.ForeColor = Theme.TextSecondary;
            lblStats.Font = new Font("Segoe UI", 12.5F, FontStyle.Bold);

            ladderPanel.Dock = DockStyle.Fill;
            ladderPanel.BackColor = Color.Transparent;
            ladderPanel.Margin = new Padding(0, 6, 0, 0);

            rootLayout.Controls.Add(lblTitle, 0, 0);
            rootLayout.Controls.Add(modeSeg, 0, 1);
            rootLayout.Controls.Add(lblStats, 0, 2);
            rootLayout.Controls.Add(ladderPanel, 0, 3);

            this.Controls.Add(rootLayout);

            ResumeLayout(false);
            PerformLayout();
        }
    }
}
