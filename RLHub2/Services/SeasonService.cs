using System.Collections.Generic;
using RLHub2.Helpers;
using RLHub2.Models;

namespace RLHub2.Services
{
    // Curated season info. Currently Season 23 with real details from patch notes v2.70.
    // No public API for changelogs — content is curated. Bilingual (PL/EN).
    public class SeasonService
    {
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
                            "Sezonowy, miękki reset rang na start",
                            "Demo Spawn Control — wybór strony bramki do odrodzenia po demolce",
                            "Ulepszony Custom Training — kontrola ruchu i prędkości auta na starcie strzału (też w powietrzu)",
                            "Speed Stat Graphs — podgląd prędkości auta i piłki na żywo",
                        }
                        : new()
                        {
                            "Seasonal soft rank reset at start",
                            "Demo Spawn Control — choose which goal side to respawn from after a demo",
                            "Custom Training upgrades — set car movement and speed at shot start (incl. mid-air)",
                            "Speed Stat Graphs — real-time car and ball velocity overlay",
                        },
                    NewFeatures = pl
                        ? new()
                        {
                            "Motyw FIFA World Cup 26™",
                            "Nowa arena: United Futura",
                            "Nocny wariant areny Futura Garden",
                            "Limitowany event FIFA World Cup 2026 (do 20 lipca)",
                        }
                        : new()
                        {
                            "FIFA World Cup 26™ theme",
                            "New arena: United Futura",
                            "Night variant of the Futura Garden arena",
                            "FIFA World Cup 2026 limited-time event (until July 20)",
                        },
                    Rewards = pl
                        ? new()
                        {
                            "Event: ukończ 10 z 14 wyzwań World Cup → nadwozie Hyundai Ioniq 6",
                            "Dodatkowo: Goal Explosion, Boost i uniwersalny Decal",
                            "Sezonowe nagrody za rangę (poziomy nagród Brąz → SSL)",
                            "Tytuł sezonu za szczytową rangę",
                        }
                        : new()
                        {
                            "Event: complete 10 of 14 World Cup challenges → Hyundai Ioniq 6 body",
                            "Plus a Goal Explosion, a Boost and a universal Decal",
                            "Seasonal ranked rewards (reward levels Bronze → SSL)",
                            "Season title for your peak rank",
                        },
                },
            };
        }
    }
}
