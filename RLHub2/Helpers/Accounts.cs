using System;
using System.Collections.Generic;
using System.Linq;
using RLHub2.Models;
using RLHub2.Services;

namespace RLHub2.Helpers
{
    // App-wide access to the user's accounts and which one is currently active.
    // All stored data (matches, MMR, sessions) is tagged with an account name.
    public static class Accounts
    {
        private static readonly SettingsStore Store = new();

        public static event Action? ActiveChanged;

        public static List<Account> All => Store.LoadAccounts();

        public static string ActiveName => Store.LoadActiveAccountName();

        public static Account? Active
        {
            get { var name = ActiveName; return All.FirstOrDefault(a => a.Name == name); }
        }

        public static void SetActive(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name == ActiveName) return;
            Store.SaveActiveAccount(name);
            ActiveChanged?.Invoke();
        }

        // Which account does this in-game player name belong to? (handles renames via aliases)
        public static Account? MatchByName(string playerName)
            => All.FirstOrDefault(a => a.Matches(playerName));

        // Every in-game name across all accounts — used when scanning replay headers.
        public static IEnumerable<string> AllNames() => All.SelectMany(a => a.AllNames());

        // Data with no account tag (from before multi-account) is treated as the active one.
        public static bool BelongsToActive(string? tag)
            => string.IsNullOrEmpty(tag) || tag == ActiveName;
    }
}
