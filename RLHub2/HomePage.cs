using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using RLHub2.Helpers;
using RLHub2.Models;
using RLHub2.Services;

namespace RLHub2
{
    public partial class HomePage : Controls.ArenaControl
    {
        protected override string ArenaFile => "stadion1.jpg";

        private readonly NewsService _newsService = new();
        private readonly MmrStore _mmr = new();
        private System.Windows.Forms.Timer? _seasonTimer;

        private static readonly (string Name, int Mmr, Color Gem)[] Tiers =
        {
            ("Bronze", 0, Color.FromArgb(150, 110, 70)),
            ("Silver", 400, Color.FromArgb(170, 180, 195)),
            ("Gold", 550, Color.FromArgb(220, 180, 70)),
            ("Platinum", 700, Color.FromArgb(120, 220, 210)),
            ("Diamond", 850, Color.FromArgb(90, 150, 255)),
            ("Champion", 1000, Color.FromArgb(180, 110, 255)),
            ("Grand Champion", 1200, Color.FromArgb(255, 90, 120)),
            ("Supersonic Legend", 1400, Color.FromArgb(255, 150, 230)),
        };

        public event EventHandler? OpenNewsRequested;

        public HomePage()
        {
            InitializeComponent();
            ApplyLanguage();

            btnOpenNews.Click += (s, e) => OpenNewsRequested?.Invoke(this, EventArgs.Empty);
            btnOpenNews.MouseEnter += (s, e) => btnOpenNews.BackColor = Color.FromArgb(150, 90, 255);
            btnOpenNews.MouseLeave += (s, e) => btnOpenNews.BackColor = Color.FromArgb(120, 60, 255);
            btnOpenNews.Resize += (s, e) => ApplyButtonRegion();

            Load += HomePage_Load;

            // live season countdown in the header
            _seasonTimer = new System.Windows.Forms.Timer { Interval = 60000 };
            _seasonTimer.Tick += (s, e) => UpdateSeasonHeader();
            _seasonTimer.Start();
            Disposed += (s, e) => _seasonTimer?.Dispose();
        }

        private void UpdateSeasonHeader()
        {
            if (IsDisposed) return;
            var left = SeasonService.CurrentSeasonEnd - DateTime.UtcNow;
            if (left <= TimeSpan.Zero)
            {
                seasonTile.Value = Localization.IsPolish ? "Zakończony" : "Ended";
                seasonTile.Subtitle = "";
            }
            else
            {
                seasonTile.Value = $"{left.Days}d {left.Hours}h {left.Minutes}m";
                seasonTile.Subtitle = (Localization.IsPolish ? "kończy się " : "ends ")
                    + SeasonService.CurrentSeasonEnd.ToLocalTime().ToString("dd.MM.yyyy");
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyButtonRegion();
        }

        private void ApplyButtonRegion()
        {
            if (btnOpenNews.Width <= 0 || btnOpenNews.Height <= 0) return;
            var r = btnOpenNews.ClientRectangle;
            int d = 14 * 2;
            var path = new GraphicsPath();
            path.AddArc(r.Left, r.Top, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            path.CloseAllFigures();
            btnOpenNews.Region = new Region(path);
        }

        private void ApplyLanguage()
        {
            header.Title = Localization.T("home_player");
            newsCard.HeaderText = Localization.T("home_latest_news");
            btnOpenNews.Text = Localization.T("home_open_news");
        }

        private async void HomePage_Load(object? sender, EventArgs e)
        {
            UpdateSeasonHeader();
            PopulateStats();
            await LoadNews();
        }

        private static string Roman(int n) => n switch { 1 => "I", 2 => "II", 3 => "III", _ => "IV" };

        private void PopulateStats()
        {
            string nick = new SettingsStore().LoadTrackedNick();
            if (string.IsNullOrWhiteSpace(nick)) nick = "Player";
            header.Value = nick;

            var list = _mmr.Load().Where(x => x.Mode == "2v2").OrderBy(x => x.Timestamp).ToList();

            if (list.Count == 0)
            {
                header.Subtitle = Localization.T("home_no_data");
                rankHero.Icon = null;
                rankHero.TierLabel = "—";
                rankHero.MmrText = "";
                rankHero.ProgressText = "";
                rankHero.FootText = Localization.T("home_no_data");
                rankHero.Fraction = 0;
                spark.SetValues(Array.Empty<int>());
                return;
            }

            int current = list[^1].Value;
            int peak = list.Max(x => x.Value);
            var weekAgo = DateTime.Now.AddDays(-7);
            var wk = list.Where(x => x.Timestamp >= weekAgo).ToList();
            int weekly = wk.Count > 0 ? current - wk[0].Value : 0;
            string wsign = weekly > 0 ? "+" : "";

            int curIdx = 0;
            for (int i = 0; i < Tiers.Length; i++)
                if (current >= Tiers[i].Mmr) curIdx = i;

            var tier = Tiers[curIdx];
            rankHero.GemColor = tier.Gem;
            rankHero.Accent = Color.FromArgb(120, 60, 255);
            rankHero.Icon = RankIcons.Get(tier.Name);

            bool hasNext = curIdx < Tiers.Length - 1;
            int nextStart = hasNext ? Tiers[curIdx + 1].Mmr : tier.Mmr;
            int div = hasNext
                ? Math.Clamp(1 + (int)((current - tier.Mmr) / Math.Max(1.0, (nextStart - tier.Mmr) / 4.0)), 1, 4)
                : 4;

            rankHero.TierLabel = tier.Name;
            rankHero.MmrText = $"Div {Roman(div)}   •   {current} MMR";

            if (hasNext)
            {
                rankHero.Fraction = (float)(current - tier.Mmr) / (nextStart - tier.Mmr);
                rankHero.ProgressText = $"{current} / {nextStart}   •   +{nextStart - current} {Localization.T("home_road")} {Tiers[curIdx + 1].Name}";
            }
            else
            {
                rankHero.Fraction = 1f;
                rankHero.ProgressText = Localization.T("home_maxed");
            }

            rankHero.FootText = $"Peak {peak}   ·   {wsign}{weekly} this week";

            header.Subtitle = $"{tier.Name}   •   {wsign}{weekly} MMR";

            spark.SetValues(list.TakeLast(16).Select(x => x.Value));
        }

        private async Task LoadNews()
        {
            try
            {
                var news = await _newsService.GetNewsAsync();
                var items = news.Take(3).Select(n => (n.Title, n.Category)).ToList();
                newsCard.SetItems(items);
            }
            catch
            {
                newsCard.SetItems(Array.Empty<(string, string)>());
            }
        }
    }
}
