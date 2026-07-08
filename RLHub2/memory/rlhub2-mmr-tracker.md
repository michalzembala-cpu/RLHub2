---
name: rlhub2-mmr-tracker
description: How the RL Hub 2 MMR Tracker stores data and key implementation facts
metadata:
  type: project
---

RL Hub 2 MMR Tracker (MMRPage) implementation:

- `Models/MmrEntry.cs` has `Mode` ("1v1"/"2v2"/"3v3") in addition to `Timestamp` and `Value`. (Legacy forms DashboardForm/NewsForm/PromptForm/MMRChartForm were deleted in a cleanup — don't look for them.)
- Persistence: `Services/MmrStore.cs` saves/loads JSON at `%LocalAppData%\RLHub2\mmr_entries.json` (System.Text.Json). Starts EMPTY — no demo/sample seeding; chart shows a centered "No data yet" empty state. Export/Import JSON + "Open folder" buttons on the selection screen.
- Detail view shows stats (Peak / Average / Change) for the current mode+range, and the chart has a hover tooltip (date + value).
- UX: MMRPage opens on a playlist-selection screen (3 StatTile cards: 1v1/2v2/3v3 showing latest MMR). Clicking a card opens that mode's detail view; "← BACK" returns to selection.
- Chart: `Controls/MmrChartControl.cs` is a CANDLESTICK chart for a SINGLE mode (open=prev entry, close=current). Fixed Y range per mode via Configure(): 1v1 = 500–900, 2v2 = 700–1100, 3v3 = 500–900. Accent: 1v1 blue, 2v2 purple, 3v3 teal.
- Add/edit/delete and history grid (DATE, MMR) all operate within the currently-selected mode.
- Range filter (WEEK/MONTH/SEASON/ALL) — SEASON is a placeholder of last 90 days (no real season-start date wired yet).
- Auto-fetch MMR (spec "future") IS implemented: "⭳ FETCH MMR" button on the selection screen pulls current ranks for all modes from tracker.gg (via ProfileServiceTracker) for the nick saved in Settings (TrackedNick), adding an entry per mode dated now. Needs a valid tracker.gg key — until approved it just shows a toast. See [[rlhub2-profile]] and [[rlhub2-design-language]].
