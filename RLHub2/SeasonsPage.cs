using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using RLHub2.Helpers;
using RLHub2.Models;
using RLHub2.Services;

namespace RLHub2
{
    public partial class SeasonsPage : Controls.ArenaControl
    {
        protected override string ArenaFile => "stadin3.jpg";

        private readonly SeasonService _service = new();
        private readonly List<Button> _buttons = new();
        private Button? _activeButton;

        public SeasonsPage()
        {
            InitializeComponent();
            ApplyLanguage();
            // Build after the handle exists, otherwise the RichTextBox loses its formatted content.
            Load += (s, e) => BuildList();
        }

        private void ApplyLanguage()
        {
            lblTitle.Text = Localization.T("page_seasons");
        }

        private void BuildList()
        {
            var seasons = _service.GetSeasons();
            seasonList.Controls.Clear();
            _buttons.Clear();

            foreach (var season in seasons)
            {
                var b = new Button
                {
                    Text = season.Name,
                    Tag = season,
                    Size = new Size(184, 46),
                    Margin = new Padding(0, 0, 0, 8),
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = Theme.TextPrimary,
                    BackColor = Theme.SurfaceAlt,
                    Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(14, 0, 0, 0)
                };
                b.FlatAppearance.BorderSize = 0;
                b.Click += (s, e) => Select((Button)s!);
                _buttons.Add(b);
                seasonList.Controls.Add(b);
            }

            if (_buttons.Count > 0)
                Select(_buttons[0]);
        }

        private void Select(Button button)
        {
            _activeButton = button;
            foreach (var b in _buttons)
            {
                bool on = b == button;
                b.BackColor = on ? Theme.Accent : Theme.SurfaceAlt;
                b.ForeColor = on ? Color.White : Theme.TextPrimary;
            }

            if (button.Tag is Season season)
                ShowSeason(season);
        }

        private void ShowSeason(Season season)
        {
            rtbDetail.Clear();

            AppendTitle(season.Name);
            AppendSection(Localization.T("season_changes"), season.Changes, Color.FromArgb(0, 140, 255));
            AppendSection(Localization.T("season_new"), season.NewFeatures, Color.FromArgb(0, 200, 180));
            AppendSection(Localization.T("season_rewards"), season.Rewards, Color.FromArgb(255, 160, 60));

            rtbDetail.SelectionStart = 0;
            rtbDetail.ScrollToCaret();
        }

        private void AppendTitle(string text)
        {
            rtbDetail.SelectionColor = Theme.TextPrimary;
            rtbDetail.SelectionFont = new Font("Segoe UI", 18F, FontStyle.Bold);
            rtbDetail.AppendText("  " + text + "\n\n");
        }

        private void AppendSection(string header, List<string> items, Color color)
        {
            rtbDetail.SelectionColor = color;
            rtbDetail.SelectionFont = new Font("Segoe UI", 13F, FontStyle.Bold);
            rtbDetail.AppendText("  " + header + "\n");

            rtbDetail.SelectionColor = Theme.TextPrimary;
            rtbDetail.SelectionFont = new Font("Segoe UI", 11.5F);
            foreach (var item in items)
                rtbDetail.AppendText("      •  " + item + "\n");

            rtbDetail.AppendText("\n");
        }
    }
}
