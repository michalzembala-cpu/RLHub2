---
name: rlhub2-seasons
description: How the RL Hub 2 Seasons page is sourced
metadata:
  type: project
---

SeasonsPage is CURATED static content (no public API for RL season changelogs). `Services/SeasonService.cs` currently returns just **Season 23** with REAL details from official patch notes v2.70 (FIFA World Cup 26 theme, United Futura arena, Demo Spawn Control, Custom Training upgrades, Speed Stat Graphs, World Cup LTE rewards = Hyundai Ioniq 6 body + extras). Bilingual (PL/EN via `Localization.IsPolish`). To add older seasons, append more `Season` objects (look up real data via WebSearch).

UI: master-detail — left FlowLayoutPanel of season buttons, right a dark read-only RichTextBox showing the selected season's three sections (ZMIANY/NOWOŚCI/NAGRODY headers localized, colored). No shop / Rocket Pass (per spec). See [[rlhub2-localization]].
