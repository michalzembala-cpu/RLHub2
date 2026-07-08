using System.Drawing;
using System.Windows.Forms;
using RLHub2.Controls;
using RLHub2.Helpers;

namespace RLHub2
{
    partial class HomePage
    {
        private TableLayoutPanel rootLayout;
        private StatTile header;
        private TableLayoutPanel contentLayout;

        private TableLayoutPanel leftLayout;
        private RankHero rankHero;
        private SparkCard spark;

        private TableLayoutPanel newsLayout;
        private NewsPreviewCard newsCard;
        private Button btnOpenNews;

        private void InitializeComponent()
        {
            rootLayout = new TableLayoutPanel();
            header = new StatTile();
            contentLayout = new TableLayoutPanel();
            leftLayout = new TableLayoutPanel();
            rankHero = new RankHero();
            spark = new SparkCard();
            newsLayout = new TableLayoutPanel();
            newsCard = new NewsPreviewCard();
            btnOpenNews = new Button();

            var pageColor = Theme.PageBg;
            var purple = Color.FromArgb(120, 60, 255);
            var blue = Color.FromArgb(0, 140, 255);

            SuspendLayout();

            this.BackColor = pageColor;
            this.Dock = DockStyle.Fill;

            rootLayout.Dock = DockStyle.Fill;
            rootLayout.BackColor = Color.Transparent;
            rootLayout.Padding = new Padding(20);
            rootLayout.ColumnCount = 1;
            rootLayout.RowCount = 2;
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90f));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // ===== PLAYER HEADER =====
            header.Dock = DockStyle.Fill;
            header.Margin = new Padding(0, 0, 0, 12);
            header.Accent = purple;
            header.Title = "PLAYER";
            header.Value = "Player";
            header.Subtitle = "Season 23";
            header.TitleFontSize = 10f;
            header.ValueFontSize = 23f;
            header.SubtitleFontSize = 11f;

            // ===== CONTENT (rank+chart | news) =====
            contentLayout.Dock = DockStyle.Fill;
            contentLayout.BackColor = Color.Transparent;
            contentLayout.Margin = new Padding(0);
            contentLayout.ColumnCount = 2;
            contentLayout.RowCount = 1;
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            leftLayout.Dock = DockStyle.Fill;
            leftLayout.BackColor = Color.Transparent;
            leftLayout.Margin = new Padding(0, 0, 12, 0);
            leftLayout.ColumnCount = 1;
            leftLayout.RowCount = 2;
            leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 56f));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 44f));

            rankHero.Dock = DockStyle.Fill;
            rankHero.Margin = new Padding(0, 0, 0, 12);
            rankHero.Accent = purple;

            spark.Dock = DockStyle.Fill;
            spark.Margin = new Padding(0);
            spark.Accent = blue;
            spark.Title = "MMR (2v2)";

            leftLayout.Controls.Add(rankHero, 0, 0);
            leftLayout.Controls.Add(spark, 0, 1);

            // ===== NEWS (right) =====
            newsLayout.Dock = DockStyle.Fill;
            newsLayout.BackColor = Color.Transparent;
            newsLayout.Margin = new Padding(0);
            newsLayout.ColumnCount = 1;
            newsLayout.RowCount = 2;
            newsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            newsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            newsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54f));

            newsCard.Dock = DockStyle.Fill;
            newsCard.Margin = new Padding(0);
            newsCard.Accent = purple;

            btnOpenNews.Text = "OPEN NEWS";
            btnOpenNews.Dock = DockStyle.Fill;
            btnOpenNews.Margin = new Padding(0, 10, 0, 0);
            btnOpenNews.FlatStyle = FlatStyle.Flat;
            btnOpenNews.FlatAppearance.BorderSize = 0;
            btnOpenNews.ForeColor = Color.White;
            btnOpenNews.BackColor = purple;
            btnOpenNews.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnOpenNews.Cursor = Cursors.Hand;

            newsLayout.Controls.Add(newsCard, 0, 0);
            newsLayout.Controls.Add(btnOpenNews, 0, 1);

            contentLayout.Controls.Add(leftLayout, 0, 0);
            contentLayout.Controls.Add(newsLayout, 1, 0);

            rootLayout.Controls.Add(header, 0, 0);
            rootLayout.Controls.Add(contentLayout, 0, 1);

            this.Controls.Add(rootLayout);

            ResumeLayout(false);
        }
    }
}
