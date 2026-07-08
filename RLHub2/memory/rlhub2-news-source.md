---
name: rlhub2-news-source
description: Where RL Hub 2 news comes from and how it's processed
metadata:
  type: project
---

RL Hub 2 news (`Services/NewsService.cs`) fetches REAL news from **Google News RSS** — 3 queries, one per category (Esports = "RLCS esports", Updates = "patch notes update", General = "Rocket League"). Titles are cleaned of the " - Publisher" suffix (publisher shown as the card description), deduped, sorted by date.

Titles are TRANSLATED to Polish via the free Google Translate gtx endpoint (`translate.googleapis.com/translate_a/single`), run concurrently, falling back to the original title on failure.

Reddit RSS was tried first but Reddit returns HTTP 429 for bot-like user agents (and rate-limits by IP), so it was abandoned. A browser-like User-Agent is still set on the HttpClient. Offline → `Fallback()` sample list. Clicking a news card opens the article in the browser. See [[rlhub2-design-language]].
