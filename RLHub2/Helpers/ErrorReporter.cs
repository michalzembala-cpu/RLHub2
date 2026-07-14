using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RLHub2.Helpers
{
    // Global crash handling: nothing should ever kill the app silently.
    // Every unhandled exception is appended to errors.log and shown in a copyable dialog.
    public static class ErrorReporter
    {
        private static bool _showing;

        public static string LogPath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RLHub2", "errors.log");

        public static void Install()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);

            // UI-thread exceptions: catch instead of letting WinForms kill the process.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => Handle(e.Exception, fatal: false);

            // Anything else on any thread — we can log and warn, but the CLR is going down.
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                Handle(e.ExceptionObject as Exception, fatal: true);

            // Faulted tasks nobody awaited: log only, never interrupt the user.
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                Log(e.Exception);
                e.SetObserved();
            };
        }

        public static void Log(Exception? ex)
        {
            if (ex == null) return;
            try
            {
                File.AppendAllText(LogPath,
                    $"===== {DateTime.Now:yyyy-MM-dd HH:mm:ss} ====={Environment.NewLine}" +
                    Describe(ex) + Environment.NewLine + Environment.NewLine);
            }
            catch { /* logging must never throw */ }
        }

        public static void OpenLog()
        {
            try
            {
                if (!File.Exists(LogPath)) File.WriteAllText(LogPath, "");
                Process.Start(new ProcessStartInfo { FileName = LogPath, UseShellExecute = true });
            }
            catch { }
        }

        private static void Handle(Exception? ex, bool fatal)
        {
            if (ex == null) return;
            Log(ex);

            if (_showing) return; // don't stack dialogs if the error repeats
            _showing = true;
            try
            {
                using var dlg = new ErrorDialog(Describe(ex), fatal);
                dlg.ShowDialog();
            }
            catch { /* if even the dialog fails, the log still has it */ }
            finally { _showing = false; }
        }

        // Everything the author needs to diagnose it, in one copyable block.
        private static string Describe(Exception ex)
        {
            var sb = new StringBuilder();
            var ver = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "?";

            sb.AppendLine($"RL Hub 2  v{ver}");
            sb.AppendLine($"Time:  {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"OS:    {Environment.OSVersion}  ({(Environment.Is64BitProcess ? "x64" : "x86")})");
            sb.AppendLine($".NET:  {Environment.Version}");
            sb.AppendLine();

            for (var e = ex; e != null; e = e.InnerException)
            {
                sb.AppendLine($"{e.GetType().FullName}: {e.Message}");
                if (!string.IsNullOrWhiteSpace(e.StackTrace))
                    sb.AppendLine(e.StackTrace);
                if (e.InnerException != null)
                    sb.AppendLine("--- inner exception ---");
            }

            return sb.ToString().TrimEnd();
        }
    }
}
