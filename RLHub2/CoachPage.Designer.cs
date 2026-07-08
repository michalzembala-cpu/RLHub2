using System.Drawing;
using System.Windows.Forms;
using RLHub2.Controls;
using RLHub2.Helpers;

namespace RLHub2
{
    partial class CoachPage
    {
        private TableLayoutPanel rootLayout;
        private Label lblTitle;
        private TableLayoutPanel tilesLayout;
        private StatTile cardG;
        private StatTile cardS;
        private StatTile cardA;
        private StatTile cardW;
        private Label lblAdviceHeader;
        private Panel advicePanel;
        private Label lblAdvice;

        private void InitializeComponent()
        {
            rootLayout = new TableLayoutPanel();
            lblTitle = new Label();
            tilesLayout = new TableLayoutPanel();
            cardG = new StatTile();
            cardS = new StatTile();
            cardA = new StatTile();
            cardW = new StatTile();
            lblAdviceHeader = new Label();
            advicePanel = new Panel();
            lblAdvice = new Label();

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
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            lblTitle.Text = "AI COACH";
            lblTitle.AutoSize = true;
            lblTitle.ForeColor = Theme.TextPrimary;
            lblTitle.Font = new Font("Segoe UI", 22F, FontStyle.Bold);

            tilesLayout.Dock = DockStyle.Fill;
            tilesLayout.BackColor = pageColor;
            tilesLayout.Margin = new Padding(0, 6, 0, 0);
            tilesLayout.ColumnCount = 4;
            tilesLayout.RowCount = 1;
            for (int i = 0; i < 4; i++)
                tilesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tilesLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            Tile(cardG, Color.FromArgb(255, 140, 0));
            Tile(cardS, Color.FromArgb(0, 140, 255));
            Tile(cardA, Color.FromArgb(150, 90, 255));
            Tile(cardW, Color.FromArgb(46, 204, 113));

            tilesLayout.Controls.Add(cardG, 0, 0);
            tilesLayout.Controls.Add(cardS, 1, 0);
            tilesLayout.Controls.Add(cardA, 2, 0);
            tilesLayout.Controls.Add(cardW, 3, 0);

            lblAdviceHeader.Text = "ADVICE";
            lblAdviceHeader.Dock = DockStyle.Fill;
            lblAdviceHeader.TextAlign = ContentAlignment.MiddleLeft;
            lblAdviceHeader.ForeColor = Theme.AccentSoft;
            lblAdviceHeader.Font = new Font("Segoe UI", 12F, FontStyle.Bold);

            advicePanel.Dock = DockStyle.Fill;
            advicePanel.BackColor = Theme.Surface;
            advicePanel.Margin = new Padding(0, 4, 0, 0);
            advicePanel.Padding = new Padding(20);

            lblAdvice.Dock = DockStyle.Fill;
            lblAdvice.ForeColor = Theme.TextPrimary;
            lblAdvice.Font = new Font("Segoe UI", 13F);
            advicePanel.Controls.Add(lblAdvice);

            rootLayout.Controls.Add(lblTitle, 0, 0);
            rootLayout.Controls.Add(tilesLayout, 0, 1);
            rootLayout.Controls.Add(lblAdviceHeader, 0, 2);
            rootLayout.Controls.Add(advicePanel, 0, 3);

            this.Controls.Add(rootLayout);

            ResumeLayout(false);
            PerformLayout();
        }

        private void Tile(StatTile t, Color accent)
        {
            t.Dock = DockStyle.Fill;
            t.Margin = new Padding(8);
            t.Accent = accent;
            t.Value = "—";
            t.ValueFontSize = 26f;
        }
    }
}
