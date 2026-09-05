using System;

namespace RLHub2.Assistant
{
    // The seam between the assistant and the window it lives in. The assistant must be able to
    // say "open the session page" without referencing DashboardShell — the shell owns the nav
    // buttons and the page factories, and a direct reference would make the tool catalog
    // untestable and circular. The shell fills these in when it starts; anything still null
    // simply means that capability is unavailable right now (no window open yet), and the
    // tools report that instead of throwing.
    public static class AssistantBridge
    {
        // Returns false when the key is not a page of the game currently open.
        public static Func<string, bool>? NavigateTo;

        // Re-runs the current page's data load, so a spoken write shows up without a click.
        public static Action? RefreshCurrentPage;

        public static bool Navigate(string key)
        {
            var nav = NavigateTo;
            return nav != null && nav(key);
        }

        public static void Refresh() => RefreshCurrentPage?.Invoke();
    }
}
