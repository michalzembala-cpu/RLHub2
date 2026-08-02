using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

            // Faulted tasks nobody awaited: log only, never interrupt the user — but not the
            // background socket that fails every few seconds while the game is closed. That is
            // expected, not a fault, and it buried the log (2504 identical stacks in one run).
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                if (!IsBenignNetwork(e.Exception)) Log(e.Exception);
                e.SetObserved();
            };
        }

        // A dropped/refused connection is normal when the game isn't running. Don't treat it as
        // a crash worth recording.
        private static bool IsBenignNetwork(Exception? ex) => ex switch
        {
            null => false,
            AggregateException agg => agg.InnerExceptions.Count > 0 && agg.InnerExceptions.All(IsBenignNetwork),
            System.Net.Sockets.SocketException => true,
            IOException io => IsBenignNetwork(io.InnerException),
            ObjectDisposedException => true,
            _ => false,
        };

        private const long MaxLogBytes = 1_000_000;   // ~1 MB; past that, the tail is all that helps

        public static void Log(Exception? ex)
        {
            if (ex == null) return;
            try
            {
                RollIfTooBig();
                File.AppendAllText(LogPath,
                    $"===== {DateTime.Now:yyyy-MM-dd HH:mm:ss} ====={Environment.NewLine}" +
                    Describe(ex) + Environment.NewLine + Environment.NewLine);
            }
            catch { /* logging must never throw */ }

            // Only the exception TYPE goes to telemetry — never the message or stack, which can
            // carry file paths or nicks. Opt-in and fire-and-forget; a no-op if not enabled.
            try { Services.Telemetry.Track("error", new() { ["type"] = ex.GetType().Name }); } catch { }
        }

        // Keep the log from growing without bound: once it crosses the cap, move it aside to
        // errors.old.log (one generation) and start fresh, so the newest crashes are never lost
        // behind megabytes of history.
        private static void RollIfTooBig()
        {
            try
            {
                var fi = new FileInfo(LogPath);
                if (!fi.Exists || fi.Length < MaxLogBytes) return;
                var old = LogPath + ".old";
                if (File.Exists(old)) File.Delete(old);
                File.Move(LogPath, old);
            }
            catch { /* best effort */ }
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

            sb.AppendLine($"NexPlay  v{ver}");
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
