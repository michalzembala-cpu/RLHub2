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
        private readonly Spinner _spinner = new();

        public NewsPage()
        {
            InitializeComponent();
            ApplyLanguage();
            StyleFilters();
            SetupSpinner();

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
            SetLoading(true);
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
                SetLoading(false);
            }
            RenderList();
        }

        private void SetupSpinner()
        {
            _spinner.Accent = Theme.Accent;
            _spinner.Size = new Size(34, 34);
            _spinner.Visible = false;
            listHost.Controls.Add(_spinner);
            _spinner.BringToFront();
            listHost.Resize += (s, e) => CenterSpinner();
            CenterSpinner();
        }

        private void CenterSpinner()
            => _spinner.Location = new Point(
                Math.Max(0, (listHost.Width - _spinner.Width) / 2),
                Math.Max(0, listHost.Height / 2 - 48));

        private void SetLoading(bool on)
        {
            _spinner.Visible = on;
            if (on) { CenterSpinner(); _spinner.BringToFront(); }
        }

        private void StyleFilters()
        {
            foreach (var b in new[] { btnAll, btnGeneral, btnEsports, btnUpdates })
            {
                b.AutoSize = true;
                b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                b.MinimumSize = new Size(0, 38);
                b.Padding = new Padding(16, 0, 16, 0);
                b.FlatStyle = FlatStyle.Flat;
                b.FlatAppearance.BorderSize = 0;
                b.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                b.Cursor = Cursors.Hand;
                b.Margin = new Padding(0, 6, 8, 6);
            }

            btnRefresh.AutoSize = true;
            btnRefresh.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnRefresh.MinimumSize = new Size(0, 38);
            btnRefresh.Padding = new Padding(16, 0, 16, 0);
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.ForeColor = Color.White;
            btnRefresh.BackColor = Theme.Accent;
            btnRefresh.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnRefresh.Cursor = Cursors.Hand;
            btnRefresh.Margin = new Padding(24, 6, 10, 6);
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
            SetLoading(true);
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
                SetLoading(false);
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
