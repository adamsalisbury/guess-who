# Guess Who? — Done Log

Append-only chronological record. Most recent entry at the bottom.

---

## Iteration 1 — Landing page & lobby
**Completed**: 2026-02-20

### What was done
- Scaffolded ASP.NET Core 8 Blazor Server project (`GuessWho/GuessWho.csproj`) with a solution file.
- Defined all domain models: `Character`, `PlayerState`, `GameSession`, and supporting enums
  (`HairColor`, `EyeColor`, `HairLength`, `GamePhase`, `JoinResult`).
- Defined all 24 characters in `CharacterData.All` with full attribute sets (hair colour, eye colour,
  glasses, hat, facial hair, hair length, bald, rosy cheeks, big nose). Character data is ready for
  face card rendering in iteration 2.
- Implemented `GameSessionService` (singleton): creates sessions with unique 4-character codes
  (alphabet excludes O, 0, I, 1), thread-safe player slot assignment via `GameSession._lock`.
- Built **Landing page** (`/`): name entry (required, max 20 chars), "New Game" button (creates session,
  navigates to lobby), "Join Game" button (reveals code input, validates code exists, joins session,
  navigates to lobby). Error states for empty name, bad code, full session.
- Built **Lobby page** (`/lobby/{Code}`): displays both player names with connection status
  (green "Connected" / grey "Waiting…"), game code prominently displayed for sharing, animated
  waiting state with dot-pulse, instant "Starting…" confirmation when both players connect,
  auto-navigation to game page after 1.2s.
- Built **Game page** (`/game/{Code}`): placeholder showing player names and session state.
  Full board implementation deferred to later iterations.
- Wrote custom CSS (`wwwroot/app.css`): dark theme (#0d1117 base), gold accent (#f0a500),
  card-based layouts, smooth animations (spin, fadeSlideDown, dotPulse, popIn), no Bootstrap.
- Removed all template boilerplate (Counter, Weather, NavMenu).
- Build result: **0 errors, 0 warnings**.

### Notes
- Player identity uses URL query params (`?name=…&token=<guid>`). Token is generated on the landing
  page and passed forward. No sessionStorage or cookies needed for iteration 1.
- Inter-player real-time: `GameSession.StateChanged` C# event; both Blazor circuits subscribe.
  Player 2's `Lobby.razor` re-renders when Player 1's session state changes, then auto-navigates.
- Session cleanup not implemented (sessions live until process restart). Tracked in `to-do-technical.md`.
