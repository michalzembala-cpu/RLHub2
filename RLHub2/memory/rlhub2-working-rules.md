---
name: rlhub2-working-rules
description: How to deliver code and structure pages for the RL Hub 2 C# WinForms project
metadata:
  type: feedback
---

For the RL Hub 2 project (D:\Users\micha\source\repos\RLHub2\RLHub2, C# / WinForms / .NET 8), ALWAYS deliver complete files, never diffs. When changing a page give the full `X.cs` AND full `X.Designer.cs` (and any other touched file in full).

**Why:** The user does not want to manually merge partial snippets ("dodaj linijkę / zmień linijkę / dopisz metodę" are explicitly forbidden).

**How to apply:** Output entire file contents for every file I touch. Each page is a separate `UserControl` with both `.cs` and `.Designer.cs` — never bare UserControls without a Designer. Hosted in `DashboardShell` (main window) which swaps pages: HomePage, NewsPage, MMRPage, ProfilePage, TournamentsPage, SeasonsPage, SettingsPage. See [[rlhub2-design-language]].
