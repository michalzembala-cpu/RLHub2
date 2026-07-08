---
name: rlhub2-ui-direction
description: User's UI/design direction for RL Hub 2 (dashboard feel)
metadata:
  type: feedback
---

User wants RL Hub 2 to feel like Tracker Network + Discord + Rocket League Garage — a gamer app, NOT an admin panel.

**Why:** current UI felt empty/generic (big cards with one number, oversized banner, too many colors).

**How to apply:**
- **Cards smaller** — no huge rectangles with one number (shrink ~30–40%), pack real data.
- **Color discipline: only 3** — purple (primary/accent), blue (info), green (online/success). Drop orange/teal from chrome.
- **No giant banner** — replace with a compact player header: nick + "Season 23 • Diamond II • +23 MMR".
- **Dashboard shows real data** (from local MMR + saved nick), e.g. RANK+MMR card, ROAD TO <next rank> progress bar, weekly MMR. Not SEASON/ONLINE/countdown placeholders.
- **Sidebar too tall (12 items)** — group into sections: Główne (Dashboard, MMR, Road to SSL, AI Coach), Społeczność (Znajomi, Turnieje), Dodatki (Garaż, Cele), Ustawienia. (Not yet implemented.)
- Wants RL graphics (rank icons, RL logo, news thumbnails, avatars) — currently text-only. See [[rlhub2-design-language]].
