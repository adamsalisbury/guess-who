# Guess Who? — Project State

## Overview
Browser-based two-player real-time implementation of the classic Guess Who? deduction game.
Two players take alternating turns asking yes/no questions to identify each other's secret Mystery Person.
First to 5 round wins takes the match.

## Architecture

### Stack
- **ASP.NET Core 8 + Blazor Server** (single project: `GuessWho/GuessWho.csproj`)
- **Interactive Server render mode** (`@rendermode InteractiveServer` on each page component)
- **No SignalR hub** — inter-player real-time communication uses a singleton `GameSessionService` with C# events; both Blazor circuits subscribe to `GameSession.StateChanged` and call `InvokeAsync(StateHasChanged)`. This is idiomatic Blazor Server and avoids a second WebSocket per client.
- **No external assets** — all visuals will be generated from attribute data (CSS/SVG)

### Solution layout
```
GuessWho/                  ← solution root (also git repo root)
├── GuessWho.sln
├── GuessWho/              ← single Blazor Server project
│   ├── Components/
│   │   ├── Pages/
│   │   │   ├── Landing.razor    ← "/" — name entry, New/Join Game
│   │   │   ├── Lobby.razor      ← "/lobby/{Code}" — wait for opponent
│   │   │   ├── Game.razor       ← "/game/{Code}" — main game board (placeholder)
│   │   │   ├── Gallery.razor    ← "/gallery" — dev utility: all 24 face cards
│   │   │   └── Gallery.razor.css
│   │   ├── Layout/
│   │   │   └── MainLayout.razor ← bare shell, no nav
│   │   ├── FaceCard.razor       ← character face card SVG component
│   │   ├── FaceCard.razor.css   ← scoped card styles (sm/md/lg sizes)
│   │   ├── App.razor
│   │   ├── Routes.razor
│   │   └── _Imports.razor
│   ├── Data/
│   │   └── CharacterData.cs     ← 24 static characters
│   ├── Models/
│   │   ├── Character.cs
│   │   ├── Enums.cs
│   │   ├── GameSession.cs
│   │   └── PlayerState.cs
│   ├── Services/
│   │   └── GameSessionService.cs  ← singleton, owns all active sessions
│   └── wwwroot/app.css            ← dark theme, gold accent, no Bootstrap dependency
├── project.md
├── to-do.md
├── to-do-technical.md
├── done.md
└── message-to-my-successor.md
```

### Player identity
Each player gets a GUID **token** generated on the landing page. It is passed through URL query parameters
(`?name=Alex&token=<guid>`) rather than session storage, keeping things simple for iteration 1.
A player's token is used to look up their `PlayerState` inside `GameSession`.

### Game codes
4 uppercase characters; alphabet excludes visually ambiguous O, 0, I, 1.
`GameSessionService` generates codes and stores sessions in a `ConcurrentDictionary<string, GameSession>`.

### Real-time between clients
`GameSession` exposes `event EventHandler? StateChanged`.
Components subscribe on `OnInitializedAsync`, unsubscribe in `Dispose()`.
When state changes (e.g. second player joins), the event fires on the server thread that made the change;
the other circuit's handler calls `InvokeAsync(StateHasChanged)` to marshal back to its own render thread.

## Current state (after Iteration 3)
- Landing page functional: name entry, New Game (creates session), Join Game (validates code, joins session)
- Lobby page functional: both players shown by name, connection status, "Waiting for opponent…" / "Starting…"
- Both players auto-navigate to `/game/{Code}` when lobby is full
- **Game page (`/game/{Code}`)**: full character selection flow implemented:
  - `CharacterSelection` phase + unconfirmed → 6-column picker grid of all 24 md face cards; hover/click
    highlights with IsMystery glow; non-selected cards dim when one is pending; sticky footer shows preview
    of selected card with "Confirm" and "Change" buttons
  - `CharacterSelection` phase + confirmed → waiting screen with player's Mystery Person revealed (lg),
    gold "Locked In!" heading, spinner, opponent name
  - `Playing` phase → placeholder card (ready for Iteration 4 game board)
  - `Game.razor.css`: scoped styles for selection-page, selection-grid, selection-footer, waiting-page,
    waiting-card; dark gold-gradient backgrounds, smooth opacity transitions on card dimming
- `GameSession.SelectMysteryPerson(token, characterId)`: validates token, sets `PlayerState.MysteryPersonId`,
  advances `Phase` to `Playing` and sets `RoundNumber = 1` when both players have confirmed; thread-safe
- `GameSessionService.SelectMysteryPerson(code, token, characterId)`: thin passthrough
- 24 characters defined in `CharacterData.All` with all required attributes
- **FaceCard component** (`Components/FaceCard.razor` + `.razor.css`): renders any character as an SVG face
  card driven purely by attribute data. Three sizes: `sm`, `md`, `lg`. States: normal, mystery, face-down.
- **Gallery page** (`/gallery`): dev utility — all 24 cards, size comparison, state comparison.
- Build: **0 errors, 0 warnings**

## Design decisions & known trade-offs
- No HTTPS redirect in dev (removed `app.UseHttpsRedirection()` from template to simplify local runs)
- Session cleanup not yet implemented — sessions persist until process restart
- No reconnect logic for dropped Blazor circuits (future: store token in sessionStorage, re-subscribe)
- Bootstrap CSS is included in the template but not actively used — custom CSS in `app.css` is the design system
