---
name: rlhub2-tournaments
description: Where RL Hub 2 Tournaments data comes from
metadata:
  type: project
---

TournamentsPage shows real, current RL tournament coverage from **Google News RSS** (`news.google.com/rss/search?q=...`) — 3 queries tagged RLCS / MAJOR / REGIONAL. `Services/TournamentService.cs` parses title (strips " - Publisher"), source, pubDate, link; dedupes; sorts by date. `Models/TournamentEvent.cs` (Name/Source/Category/Link/Date); `Controls/TournamentCard.cs` shows category pill + source + relative date; clicking opens the article. Filters ALL/RLCS/MAJORS/REGIONAL.

octane.gg was tried first but is UNREACHABLE both from the dev sandbox (TLS fails) AND failed to load on the user's machine, so it was dropped. Google News is the same reliable source the News page uses (verified working). Offline → small fallback list. These are esports tournaments, not in-game RL tournaments (no public API). See [[rlhub2-news-source]].
