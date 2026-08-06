using System;
using System.Drawing;
using RLHub2.Services;

namespace RLHub2.Helpers
{
    public enum GameId
    {
        RocketLeague,
        Cs2,
        Overwatch,
    }

    // Which game the app is showing. Each game has its own pages, its own data and its own
    // idea of identity: Rocket League is played on named accounts the user picks between,
    // CS2 is whichever Steam account is running — so only Rocket League asks who you are.
    public static class Games
    {
        private static readonly SettingsStore Store = new();

        public static event Action? ActiveChanged;

        public static GameId Active
        {
            get => Store.Load().ActiveGame switch
            {
                "cs2" => GameId.Cs2,
                "ow" => GameId.Overwatch,
                _ => GameId.RocketLeague,
            };
        }

        public static void SetActive(GameId g)
        {
            if (g == Active) return;
            var cfg = Store.Load();
            cfg.ActiveGame = Key(g);
            Store.Save(cfg);
            ActiveChanged?.Invoke();
        }

        public static string Key(GameId g) => g switch
        {
            GameId.Cs2 => "cs2",
            GameId.Overwatch => "ow",
            _ => "rl",
        };

        public static string Name(GameId g) => g switch
        {
            GameId.Cs2 => "Counter-Strike 2",
            GameId.Overwatch => "Overwatch 2",
            _ => "Rocket League",
        };

        public static string ShortName(GameId g) => g switch
        {
            GameId.Cs2 => "CS2",
            GameId.Overwatch => "OW",
            _ => "RL",
        };

        // The page a game opens on.
        public static string HomePage(GameId g) => g switch
        {
            GameId.Cs2 => "cs2",
            GameId.Overwatch => "ow",
            _ => "home",
        };

        // Each game gets its own accent so you can tell at a glance which one you are in.
        public static Color Accent(GameId g) => g switch
        {
            GameId.Cs2 => Color.FromArgb(222, 130, 40),      // amber
            GameId.Overwatch => Color.FromArgb(250, 150, 20), // Overwatch orange
            _ => Color.FromArgb(120, 60, 255),                // Rocket League purple
        };

        // Only Rocket League tracks named accounts; CS2 identity comes from whoever is signed
        // into Steam, and Overwatch identity is the BattleTag entered in Settings — so neither
        // has a profile to pick between.
        public static bool HasProfiles(GameId g) => g == GameId.RocketLeague;

        // Cover art for the game-picker tile (in Resources). Falls back to a drawn tile if absent.
        public static string TileImage(GameId g) => g switch
        {
            GameId.Cs2 => "game_cs2.jpg",
            GameId.Overwatch => "game_ow.jpg",
            _ => "game_rl.jpg",
        };
    }
}
