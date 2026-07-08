using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RLHub2.Controls;
using RLHub2.Helpers;
using RLHub2.Models;
using RLHub2.Services;

namespace RLHub2
{
    public partial class NewsPage : Controls.ArenaControl
    {
        protected override string ArenaFile => "stadin3.jpg";

        private readonly NewsService _newsService = new();
        private List<NewsItem> _all = new();
        private string _category = "ALL";

        public NewsPage()
        {
            InitializeComponent();
            ApplyLanguage();

            btnAll.Click += (s, e) => SetCategory("ALL");
            btnGeneral.Click += (s, e) => SetCategory("General");
            btnEsports.Click += (s, e) => SetCategory("Esports");
            btnUpdates.Click += (s, e) => SetCategory("Updates");
            btnRefresh.Click += async (s, e) => await ReloadAsync();

            listPanel.SizeChanged += (s, e) => ResizeCards();

            Load += NewsPage_Load;
        }

        private async System.Threading.Tasks.Task ReloadAsync()
        {
            btnRefresh.Enabled = false;
            lblStatus.Text = Localization.T("news_loading");
            lblStatus.Visible = true;
            try
            {
                _all = await _newsService.GetNewsAsync();
            }
            catch
            {
                _all = new List<NewsItem>();
            }
            finally
            {
                btnRefresh.Enabled = true;
            }
            RenderList();
        }

        private void ApplyLanguage()
        {
            lblTitle.Text = Localization.T("news_title");
            btnAll.Text = Localization.T("news_all");
            btnGeneral.Text = Localization.T("news_general");
            btnEsports.Text = Localization.T("news_esports");
            btnUpdates.Text = Localization.T("news_updates");
            btnRefresh.Text = Localization.T("refresh");
        }

        private static string LocalizeCategory(string category) => category switch
        {
            "General" => Localization.T("news_general"),
            "Esports" => Localization.T("news_esports"),
            "Updates" => Localization.T("news_updates"),
            _ => category
        };

        private async void NewsPage_Load(object? sender, EventArgs e)
        {
            lblStatus.Text = Localization.T("news_loading");
            lblStatus.Visible = true;
            try
            {
                _all = await _newsService.GetNewsAsync();
            }
            catch
            {
                _all = new List<NewsItem>();
            }

            SetCategory("ALL");
        }

        private void SetCategory(string category)
        {
            _category = category;

            foreach (var (b, key) in new[]
                     {
                         (btnAll, "ALL"), (btnGeneral, "General"),
                         (btnEsports, "Esports"), (btnUpdates, "Updates")
                     })
            {
                bool on = key == category;
                b.BackColor = on ? Theme.Accent : Theme.SurfaceAlt;
                b.ForeColor = on ? Color.White : Theme.TextPrimary;
            }

            RenderList();
        }

        private void RenderList()
        {
            listPanel.SuspendLayout();

            foreach (Control c in listPanel.Controls)
                c.Dispose();
            listPanel.Controls.Clear();

            var items = _all
                .Where(n => _category == "ALL" || n.Category == _category)
                .OrderByDescending(n => n.PublishedDate)
                .ToList();

            int width = CardWidth();

            foreach (var item in items)
            {
                var card = new NewsCard
                {
                    Text = item.Title,
                    Category = LocalizeCategory(item.Category),
                    Description = item.Description,
                    DateText = FormatDate(item.PublishedDate),
                    Accent = AccentFor(item.Category),
                    Width = width,
                    Margin = new Padding(0, 0, 0, 12)
                };

                if (!string.IsNullOrWhiteSpace(item.Link))
                {
                    string url = item.Link;
                    card.Cursor = Cursors.Hand;
                    card.Click += (s, e) => OpenLink(url);
                }

                listPanel.Controls.Add(card);
            }

            listPanel.ResumeLayout();

            lblStatus.Visible = items.Count == 0;
            if (items.Count == 0)
                lblStatus.Text = Localization.T("news_empty");
        }

        private void ResizeCards()
        {
            int width = CardWidth();
            foreach (Control c in listPanel.Controls)
                c.Width = width;
        }

        private static void OpenLink(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch
            {
                // ignore — browser unavailable
            }
        }

        private int CardWidth()
        {
            int w = listPanel.ClientSize.Width - listPanel.Padding.Horizontal - 4;
            return Math.Max(200, w);
        }

        private static Color AccentFor(string category) => category switch
        {
            "General" => Color.FromArgb(0, 140, 255),
            "Esports" => Color.FromArgb(150, 90, 255),
            "Updates" => Color.FromArgb(0, 200, 180),
            _ => Color.FromArgb(120, 60, 255)
        };

        private static string FormatDate(DateTime date)
        {
            var span = DateTime.Now - date;
            if (span.TotalHours < 1) return $"{Math.Max(1, (int)span.TotalMinutes)} min ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
            return date.ToString("dd.MM.yyyy");
        }
    }
}
