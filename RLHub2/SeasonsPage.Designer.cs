using System.Drawing;
using System.Windows.Forms;
using RLHub2.Helpers;

namespace RLHub2
{
    partial class SeasonsPage
    {
        private TableLayoutPanel rootLayout;
        private Label lblTitle;
        private TableLayoutPanel contentLayout;
        private FlowLayoutPanel seasonList;
        private RichTextBox rtbDetail;

        private void InitializeComponent()
        {
            rootLayout = new TableLayoutPanel();
            lblTitle = new Label();
            contentLayout = new TableLayoutPanel();
            seasonList = new FlowLayoutPanel();
            rtbDetail = new RichTextBox();

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
            rootLayout.RowCount = 2;
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // ===== TITLE =====
            lblTitle.Text = "SEASONS";
            lblTitle.AutoSize = true;
            lblTitle.ForeColor = Theme.TextPrimary;
            lblTitle.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
            lblTitle.Margin = new Padding(2, 0, 0, 0);

            // ===== CONTENT (list | detail) =====
            contentLayout.Dock = DockStyle.Fill;
            contentLayout.BackColor = pageColor;
            contentLayout.Margin = new Padding(0, 8, 0, 0);
            contentLayout.ColumnCount = 2;
            contentLayout.RowCount = 1;
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210f));
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            seasonList.Dock = DockStyle.Fill;
            seasonList.BackColor = pageColor;
            seasonList.Margin = new Padding(0, 0, 12, 0);
            seasonList.FlowDirection = FlowDirection.TopDown;
            seasonList.WrapContents = false;
            seasonList.AutoScroll = true;

            rtbDetail.Dock = DockStyle.Fill;
            rtbDetail.Margin = new Padding(0);
            rtbDetail.BackColor = panelColor;
            rtbDetail.ForeColor = Theme.TextPrimary;
            rtbDetail.BorderStyle = BorderStyle.None;
            rtbDetail.ReadOnly = true;
            rtbDetail.Multiline = true;
            rtbDetail.ScrollBars = RichTextBoxScrollBars.Vertical;
            rtbDetail.Font = new Font("Segoe UI", 11F);
            rtbDetail.TabStop = false;

            contentLayout.Controls.Add(seasonList, 0, 0);
            contentLayout.Controls.Add(rtbDetail, 1, 0);

            // ===== ASSEMBLE =====
            rootLayout.Controls.Add(lblTitle, 0, 0);
            rootLayout.Controls.Add(contentLayout, 0, 1);

            this.Controls.Add(rootLayout);

            ResumeLayout(false);
            PerformLayout();
        }
    }
}
