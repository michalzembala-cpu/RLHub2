using System.Collections.Generic;
using RLHub2.Helpers;
using RLHub2.Models;

namespace RLHub2.Services
{
    // Curated season info. Currently Season 23 with real details from patch notes v2.70.
    // No public API for changelogs — content is curated. Bilingual (PL/EN).
    public class SeasonService
    {
        // Current season + its (estimated) end. Season 23 started 2026-06-10; end is based
        // on the ~91-day free-to-play cycle (2026-09-23, 15:00 UTC) — not officially confirmed.
        public const string CurrentSeasonName = "Season 23";
        public static readonly System.DateTime CurrentSeasonStart =
            new(2026, 6, 10, 16, 0, 0, System.DateTimeKind.Utc);
        public static readonly System.DateTime CurrentSeasonEnd =
            new(2026, 9, 23, 15, 0, 0, System.DateTimeKind.Utc);

        public List<Season> GetSeasons()
        {
            bool pl = Localization.IsPolish;

            return new List<Season>
            {
                new Season
                {
                    Name = "Season 23",
                    Changes = pl
                        ? new()
                        {
                            "Start sezonu: 10 czerwca 2026",
                            "Sezonowy, miękki reset rang na start",
                            "Demo Spawn Control — po demolce wybierasz miejsce odrodzenia na swojej połowie",
                            "Custom Training — kontrola prędkości startowej auta i pochylenia (scenariusze powietrzne)",
                            "Trzy nowe narzędzia w grze — najczęściej proszone przez community",
                        }
                        : new()
                        {
                            "Season start: June 10, 2026",
                            "Seasonal soft rank reset at start",
                            "Demo Spawn Control — after a demo, pick your respawn spot on your side of the field",
                            "Custom Training — set the car's starting speed and pitch (aerial scenarios)",
                            "Three new in-game tools — most-requested community features",
                        },
                    NewFeatures = pl
                        ? new()
                        {
                            "Motyw FIFA World Cup 2026™",
                            "Nowa arena: United Futura",
                            "Futura Garden — wariant nocny",
                            "Rocket Pass Premium: Ryza Trophy (Tier 1) i Ryza T60 (Tier 40)",
                            "Rocket Pass: nowe Boosty, Traile, Anteny i Hymny",
                        }
                        : new()
                        {
                            "FIFA World Cup 2026™ theme",
                            "New arena: United Futura",
                            "Futura Garden — Night variant",
                            "Rocket Pass Premium: Ryza Trophy (Tier 1) and Ryza T60 (Tier 40)",
                            "Rocket Pass: new Boosts, Trails, Antennas and Anthems",
                        },
                    Rewards = pl
                        ? new()
                        {
                            "Event World Cup: ukończ 10 z 14 wyzwań → nadwozie Hyundai Ioniq 6",
                            "Nagrody za event — więcej im wyżej Twój kraj w globalnym rankingu (do 20 lipca)",
                            "Sezonowe nagrody za rangę (Brąz → SSL)",
                            "Tytuł sezonu za szczytową rangę",
                        }
                        : new()
                        {
                            "World Cup event: complete 10 of 14 challenges → Hyundai Ioniq 6 body",
                            "Event rewards — more the higher your country climbs (until July 20)",
                            "Seasonal ranked rewards (Bronze → SSL)",
                            "Season title for your peak rank",
                        },
                },
            };
        }
    }
}
