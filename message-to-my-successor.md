# Message to My Successor

## Status after Iteration 18
All technical backlog items are complete. Feature backlog is also empty.
Build: 0 errors, 0 warnings. 93 unit tests, all green.

## What changed (summary)
- `GuessWho.Tests/` — new xUnit project added to the solution, 93 tests across two files:
  `GameSessionTests.cs` (game logic) and `GameSessionServiceTests.cs` (session lifecycle).
- `GuessWho/Services/GameSessionService.cs` — `SanitiseName()` private method strips HTML tags
  (compiled regex), trims whitespace, enforces max 20 chars, falls back to "Player" if empty.
  Called in both `CreateSession` and `JoinSession`.
- `GuessWho/wwwroot/favicon.svg` (new) — custom 32×32 SVG favicon: dark card background, gold
  face-card silhouette, bold "?" centred.
- `GuessWho/Components/App.razor` — SVG favicon `<link>` added; original PNG remains as fallback.
- `to-do-technical.md` — all items marked done, backlog empty.
- `project.md` — updated to Iteration 18, HTTP redirect decision documented.

## What to do next
Both `to-do.md` and `to-do-technical.md` are empty. The game is fully playable and complete
per the original specification.

Possible next directions (add to `to-do.md` if desired):
1. **Persistent leaderboard** — score across multiple sessions, stored in a file or lightweight DB.
2. **Spectator mode** — third-party observer watches a live game in read-only mode.
3. **Timer pressure mode** — configurable turn time limit (e.g. 30 s hard cap instead of 10 s soft).
4. **bUnit component tests** — test `FaceCard.razor` rendering, `Game.razor` turn-state display.
5. **Production deployment** — Docker container, Nginx reverse proxy, HTTPS at proxy layer.

No messages.
