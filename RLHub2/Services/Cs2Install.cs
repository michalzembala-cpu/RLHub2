using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace RLHub2.Services
{
    // Finds Counter-Strike 2 and installs the Game State Integration config it needs.
    //
    // GSI only speaks to apps the game has been told about: a .cfg in the game's cfg folder
    // names a URL and the data to send, and CS2 POSTs JSON there as you play. Without that
    // file the game sends nothing — so the app writes it itself rather than making the user
    // hand-place a config in a Steam directory.
    public static class Cs2Install
    {
        public const string ConfigName = "gamestate_integration_rlhub2.cfg";

        // Steam can hold games on several drives; the library list points at all of them.
        private static IEnumerable<string> SteamLibraries()
        {
            var roots = new[]
            {
                @"C:\Program Files (x86)\Steam",
                @"C:\Program Files\Steam",
            };

            foreach (var root in roots)
            {
                var vdf = Path.Combine(root, "steamapps", "libraryfolders.vdf");
                if (!File.Exists(vdf)) continue;

                yield return root;
                string text;
                try { text = File.ReadAllText(vdf); } catch { continue; }

                foreach (Match m in Regex.Matches(text, "\"path\"\\s+\"([^\"]+)\""))
                {
                    var p = m.Groups[1].Value.Replace(@"\\", @"\");
                    if (Directory.Exists(p)) yield return p;
                }
            }
        }

        // The csgo folder of an actual CS2 install (the one with the executable — an old
        // leftover folder without cs2.exe would silently never send anything).
        public static string? CsgoDir()
        {
            foreach (var lib in SteamLibraries())
            {
                var game = Path.Combine(lib, "steamapps", "common", "Counter-Strike Global Offensive");
                if (!File.Exists(Path.Combine(game, "game", "bin", "win64", "cs2.exe"))) continue;

                var csgo = Path.Combine(game, "game", "csgo");
                if (Directory.Exists(csgo)) return csgo;
            }
            return null;
        }

        public static string? ConfigPath()
        {
            var csgo = CsgoDir();
            return csgo == null ? null : Path.Combine(csgo, "cfg", ConfigName);
        }

        public static bool IsInstalled => CsgoDir() != null;

        public static bool IsConfigured
        {
            get { var p = ConfigPath(); return p != null && File.Exists(p); }
        }

        // Writes the config. CS2 reads it at startup, so a running game must be restarted.
        // Returns false if CS2 isn't installed or the file couldn't be written.
        public static bool WriteConfig(int port)
        {
            var path = ConfigPath();
            if (path == null) return false;

            var cfg =
$@"""RL Hub 2 Game State Integration""
{{
	""uri""			""http://127.0.0.1:{port}/""
	""timeout""		""5.0""
	""buffer""		""0.1""
	""throttle""	""0.5""
	""heartbeat""	""10.0""
	""data""
	{{
		""provider""			""1""
		""map""					""1""
		""round""				""1""
		""player_id""			""1""
		""player_state""		""1""
		""player_match_stats""	""1""
	}}
}}
";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, cfg);
                return true;
            }
            catch { return false; }
        }
    }
}
