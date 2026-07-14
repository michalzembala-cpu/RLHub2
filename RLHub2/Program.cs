using RLHub2.Helpers;
using RLHub2.Services;

namespace RLHub2
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Must come first: from here on no exception can kill the app silently.
            ErrorReporter.Install();

            using (var splash = new SplashForm())
                splash.ShowDialog();

            // "Who's playing?" — pointless with a single account, so only ask when there's a choice.
            var store = new SettingsStore();
            bool pickedProfile = false;
            if (store.LoadAskProfileOnStart() && Accounts.All.Count > 1)
            {
                using var picker = new ProfilePickerForm();
                pickedProfile = picker.ShowDialog() == DialogResult.OK;
            }

            // Choosing a profile starts a fresh session — land on Home, not on whatever tab the
            // previous run happened to end on.
            Application.Run(new DashboardShell(pickedProfile ? "home" : null));
        }
    }
}