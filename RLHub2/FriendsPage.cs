using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using RLHub2.Helpers;
using RLHub2.Services;

namespace RLHub2
{
    public partial class FriendsPage : Controls.ArenaControl
    {
        protected override string ArenaFile => "rl_bg.png";

        private readonly FriendStore _store = new();
        private readonly ProfileServiceTracker _service = new();
        private List<string> _friends = new();

        public FriendsPage()
        {
            InitializeComponent();
            ApplyLanguage();

            btnAdd.Click += async (s, e) => await AddFriend();
            btnDelete.Click += async (s, e) => await DeleteFriend();
            btnRefresh.Click += async (s, e) => await FetchAll();
            txtFriend.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.Handled = true; e.SuppressKeyPress = true; await AddFriend(); }
            };

            Load += async (s, e) => { _friends = _store.Load(); await FetchAll(); };
        }

        private void ApplyLanguage()
        {
            lblTitle.Text = Localization.T("friends_title");
            txtFriend.PlaceholderText = Localization.T("friends_add_ph");
            btnAdd.Text = Localization.T("friends_add");
            btnDelete.Text = Localization.T("friends_delete");
            btnRefresh.Text = Localization.T("friends_refresh");
            grid.Columns["colNick"].HeaderText = Localization.T("friends_col_nick");
            grid.Columns["colMmr"].HeaderText = Localization.T("friends_col_mmr");
        }

        private async System.Threading.Tasks.Task FetchAll()
        {
            if (_friends.Count == 0)
            {
                grid.Rows.Clear();
                lblStatus.Text = Localization.T("friends_empty");
                lblStatus.Visible = true;
                return;
            }

            lblStatus.Text = Localization.T("friends_loading");
            lblStatus.Visible = true;
            btnRefresh.Enabled = false;

            var results = new List<(string Nick, int Mmr)>();
            foreach (var nick in _friends)
            {
                int mmr = 0;
                try
                {
                    var p = await _service.GetProfileAsync(nick);
                    mmr = p.Ranks.FirstOrDefault(r => r.Mode == "2v2")?.Mmr ?? 0;
                }
                catch { /* key inactive / not found -> 0 */ }
                results.Add((nick, mmr));
            }

            results.Sort((a, b) => b.Mmr.CompareTo(a.Mmr));

            grid.Rows.Clear();
            int rank = 1;
            foreach (var r in results)
            {
                int i = grid.Rows.Add(rank++, r.Nick, r.Mmr > 0 ? r.Mmr.ToString("N0") : "—");
                grid.Rows[i].Tag = r.Nick;
            }

            btnRefresh.Enabled = true;
            lblStatus.Visible = false;
        }

        private async System.Threading.Tasks.Task AddFriend()
        {
            string nick = txtFriend.Text.Trim();
            if (nick.Length == 0 || _friends.Contains(nick, StringComparer.OrdinalIgnoreCase)) { txtFriend.Clear(); return; }

            _friends.Add(nick);
            _store.Save(_friends);
            txtFriend.Clear();
            txtFriend.Focus();
            await FetchAll();
        }

        private async System.Threading.Tasks.Task DeleteFriend()
        {
            if (grid.SelectedRows.Count == 0 || grid.SelectedRows[0].Tag is not string nick) return;
            _friends.RemoveAll(f => string.Equals(f, nick, StringComparison.OrdinalIgnoreCase));
            _store.Save(_friends);
            await FetchAll();
        }
    }
}
