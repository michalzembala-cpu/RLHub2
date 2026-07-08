---
name: rlhub2-extra-features
description: Extra no-API RL Hub 2 features added beyond the original spec
metadata:
  type: project
---

RL Hub 2 gained several fully-local (no-API) features:

- **Goals** page (nav key "goals", 🎯): add/check-off/delete rank goals with a progress bar. `Models/Goal.cs`, `Services/GoalStore.cs` (goals.json).
- **Garage** page (nav key "garage", 🔧): two sections — Car Presets (name + body dropdown, add/delete/export JSON) and Achievements (text + checkbox, add/toggle/delete). `Models/CarPreset.cs` (CarPreset + Achievement), `Services/GarageStore.cs` (presets.json, achievements.json).
- **MMR extras** in MMRPage: stats panel also shows all-time peak, this-week gain, and a next-rank prediction (~N weeks) from local entries; plus "⭳ FETCH MMR" auto-fetch (see [[rlhub2-mmr-tracker]]).

Three MORE pages were pre-built (function once the tracker.gg key is active; error/empty until then):
- **Road to SSL** (nav "road", 🚀): rank ladder Bronze→SSL from LOCAL MMR entries (peak = ✓, current = ring), + current/to-next/pace/ETA. Works offline now. `RoadPage`.
- **AI Coach** (nav "coach", 🤖): auto-fetches the saved-nick profile, shows goals/saves/assists per game + win%, and gives advice (thresholds). `CoachPage`. Profile model gained `Saves`/`Mvps` (parsed in ProfileServiceTracker overview).
- **Friends** (nav "friends", 👥): add friend nicks (friends.json via `FriendStore`), fetches each 2v2 MMR from tracker.gg and shows a sorted leaderboard. `FriendsPage`.
Also: Profile auto-searches the saved nick on open (silent); MMR auto-fetch button.

Sidebar now has 12 nav items (order: Home, MMR, Road, Coach, Friends, Goals, Garage, News, Profile, Tournaments, Seasons, Settings) — navPanel is a 12-row TableLayoutPanel; adding a page means bumping RowCount + the loop + a NavButton field/init/ConfigureNav/Controls.Add in DashboardShell.Designer.cs AND navButtons array + click + NavigateKey case + ApplyNavTexts in DashboardShell.cs. Deliberately skipped (tracker.gg can't provide): Daily/Item Shop, live Esports/RLCS Hub, in-game tournaments, garage loadout, spectator, live match tracker, world ranking. See [[rlhub2-localization]].
