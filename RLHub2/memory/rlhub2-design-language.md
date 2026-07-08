---
name: rlhub2-design-language
description: Visual style and goal for the RL Hub 2 Rocket League desktop app
metadata:
  type: project
---

RL Hub 2 is a Rocket League desktop dashboard. It must NOT look like default WinForms or Material Design — it should feel like the Rocket League game launcher / a modern dashboard.

Palette: dark background (near-black navy ~RGB 12,12,26), with navy / blue / purple / teal accents. Large rounded tiles, hover glow effects, smooth transitions.

Page intents: **Home** = dashboard (season, server status, season-end / challenges / tournaments countdowns, 3 latest news + OPEN NEWS button) — NOT a news list. **News** = full news with categories General/Esports/Updates. **MMR** = big feature: 1v1/2v2/3v3 entries (add/edit/delete), colored chart, ranges week/month/season/all, local JSON storage. **Profile** = ranks/stats by nick. **Tournaments** = current RL tournaments. **Seasons** = changes/new/rewards (no shop/rocket pass). **Settings** = Dark/Light themes.

Reusable custom-drawn controls live in `Controls/` (e.g. `RoundedPanel`, `StatTile`, `NewsPreviewCard`) — they draw text in OnPaint to avoid WinForms transparent-label issues on gradients. See [[rlhub2-working-rules]].
