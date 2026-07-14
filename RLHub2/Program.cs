using RLHub2.Helpers;

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

            Application.Run(new DashboardShell());
        }
    }
}