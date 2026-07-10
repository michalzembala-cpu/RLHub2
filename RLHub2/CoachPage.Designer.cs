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
        private StatTile tileOverall;
        private StatTile tileAtk;
        private StatTile tileDef;
        private StatTile tileShot;
        private StatTile tileBoost;
        private StatTile tilePos;
        private Label lblAdviceHeader;
        private Panel advicePanel;
        private Label lblAdvice;

        private void InitializeComponent()
        {
            rootLayout = new TableLayoutPanel();
            lblTitle = new Label();
            tilesLayout = new TableLayoutPanel();
            tileOverall = new StatTile();
            tileAtk = new StatTile();
            tileDef = new StatTile();
            tileShot = new StatTile();
            tileBoost = new StatTile();
            tilePos = new StatTile();
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
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 112f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            lblTitle.Text = "AI COACH";
            lblTitle.AutoSize = true;
            lblTitle.ForeColor = Theme.TextPrimary;
            lblTitle.Font = new Font("Segoe UI", 22F, FontStyle.Bold);

            tilesLayout.Dock = DockStyle.Fill;
            tilesLayout.BackColor = pageColor;
            tilesLayout.Margin = new Padding(0, 6, 0, 0);
            tilesLayout.ColumnCount = 6;
            tilesLayout.RowCount = 1;
            for (int i = 0; i < 6; i++)
                tilesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 6));
            tilesLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            Tile(tileOverall, Color.FromArgb(120, 60, 255));
            Tile(tileAtk, Color.FromArgb(255, 140, 0));
            Tile(tileDef, Color.FromArgb(0, 140, 255));
            Tile(tileShot, Color.FromArgb(46, 204, 113));
            Tile(tileBoost, Color.FromArgb(0, 200, 180));
            Tile(tilePos, Color.FromArgb(235, 70, 140));

            tileOverall.Title = "OVERALL";
            tilesLayout.Controls.Add(tileOverall, 0, 0);
            tilesLayout.Controls.Add(tileAtk, 1, 0);
            tilesLayout.Controls.Add(tileDef, 2, 0);
            tilesLayout.Controls.Add(tileShot, 3, 0);
            tilesLayout.Controls.Add(tileBoost, 4, 0);
            tilesLayout.Controls.Add(tilePos, 5, 0);

            lblAdviceHeader.Text = "ADVICE";
            lblAdviceHeader.Dock = DockStyle.Fill;
            lblAdviceHeader.TextAlign = ContentAlignment.MiddleLeft;
            lblAdviceHeader.ForeColor = Theme.AccentSoft;
            lblAdviceHeader.Font = new Font("Segoe UI", 12F, FontStyle.Bold);

            advicePanel.Dock = DockStyle.Fill;
            advicePanel.BackColor = Theme.Surface;
            advicePanel.Margin = new Padding(0, 4, 0, 0);
            advicePanel.Padding = new Padding(20, 16, 20, 16);
            advicePanel.AutoScroll = true;

            lblAdvice.Dock = DockStyle.Top;
            lblAdvice.AutoSize = true;
            lblAdvice.ForeColor = Theme.TextPrimary;
            lblAdvice.Font = new Font("Segoe UI", 11.5F);
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
            t.Margin = new Padding(0, 0, 10, 0);
            t.Accent = accent;
            t.Value = "—";
            t.TitleFontSize = 9f;
            t.ValueFontSize = 24f;
            t.SubtitleFontSize = 8.5f;
        }
    }
}
