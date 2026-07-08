---
name: rlhub2-localization
description: How app-wide language (PL/EN) works in RL Hub 2
metadata:
  type: project
---

RL Hub 2 has app-wide language switching (Polish/English) AND theme switching (Dark/Light), both chosen in Settings.

`Helpers/Theme.cs` mirrors Localization: static `Mode` (AppTheme Dark/Light), `ThemeChanged` event, `Initialize()`/`SetTheme()`, and color properties (PageBg, Sidebar, Surface, SurfaceAlt, CardTop/Bottom, TextPrimary/Secondary/Muted, Grid*, Accent). Controls read `Theme.*` at paint time; pages set colors from `Theme.*` in their Designer (re-read on rebuild). `DashboardShell.ApplyThemeColors()` recolors the persistent shell and `OnThemeChanged` rebuilds the current page (BeginInvoke-deferred). Theme persisted in settings.json (`Theme` = "dark"/"light"). Filter/range buttons set BackColor+ForeColor per active state in code so they work in both themes.


- `Helpers/Localization.cs` — static holder: `Language` (AppLanguage PL/EN), `T(key)` lookup against Pl/En dictionaries, `LanguageChanged` event, `Initialize()` (startup, no event) / `SetLanguage()` (fires event).
- `Services/SettingsStore.cs` — persists language in `%LocalAppData%\RLHub2\settings.json`. Default = Polish.
- `DashboardShell` loads the saved language before building pages, localizes nav buttons, and on `LanguageChanged` rebuilds the nav + recreates the current page (via a stored `Func<UserControl>` factory + BeginInvoke to defer past the triggering event).
- Each page sets its strings from `Localization.T(...)` in an `ApplyLanguage()` called in its constructor; recreation on language change re-runs it. Custom controls expose text properties (NavButton.Text, StatTile.Title, NewsPreviewCard.HeaderText, MmrChartControl.EmptyTitle/EmptySub, NewsCard.Category).
- News: titles are translated to Polish ONLY when language is Polish (see [[rlhub2-news-source]]); category pills are localized for display while filtering still uses the English category key.
