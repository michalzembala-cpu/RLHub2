using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using RLHub2.Helpers;
using RLHub2.Models;
using RLHub2.Services;

namespace RLHub2
{
    public partial class ProfilePage : Controls.ArenaControl
    {
        protected override string ArenaFile => "stadion1.jpg";

        // tracker.gg fallback (used only when no ballchasing key is set)
        private readonly IProfileService _service = new ProfileServiceTracker();
        private readonly BallMatchStore _matchStore = new();
        private readonly SettingsStore _settings = new();

        public ProfilePage()
        {
            InitializeComponent();
            ApplyLanguage();

            btnSearch.Click += async (s, e) => await SearchAsync();
            txtNick.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    await SearchAsync();
                }
            };

            Load += async (s, e) => await AutoLoadAsync();
        }

        private bool HasBallchasing => !string.IsNullOrWhiteSpace(_settings.LoadBallchasingKey());

        private async Task AutoLoadAsync()
        {
            string nick = Accounts.ActiveName;

            if (HasBallchasing)
            {
                // show cached matches instantly, then sync (upload + fetch) in the background
                var cached = _matchStore.LoadForActive();
                if (cached.Count > 0)
                    Populate(BuildProfile(cached, nick));

                try
                {
                    await Task.Run(() => new BallchasingSync().SyncAsync());
                }
                catch { }

                if (!IsDisposed)
                {
                    var updated = _matchStore.LoadForActive();
                    if (updated.Count > 0)
                        Populate(BuildProfile(updated, string.IsNullOrWhiteSpace(nick) ? "Player" : nick));
                }
                return;
            }

            // no ballchasing key → fall back to tracker.gg auto-search
            if (!string.IsNullOrWhiteSpace(nick))
            {
                txtNick.Text = nick;
                await SearchAsync(silent: true);
            }
        }

        private void ApplyLanguage()
        {
            lblTitle.Text = Localization.T("page_profile");
            txtNick.PlaceholderText = Localization.T("profile_search_ph");
            btnSearch.Text = Localization.T("profile_search_btn");
            lblPrompt.Text = Localization.T("profile_prompt");

            lblRanks.Text = Localization.T("profile_ranks");
            lblStats.Text = Localization.T("profile_stats");
            lblSeasons.Text = Localization.T("profile_seasons");

            cardWins.Title = Localization.T("profile_wins");
            cardMatches.Title = Localization.T("profile_matches");
            cardGoals.Title = Localization.T("profile_goals");
            cardAssists.Title = Localization.T("profile_assists");

            seasonsGrid.Columns["colSeason"].HeaderText = Localization.T("profile_col_season");
            seasonsGrid.Columns["colRank"].HeaderText = Localization.T("profile_col_rank");
            seasonsGrid.Columns["colMmr"].HeaderText = Localization.T("profile_col_mmr");
        }

        private async Task SearchAsync(bool silent = false)
        {
            string nick = txtNick.Text.Trim();
            if (nick.Length == 0)
                return;

            btnSearch.Enabled = false;
            string original = btnSearch.Text;
            btnSearch.Text = Localization.T("profile_loading");
            try
            {
                if (HasBallchasing)
                {
                    // read-only lookup by name (no upload for searched players)
                    var matches = await new BallchasingService().GetPlayerMatchesAsync(nick, 30);
                    if (matches.Count == 0)
                    {
                        if (!silent) Toast.Show(this, Localization.T("profile_error"), ToastKind.Info, 4000);
                        return;
                    }
                    Populate(BuildProfile(matches, nick));
                }
                else
                {
                    var profile = await _service.GetProfileAsync(nick);
                    Populate(profile);
                }
            }
            catch (ProfileServiceTracker.NoKeyException)
            {
                if (!silent) Toast.Show(this, Localization.T("profile_no_key"), ToastKind.Info, 4000);
            }
            catch
            {
                if (!silent) Toast.Show(this, Localization.T("profile_error"), ToastKind.Error, 4000);
            }
            finally
            {
                btnSearch.Enabled = true;
                btnSearch.Text = original;
            }
        }

        // Builds a Profile from ballchasing matches: latest rank per playlist,
        // totals over the stored matches, and a recent-match history table.
        private static Profile BuildProfile(List<BallMatch> matches, string nick)
        {
            var p = new Profile { Nick = nick };
            if (matches.Count == 0) return p;

            var byDate = matches.OrderByDescending(m => m.Date).ToList();

            foreach (var mode in new[] { "1v1", "2v2", "3v3" })
            {
                var latest = byDate.FirstOrDefault(m => m.Mode == mode && m.Ranked && m.RankName.Length > 0)
                             ?? byDate.FirstOrDefault(m => m.Mode == mode);
                if (latest != null)
                    p.Ranks.Add(new PlaylistRank { Mode = mode, RankName = latest.RankName, Mmr = latest.MmrApprox });
            }

            p.Matches = matches.Count;
            p.Wins = matches.Count(m => m.Won);
            p.Goals = matches.Sum(m => m.Goals);
            p.Assists = matches.Sum(m => m.Assists);
            p.Saves = matches.Sum(m => m.Saves);
            p.Mvps = matches.Count(m => m.Mvp);

            foreach (var m in byDate.Take(30))
            {
                string when = m.Date.ToString("MM-dd HH:mm") + (m.Won ? "  ✔" : "  ✖");
                string rank = m.RankName.Length > 0 ? m.RankName : (m.Mode.Length > 0 ? m.Mode : "—");
                p.SeasonHistory.Add(new SeasonRecord { Season = when, PeakRank = rank, PeakMmr = m.MmrApprox });
            }

            var main = p.Ranks.Find(r => r.Mode == "2v2") ?? (p.Ranks.Count > 0 ? p.Ranks[0] : null);
            if (main != null) { p.Rank = main.RankName; p.MMR = main.Mmr; }

            return p;
        }

        private void Populate(Profile p)
        {
            SetRankCard(cardR1, p, "1v1");
            SetRankCard(cardR2, p, "2v2");
            SetRankCard(cardR3, p, "3v3");

            cardWins.Value = p.Wins.ToString("N0");
            cardMatches.Value = p.Matches.ToString("N0");
            cardGoals.Value = p.Goals.ToString("N0");
            cardAssists.Value = p.Assists.ToString("N0");

            seasonsGrid.Rows.Clear();
            foreach (var s in p.SeasonHistory)
                seasonsGrid.Rows.Add(s.Season, s.PeakRank, s.PeakMmr.ToString("N0"));

            lblPrompt.Visible = false;
            contentLayout.Visible = true;
        }

        private static void SetRankCard(Controls.StatTile card, Profile p, string mode)
        {
            var rank = p.Ranks.FirstOrDefault(r => r.Mode == mode);
            if (rank == null)
            {
                card.Value = "—";
                card.Subtitle = "";
                card.Icon = null;
                return;
            }

            card.Value = rank.RankName;
            card.Subtitle = $"MMR {rank.Mmr:N0}";
            card.Icon = RankIcons.GetForRankName(rank.RankName);
        }
    }
}
