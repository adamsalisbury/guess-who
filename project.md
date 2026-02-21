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

## Current state (after Iteration 10)
- Landing page functional: name entry, New Game (creates session), Join Game (validates code, joins session)
- Lobby page functional: both players shown by name, connection status, auto-navigation to game page
- Both players auto-navigate to `/game/{Code}` when lobby is full
- **Mystery Person selection** (CharacterSelection phase): picker grid, footer preview, confirm flow, waiting screen — all functional and real-time
- **Game board** (`/game/{Code}` Playing phase): full two-column desktop layout implemented:
  - **Left column** — two board sections stacked vertically, each independently scrollable:
    - **Opponent board (top, ~40%)**: 6-column `sm`-card grid in opponent's `BoardOrder`; face-up cards are
      guessable (blue hover) when the active player is in guess mode; "— GUESS MODE" label in header
    - **Own board (bottom, remaining)**: 6-column `md`-card grid in player's own `BoardOrder`; Mystery
      Person card has gold glow (`IsMystery`); header shows player's name
  - **Right column (340px)** — three stacked sections:
    - **Score bar**: Round number, championship score ("Alex 0 – 0 Bernard"), named turn indicator
      ("Your turn, [name]" gold/pulsing dot | "Waiting for [opponent]…" muted italic)
    - **Mystery Person panel**: `lg` FaceCard with gold glow, "Your Mystery Person" label, keep-secret hint
    - **Chat panel** (Iteration 6): live message log with per-kind styling; chat input area has 5 states:
      1. Active, no question, not guess mode → input + "🎯 Make a Guess Instead" button
      2. Active, no question, guess mode active, no pending → blue hint + "Cancel Guess Mode"
      3. Active, no question, guess mode active, pending card → confirmation panel + Confirm/Cancel
      4. Active, question asked → locked (Awaiting answer / Countdown / "end your turn")
      5. Inactive pending answer → Yes/No buttons; Inactive waiting → disabled input
- **Turn management** (Iteration 5): `GameSession.ActivePlayerToken`, `QuestionAsked`, `AwaitingAnswer` drive all turn-state logic
- **Guessing mechanic** (Iteration 9):
  - Active player clicks "🎯 Make a Guess Instead" → enters guess mode (blue hover on opponent's face-up cards)
  - Clicking a card in guess mode → confirmation panel ("A wrong guess means you lose the round immediately")
  - Confirm → `GameSessionService.MakeGuess(Code, MyToken, charId)` → phase transitions to `RoundEnd`
  - Correct guess: caller wins round, `RoundWins++`. Wrong guess: opponent wins round, `RoundWins++`
  - `MakeGuess` also sets `IsMatchOver = true` + `MatchWinnerToken` when winner reaches 5 wins
- **Round-end overlay with full consensus** (Iteration 10):
  - Fixed full-screen dark overlay with animated modal card shown to both players simultaneously
  - Outcome heading (green "You win the round! 🎉" or red "You lose the round")
  - **Match champion banner**: when `IsMatchOver`, shows "🏆 You win the match!" (green for winner,
    gold for loser)
  - Both Mystery People revealed as gold-glowing `md` FaceCards
  - Championship score recap
  - **Consensus mechanism**: both players must click the same option before it executes:
    - **New Round** / **Play Again** (gold) — `PostRoundDecision.NewRound`
    - **End Game** (secondary) — `PostRoundDecision.EndGame`
    - Decision chips show each player's choice (or "—") once either clicks; chosen chip glows gold
    - Buttons disable after clicking — no changing your mind
    - "Waiting for [opponent] to decide…" shown when you've decided but they haven't
    - Disagreement: "Waiting to agree… game ends automatically in 60s if unresolved"
    - 60-second server-side `System.Threading.Timer` defaults to EndGame on expiry
  - When `IsMatchOver` and both pick "Play Again": `ExecuteNewRound` resets both `RoundWins` to 0
- **Face elimination** (Iteration 8): active player clicks own board to flip faces down; Mystery Person immune. Opponent board syncs in real time.
- **Turn countdown** (Iteration 7): `GameSession.CountdownStartedAt` set by `AnswerQuestion()`; client-side 500ms timer drives display and auto-fires `StartNextTurn` after 10s (active player only).
- Build: **0 errors, 0 warnings**

## GameSession phase flow
```
Lobby → CharacterSelection → Playing ⇄ RoundEnd → GameEnd
                               ↑_________________________|  (ExecuteNewRound resets to CharacterSelection)
```

### Key GameSession methods
| Method | Phase guard | Effect |
|---|---|---|
| `AddPlayer` | Lobby | Adds player; P2 join → CharacterSelection |
| `SelectMysteryPerson` | CharacterSelection | Both chosen → Playing + shuffle boards |
| `AskQuestion` | Playing, active player | Posts question, sets `QuestionAsked` |
| `AnswerQuestion` | Playing, inactive player | Posts answer, starts countdown |
| `EliminateCharacter` | Playing, active player | Flips face on own board |
| `MakeGuess` | Playing, active player | Resolves round → RoundEnd; sets IsMatchOver on 5 wins |
| `MakePostRoundDecision` | RoundEnd | Records player's choice; executes on consensus or 60s timeout |
| `StartNextTurn` | Playing | Flips active player, resets per-turn state |
| `ExecuteNewRound` (private) | called from MakePostRoundDecision | Resets round → CharacterSelection; resets scores if IsMatchOver |
| `ExecuteEndGame` (private) | called from MakePostRoundDecision | → GameEnd (both clients navigate home) |

## Design decisions & known trade-offs
- No HTTPS redirect in dev (removed `app.UseHttpsRedirection()` from template to simplify local runs)
- Session cleanup not yet implemented — sessions persist until process restart
- No reconnect logic for dropped Blazor circuits (future: store token in sessionStorage, re-subscribe)
- Bootstrap CSS is included in the template but not actively used — custom CSS in `app.css` is the design system
- `GameSession` imports `GuessWho.Data` (for `CharacterData`) to populate `BoardOrder`. Acceptable for
  a single-project small game; would separate in a multi-project architecture.
- Chat auto-scroll uses `eval` JS interop (pragmatic). A proper JS module can replace it in a polish iteration.
- `_postRoundTimeoutTimer` fires on a thread-pool thread; its callback acquires `_lock` before reading `Phase`.
- `GetEliminateCallback` and `GetGuessCallback` both use `EventCallback.Factory.Create<Character?>` to avoid Razor ternary type-inference compiler errors.
- `EndReason` property name avoids naming conflict with the `RoundEndReason` enum type in the same namespace.
- `GetPostRoundDecision(token)` reads `_postRoundDecisions` without a lock — safe because reads happen after
  `StateChanged` fires (post-mutation) and the game has at most 2 players on 2 threads.
