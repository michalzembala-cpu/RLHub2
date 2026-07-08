---
name: rlhub2-profile
description: How the RL Hub 2 Profile page gets its data
metadata:
  type: project
---

ProfilePage: enter a nickname → shows ranks per playlist (1v1/2v2/3v3 as StatTiles), stats (wins/matches/goals/assists), and a season-history grid.

REAL data: `ProfilePage` uses `Services/ProfileServiceTracker.cs`, which calls the OFFICIAL `public-api.tracker.gg/v2/rocket-league/standard/profile/{platform}/{nick}` with a `TRN-Api-Key` header, trying platforms epic→steam→psn→xbl. Parses playlist segments (ids 10=1v1, 11=2v2, 13=3v3) for rank/MMR and the overview segment for wins/goals/assists/matches.

The key is read from settings.json via `SettingsStore.LoadTrackerKey()` and is entered by the user in the Settings page (TRACKER.GG API KEY field). The site API `api.tracker.gg` is Cloudflare-blocked for apps (tried, failed even on the user's IP), but `public-api.tracker.gg` is reachable (returns 401, not Cloudflare) — so a VALID key is required. The bundled trn_api_key.txt key (8de3...) is INVALID (401). tracker.gg RL access may require key approval.

No-key → throws `ProfileServiceTracker.NoKeyException` → page tells user to set the key in Settings. Other failures → localized error (NO mock shown). `Services/ProfileServiceMock.cs` kept but NOT wired. See [[rlhub2-localization]].
