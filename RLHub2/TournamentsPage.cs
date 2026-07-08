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
    public partial class TournamentsPage : Controls.ArenaControl
    {
        protected override string ArenaFile => "stadin3.jpg";

        private readonly TournamentService _service = new();
        private List<TournamentEvent> _all = new();
        private string _filter = "ALL";

        public TournamentsPage()
        {
            InitializeComponent();
            ApplyLanguage();

            btnAll.Click += (s, e) => SetFilter("ALL");
            btnRlcs.Click += (s, e) => SetFilter("RLCS");
            btnMajor.Click += (s, e) => SetFilter("MAJOR");
            btnRegional.Click += (s, e) => SetFilter("REGIONAL");
            btnRefresh.Click += async (s, e) => await ReloadAsync();

            listPanel.SizeChanged += (s, e) => ResizeCards();

            Load += TournamentsPage_Load;
        }

        private async System.Threading.Tasks.Task ReloadAsync()
        {
            btnRefresh.Enabled = false;
            lblStatus.Text = Localization.T("tour_loading");
            lblStatus.Visible = true;
            try { _all = await _service.GetTournamentsAsync(); }
            catch { _all = new List<TournamentEvent>(); }
            finally { btnRefresh.Enabled = true; }
            RenderList();
        }

        private void ApplyLanguage()
        {
            lblTitle.Text = Localization.T("page_tournaments");
            btnAll.Text = Localization.T("tour_all");
            btnRlcs.Text = Localization.T("tour_rlcs");
            btnMajor.Text = Localization.T("tour_major");
            btnRegional.Text = Localization.T("tour_regional");
            btnRefresh.Text = Localization.T("refresh");
            lblStatus.Text = Localization.T("tour_loading");
        }

        private async void TournamentsPage_Load(object? sender, EventArgs e)
        {
            lblStatus.Text = Localization.T("tour_loading");
            lblStatus.Visible = true;

            try { _all = await _service.GetTournamentsAsync(); }
            catch { _all = new List<TournamentEvent>(); }

            SetFilter("ALL");
        }

        private void SetFilter(string filter)
        {
            _filter = filter;

            foreach (var (b, key) in new[]
                     {
                         (btnAll, "ALL"), (btnRlcs, "RLCS"),
                         (btnMajor, "MAJOR"), (btnRegional, "REGIONAL")
                     })
            {
                bool on = key == filter;
                b.BackColor = on ? Theme.Accent : Theme.SurfaceAlt;
                b.ForeColor = on ? Color.White : Theme.TextPrimary;
            }

            RenderList();
        }

        private void RenderList()
        {
            listPanel.SuspendLayout();
            foreach (Control c in listPanel.Controls) c.Dispose();
            listPanel.Controls.Clear();

            var items = _all
                .Where(t => _filter == "ALL" || t.Category == _filter)
                .ToList();

            int width = CardWidth();

            foreach (var t in items)
            {
                var card = new TournamentCard
                {
                    Text = t.Name,
                    StatusText = t.Category,
                    RegionName = t.Source,
                    Tier = "",
                    DateText = FormatDate(t.Date),
                    Prize = "",
                    Accent = AccentOf(t.Category),
                    Width = width,
                    Margin = new Padding(0, 0, 0, 12)
                };

                if (!string.IsNullOrWhiteSpace(t.Link))
                {
                    string url = t.Link;
                    card.Cursor = Cursors.Hand;
                    card.Click += (s, e) => OpenLink(url);
                }

                listPanel.Controls.Add(card);
            }

            listPanel.ResumeLayout();

            lblStatus.Visible = items.Count == 0;
            if (items.Count == 0)
                lblStatus.Text = Localization.T("tour_empty");
        }

        private void ResizeCards()
        {
            int width = CardWidth();
            foreach (Control c in listPanel.Controls)
                c.Width = width;
        }

        private int CardWidth()
        {
            int w = listPanel.ClientSize.Width - listPanel.Padding.Horizontal - 4;
            return Math.Max(220, w);
        }

        private static void OpenLink(string url)
        {
            try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
            catch { /* ignore */ }
        }

        private static Color AccentOf(string category) => category switch
        {
            "RLCS" => Color.FromArgb(150, 90, 255),
            "MAJOR" => Color.FromArgb(255, 140, 0),
            "REGIONAL" => Color.FromArgb(0, 200, 180),
            _ => Color.FromArgb(0, 140, 255)
        };

        private static string FormatDate(DateTime date)
        {
            var span = DateTime.Now - date;
            if (span.TotalHours < 1) return $"{Math.Max(1, (int)span.TotalMinutes)} min";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays}d";
            return date.ToString("dd.MM.yyyy");
        }
    }
}
