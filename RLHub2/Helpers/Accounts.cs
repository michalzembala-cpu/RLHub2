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

        // Add a new account and make it active. If the name (or an alias) already belongs to an
        // existing account, that one is activated instead of creating a duplicate.
        public static void Add(string name, IEnumerable<string>? aliases = null)
        {
            name = (name ?? "").Trim();
            if (name.Length == 0) return;

            var all = All;
            var existing = all.FirstOrDefault(a => a.Matches(name));
            if (existing != null) { SetActive(existing.Name); return; }

            all.Add(new Account
            {
                Name = name,
                Aliases = (aliases ?? Enumerable.Empty<string>())
                    .Select(a => (a ?? "").Trim())
                    .Where(a => a.Length > 0 && !string.Equals(a, name, StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
            });
            Store.SaveAccounts(all);
            SetActive(name);
        }

        // Which account does this in-game player name belong to? (handles renames via aliases)
        public static Account? MatchByName(string playerName)
            => All.FirstOrDefault(a => a.Matches(playerName));

        // Every in-game name across all accounts — used when scanning replay headers.
        public static IEnumerable<string> AllNames() => All.SelectMany(a => a.AllNames());

        // Data with no account tag (from before multi-account) is treated as the active one.
        public static bool BelongsToActive(string? tag)
            => string.IsNullOrEmpty(tag) || tag == ActiveName;

        // Same test, but with the active name snapshotted once. Filtering a list of matches
        // through BelongsToActive would look the active account up per entry.
        public static Func<string?, bool> ActiveFilter()
        {
            var active = ActiveName;
            return tag => string.IsNullOrEmpty(tag) || tag == active;
        }
    }
}
