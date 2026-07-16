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

            var store = new SettingsStore();

            // Game first, then who you are: "which game" decides which pages exist, and only
            // Rocket League has profiles to choose between.
            bool picked = false;
            if (store.LoadAskGameOnStart())
            {
                using var gamePicker = new GamePickerForm();
                picked = gamePicker.ShowDialog() == DialogResult.OK;
            }

            // "Who's playing?" — pointless with a single account, so only ask when there's a choice.
            if (picked && Games.HasProfiles(Games.Active)
                && store.LoadAskProfileOnStart() && Accounts.All.Count > 1)
            {
                using var picker = new ProfilePickerForm();
                picker.ShowDialog();
            }

            // Picking starts a fresh session — land on the game's own home page, not on whatever
            // tab the previous run happened to end on.
            Application.Run(new DashboardShell(picked ? Games.HomePage(Games.Active) : null));
        }
    }
}