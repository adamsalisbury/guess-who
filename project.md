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
- **No external assets** — all visuals generated from attribute data (CSS/SVG)

### Solution layout
```
GuessWho/                  ← solution root (also git repo root)
├── GuessWho.sln
├── GuessWho/              ← single Blazor Server project
│   ├── Components/
│   │   ├── Pages/
│   │   │   ├── Landing.razor    ← "/" — name entry, New/Join Game
│   │   │   ├── Lobby.razor      ← "/lobby/{Code}" — wait for opponent
│   │   │   ├── Game.razor       ← "/game/{Code}" — full game board
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

## Current state (after Iteration 6)
- Landing page functional: name entry, New Game (creates session), Join Game (validates code, joins session)
- Lobby page functional: both players shown by name, connection status, auto-navigation to game page
- Both players auto-navigate to `/game/{Code}` when lobby is full
- **Mystery Person selection** (CharacterSelection phase): picker grid, footer preview, confirm flow, waiting screen — all functional and real-time
- **Game board** (`/game/{Code}` Playing phase): full two-column desktop layout implemented:
  - **Left column** — two board sections stacked vertically, each independently scrollable:
    - **Opponent board (top, ~40%)**: 6-column `sm`-card grid in opponent's `BoardOrder`; all face-up;
      header shows opponent's name; read-only
    - **Own board (bottom, remaining)**: 6-column `md`-card grid in player's own `BoardOrder`; Mystery
      Person card has gold glow (`IsMystery`); header shows player's name
  - **Right column (340px)** — three stacked sections:
    - **Score bar**: Round number, championship score ("Alex 0 – 0 Bernard"), named turn indicator
      ("Your turn, [name]" gold/pulsing dot | "Waiting for [opponent]…" muted italic)
    - **Mystery Person panel**: `lg` FaceCard with gold glow, "Your Mystery Person" label, keep-secret hint
    - **Chat panel** (Iteration 6): live message log with per-kind styling; four-state input area
      (active pre-question → input; active awaiting answer → locked + "Waiting…"; active post-answer →
      locked + "End your turn"; inactive pending → Yes/No buttons; inactive waiting → disabled input).
      Enter key submits. Auto-scroll on new messages via JS interop.
- **Turn management** (Iteration 5):
  - `GameSession.ActivePlayerToken`: token of the current active player; set to Player1 on phase→Playing
  - `GameSession.QuestionAsked`: `true` after question sent; resets in `StartNextTurn`
  - `GameSession.AwaitingAnswer`: derived — true if `QuestionAsked` and last log entry is not an Answer
  - `GameSession.AskQuestion(token, text)`: posts Question to log, sets `QuestionAsked`
  - `GameSession.AnswerQuestion(token, yes)`: posts Answer to log; locked to inactive player only
  - `GameSession.StartNextTurn()`: flips turn between players under `_lock`, resets `QuestionAsked`
  - `_isMyTurn` computed property in `Game.razor` drives all conditional UI
  - "End Turn" button passes turn immediately; both circuits re-render via `StateChanged`
- **Chat model** (Iteration 6):
  - `Models/ChatMessage.cs`: `SenderName`, `Text`, `Kind (ChatMessageKind)`
  - `Models/Enums.cs`: `ChatMessageKind { Question, Answer, System }`
  - `GameSession.ChatLog`: `IReadOnlyList<ChatMessage>` — cleared at round start
- `PlayerState.BoardOrder`: `List<int>` of all 24 character IDs in a player-specific random shuffle;
  populated by `GameSession.ShuffleBoardOrders()` when phase transitions CharacterSelection → Playing.
  Each player's board order is independently shuffled.
- `GameSession.ShuffleBoardOrders()`: private helper; uses `Random.Shared.Shuffle(Span<T>)` on a cloned
  array; called once inside the `_lock` at the moment of phase transition; also clears `_chatLog`.
- Build: **0 errors, 0 warnings**

## Design decisions & known trade-offs
- No HTTPS redirect in dev (removed `app.UseHttpsRedirection()` from template to simplify local runs)
- Session cleanup not yet implemented — sessions persist until process restart
- No reconnect logic for dropped Blazor circuits (future: store token in sessionStorage, re-subscribe)
- Bootstrap CSS is included in the template but not actively used — custom CSS in `app.css` is the design system
- `GameSession` imports `GuessWho.Data` (for `CharacterData`) to populate `BoardOrder`. Acceptable for
  a single-project small game; would separate in a multi-project architecture.
- Chat auto-scroll uses `eval` JS interop (pragmatic). A proper JS module can replace it in a polish iteration.
- No countdown timer yet — implemented in Iteration 8 (turn end mechanics)
