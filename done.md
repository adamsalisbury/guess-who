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

---

## Iteration 2 — Character data & face card rendering
**Completed**: 2026-02-20

### What was done
- Created **`FaceCard.razor`** (`Components/FaceCard.razor` + `FaceCard.razor.css`): a reusable Blazor
  component that renders any of the 24 characters as a stylised SVG portrait derived entirely from
  attribute data — no external image files.
  - **Hair**: non-bald characters get a hair-cap ellipse (colour-matched to attribute) with the face
    ellipse overlaid on top. Long-haired characters also get narrow side panels extending below the face.
    Bald characters skip the hair cap entirely.
  - **Eyes**: white sclera ellipses, coloured iris circles, dark pupils, and bright highlights.
  - **Eyebrows**: arched path coloured by hair colour.
  - **Nose**: subtle curved path normally; wider curved path + tinted oval for big-nose characters.
  - **Mouth**: drawn only when no facial hair (beard visually covers the mouth area).
  - **Rosy cheeks**: semi-transparent pink ellipses on both cheeks.
  - **Facial hair**: closed moustache path + beard ellipse covering lower face, coloured by hair attribute.
  - **Glasses**: two rounded-rect lens frames with bridge and arms in silvery blue.
  - **Hat**: dark crown rect + contrasting band + wider brim rect, drawn last so it sits on top of hair.
  - **Face-down state**: dark card with a grey cross — used for eliminated characters.
  - Three CSS size variants: `sm` (72 px art, for opponent grid), `md` (100 px, own board),
    `lg` (148 px, Mystery Person display).
  - `IsMystery="true"` adds a gold border glow. Optional `OnClick` callback for future interactivity.
- Created **`Gallery.razor`** (`/gallery`): dev-utility page showing all 24 characters in a 6×4 `md`
  grid with small attribute-chip summaries per card, a size-comparison section (sm/md/lg of same
  character), and a card-states section (normal / mystery / face-down).
- Build result: **0 errors, 0 warnings**.

### Notes
- The gallery page is accessible at `/gallery` by URL — no navigation link in the game UI.
  It exists only for visual QA and developer inspection.
- All visual information encoded purely in SVG. Characters are meaningfully distinct at all three sizes.
- FaceCard uses Blazor CSS isolation (`.razor.css`). The SVG scales via `width:100%; height:auto`.
- Hat is always drawn last in SVG painter order, so it correctly sits on top of hair.

---

## Iteration 3 — Mystery Person selection
**Completed**: 2026-02-20

### What was done
- Added `GameSession.SelectMysteryPerson(string token, int characterId)`: validates player token,
  sets `PlayerState.MysteryPersonId`, and when both players have confirmed advances `Phase` to
  `Playing` and sets `RoundNumber = 1`. All state changes inside the session `_lock`; fires
  `StateChanged` so both circuits re-render immediately.
- Added `GameSessionService.SelectMysteryPerson(code, token, characterId)`: thin passthrough to
  the session method, consistent with the service's existing API style.
- Rewrote `Game.razor` from a placeholder into a three-screen state machine:
  1. **Picker screen** (phase = `CharacterSelection`, player not yet confirmed): 6-column grid of
     all 24 face cards at `md` size. Clicking a card sets it as pending (IsMystery glow); all other
     cards dim to 35% opacity. Sticky footer bar shows a `sm` preview of the selected card, player
     name, "Confirm" button, and "Change" button. Confirmation calls `SelectMysteryPerson`.
  2. **Waiting screen** (phase = `CharacterSelection`, player confirmed): displays the chosen
     Mystery Person at `lg` size with gold glow, "Locked In!" heading, opponent's name, and a
     CSS spinner. Transitions automatically when the other player confirms (StateChanged event).
  3. **Playing placeholder** (phase = `Playing`): card with both player names and round number,
     ready to be replaced by the full board in Iteration 4.
- Created `Game.razor.css`: scoped styles for `.selection-page` (flex-column, full-height),
  `.selection-grid` (6-column CSS grid, 100px columns), `.selection-card-wrap--dimmed` (opacity
  0.35 transition), `.selection-footer` (sticky bar), `.waiting-page`, `.waiting-card` (centered
  panel with fadeIn animation). All colours use existing design tokens from `app.css`.
- `Game.razor` implements `IDisposable` and unsubscribes `StateChanged` in `Dispose()` to prevent
  memory leaks and cross-circuit ghost renders.
- Build result: **0 errors, 0 warnings**.

### Notes
- Selection is simultaneous and independent — neither player knows what the other has chosen.
- `_pendingId` is local component state; it is never persisted to the session. Only `MysteryPersonId`
  on `PlayerState` is the confirmed server-side truth.
- The `PlayerState.MysteryPersonId.HasValue` flag drives the picker/waiting branch; no separate
  local "confirmed" boolean needed.
- Phase transition (CharacterSelection → Playing) fires a single `StateChanged`, causing both
  circuits' `OnSessionStateChanged` handlers to call `InvokeAsync(StateHasChanged)` simultaneously,
  so both players see the Playing screen at essentially the same instant.

---

## Iteration 4 — Game board layout
**Completed**: 2026-02-20

### What was done
- Added `BoardOrder: List<int>` to `PlayerState` — a per-player randomly-shuffled list of all 24
  character IDs. This determines the visual order of cards on each player's board, independently
  randomised so both players see a different arrangement.
- Added `GameSession.ShuffleBoardOrders()` (private, called inside `_lock`): uses
  `Random.Shared.Shuffle(Span<T>)` on a cloned `int[]` of all character IDs to independently
  shuffle both players' board orders. Called exactly once when phase transitions to Playing.
- Added `using GuessWho.Data;` to `GameSession.cs` so it can access `CharacterData.All`.
- Replaced the Playing-phase placeholder in `Game.razor` with the full two-column game board:
  - **Left column** (`flex: 1`, `flex-direction: column`):
    - **Opponent board (top, `flex: 0 0 40%`)**: header with opponent name; 6-column `sm` card
      grid in opponent's `BoardOrder`; `FaceDown` driven by `_opponent.EliminatedIds`; read-only.
    - **1px divider**.
    - **Own board (bottom, `flex: 1`)**: header with player's own name; 6-column `md` card grid
      in player's own `BoardOrder`; Mystery Person card gets `IsMystery="true"` (gold glow);
      eliminated cards get `FaceDown="true"`. No `OnClick` yet (flip mechanic is Iteration 5).
  - **Right column** (`width: 340px`, `flex-direction: column`, dark surface background):
    - **Score bar**: round number, championship score with player names and gold numerals,
      "Game in progress…" turn status placeholder.
    - **Mystery Person panel**: `lg` FaceCard (gold glow), "Your Mystery Person" label,
      keep-secret hint. Hidden from opponent (each player only sees their own `_me` data).
    - **Chat panel** (`flex: 1`): scrollable log ("The game is afoot…" placeholder, messages
      pin to bottom); disabled text input + Send button.
- Added `Game.razor.css` board layout section: `.game-board`, `.game-left`, `.board-section`,
  `.board-section--own`, `.board-divider`, `.board-header`, `.board-grid-area`, `.board-grid`,
  `.board-grid--sm` (6×78px cols, 6px gap), `.board-grid--md` (6×108px cols, 8px gap),
  `.game-right`, `.score-bar`, `.score-display`, `.score-name`, `.score-value`, `.score-dash`,
  `.turn-status`, `.mystery-panel`, `.mystery-label`, `.mystery-card-wrap`, `.mystery-keep-secret`,
  `.chat-panel`, `.chat-header`, `.chat-log`, `.chat-placeholder`, `.chat-input-area`,
  `.chat-input`, `.chat-send-btn`.
- Build result: **0 errors, 0 warnings**.

### Notes
- Both board grid areas use `overflow-y: auto` — each scrolls independently.
- `min-height: 0` on `.board-section` and `.chat-panel` is essential for flex-in-flex overflow to work.
- `justify-content: center` on `.board-grid` centres the 6-column track within the left column;
  cards are left-aligned within each fixed-width cell.
- `_me` is set once in `OnInitialized` and never re-assigned — since `PlayerState` is a reference type,
  `_me.BoardOrder` automatically reflects server-side mutations to the same object.
- The score bar displays `Player1?.RoundWins` and `Player2?.RoundWins` (both 0 at game start).
  Championship scoring wired in Iteration 8.

---

## Iteration 5 — Turn management
**Completed**: 2026-02-21

### What was done
- Added `ActivePlayerToken` (string, private set) to `GameSession`. Set to `Player1.Token` when phase
  transitions to Playing. Never null during Playing phase.
- Added `QuestionAsked` (bool, private set) to `GameSession`. Resets to `false` on each `StartNextTurn`.
  Not wired to chat UI yet — chat flow wired in Iteration 6.
- Added `IsActivePlayer(string token)` helper — null/empty safe, compares token to `ActivePlayerToken`.
- Added `StartNextTurn(string callerToken)` inside `_lock`: no-ops if caller is not active player
  (prevents double-fire from stale callbacks), flips `ActivePlayerToken` between P1/P2, resets
  `QuestionAsked`, calls `NotifyStateChanged()`.
- Added `GameSessionService.StartNextTurn(code, token)` passthrough.
- `Game.razor`: added `_isMyTurn` computed property (reads `_session.IsActivePlayer(MyToken)`).
  Turn status in score bar replaced with named indicator:
  - Active: pulsing gold dot + "Your turn, [name]" (`.turn-status--active`, gold, bold)
  - Inactive: "Waiting for [opponent]…" (`.turn-status--waiting`, muted italic)
  Chat input and Send button: `disabled="@(!_isMyTurn)"`. Placeholder text changes to
  "Waiting for your turn…" when inactive.
  End Turn button rendered only for active player; calls `EndTurn()` → `StartNextTurn`.
- `Game.razor.css`: `.turn-status--active`, `.turn-status--waiting`, `.turn-indicator-dot`
  (7px pulsing gold dot using existing `dotPulse` keyframe from `app.css`). Chat input area
  refactored to `flex-direction: column` with inner `.chat-input-row` for the input + send pair;
  `.end-turn-btn` with gold border/text hover state.
- Build result: **0 errors, 0 warnings**.

### Notes
- `_isMyTurn` is a computed property, not a field — re-evaluated on every render. Since `StateChanged`
  fires `InvokeAsync(StateHasChanged)`, both circuits re-render after `StartNextTurn`, so the turn
  indicator updates immediately for both players simultaneously.
- No countdown timer yet — that is Iteration 3 of turn mechanics (currently item 3 in to-do.md).
  For this iteration, only the End Turn button passes the turn.
- `QuestionAsked` is tracked on the server but not yet connected to chat UI — fully wired in Iteration 6.

---

## Iteration 6 — Chat panel & question flow
**Completed**: 2026-02-21

### What was done
- Added `ChatMessageKind` enum (`Question`, `Answer`, `System`) to `Enums.cs`.
- Created `Models/ChatMessage.cs`: `SenderName`, `Text`, `Kind` — immutable record-style class.
- Updated `GameSession`:
  - `_chatLog: List<ChatMessage>` (private), exposed as `IReadOnlyList<ChatMessage> ChatLog`.
  - `AwaitingAnswer` computed property: `true` when `QuestionAsked` but the last log entry is not an `Answer`.
  - `AskQuestion(callerToken, text)`: posts a Question to the log, sets `QuestionAsked = true`. No-ops if caller is not active player, question already asked, or text is empty.
  - `AnswerQuestion(callerToken, yes)`: posts an Answer to the log. No-ops if caller is active player or `AwaitingAnswer` is false.
  - `ShuffleBoardOrders()` now clears `_chatLog` at round start.
- Added `AskQuestion` and `AnswerQuestion` passthroughs to `GameSessionService`.
- Rewrote the chat panel in `Game.razor`:
  - Injected `IJSRuntime JS` for auto-scroll.
  - Real chat log rendered from `_session.ChatLog` with per-kind CSS classes; placeholder shows when empty.
  - Input area branches across four turn states:
    1. Active, no question yet → text input + Send button (Enter or click)
    2. Active, question sent, awaiting answer → "Waiting for [opponent] to answer…"
    3. Active, question answered → "Question answered — end your turn when ready."
    4. Inactive, question awaiting answer → Yes / No buttons with asker's name
    5. Inactive, nothing pending → disabled text input (waiting indicator)
  - `SendQuestion()`: trims input, calls `AskQuestion`, clears field, scrolls log.
  - `RespondToQuestion(bool yes)`: calls `AnswerQuestion`, scrolls log.
  - `OnChatKeyDown`: submits on Enter.
  - `ScrollChatToBottom()`: `eval` JS interop, swallows failures.
  - `OnSessionStateChanged` now also auto-scrolls the log on every state change during Playing phase.
- Added CSS in `Game.razor.css`:
  - `.chat-msg` with `--question` (gold left border, gold sender), `--answer` (green right border, right-aligned, green sender), `--system` (muted italic centred) variants.
  - `.chat-yn-row`, `.chat-yn-prompt`, `.chat-yn-buttons`, `.btn-yes` (green), `.btn-no` (red).
  - `.chat-awaiting` (muted italic status line).
- Build result: **0 errors, 0 warnings**.

### Notes
- `AwaitingAnswer` is a pure derived property on the server — no extra state field needed.
- The Yes/No buttons are shown to the INACTIVE player only (`!_isMyTurn && _session.AwaitingAnswer`).
- `QuestionAsked` stays `true` after the answer is received — this keeps the active player's input
  locked for the rest of their turn. It resets in `StartNextTurn` (on turn end).
- Auto-scroll uses `eval` JS interop — pragmatic for iteration 6; can be replaced with a proper
  JS module in a later polish pass.
- No countdown timer yet — that is Iteration 8 (turn end mechanics with 10-second countdown).

---

## Iteration 8 — Face elimination (own board)
**Completed**: 2026-02-21

### What was done
- Added `GameSession.EliminateCharacter(callerToken, characterId)`:
  - Guarded on Playing phase, active player only, Mystery Person immunity (`MysteryPersonId` check), and
    idempotency (`HashSet.Add` returns false if already present — double-click safe).
  - Fires `NotifyStateChanged()` on every successful elimination so both circuits re-render immediately.
- Added `GameSessionService.EliminateCharacter(code, token, characterId)` thin passthrough.
- Updated `FaceCard.razor`:
  - New `IsEliminatable` boolean parameter (default: false). Adds `.face-card--eliminatable` CSS class
    and tooltip `"Click to eliminate"` when true.
- Updated `FaceCard.razor.css`:
  - `.face-card--eliminatable:hover` overrides the default blue interactive highlight with a red border
    (`#e05555`), red box-shadow, red name tint, and a 2px lift animation. Uses `!important` to override
    the generic `[style*="cursor:pointer"]:hover` blue rule.
- Updated `Game.razor` own board foreach:
  - `canEliminate = _isMyTurn && !isMystery && !isEliminated` computed per card.
  - `GetEliminateCallback(int characterId, bool canEliminate)`: returns a properly typed
    `EventCallback<Character?>` using `EventCallback.Factory.Create` when active; avoids Razor ternary
    lambda type-inference issues.
  - `EliminateCard(int characterId)`: client-side guard (`_isMyTurn && MyToken is not null`) then
    calls `GameSessionService.EliminateCharacter`. Server independently guards again.
  - Mystery Person card: `IsEliminatable=false`, `OnClick=default` — gold glow communicates immunity.
  - Already-eliminated cards: `IsEliminatable=false`, `OnClick=default`, `FaceDown=true` — read-only.
- Board header counts updated dynamically:
  - Own board: "24 remaining" → "N remaining · M eliminated" once eliminations begin.
  - Opponent board: "24 remaining" → "M eliminated" once they start flipping.
- Opponent elimination sync: `_opponent.EliminatedIds` is read on every render; since `StateChanged`
  fires on each elimination, both circuits re-render immediately and the opponent's top board flips
  in real time without any additional code.
- Build result: **0 errors, 0 warnings**.

### Notes
- Elimination is only available to the active player (turn guard on both client and server).
  During the post-answer countdown, `ActivePlayerToken` still belongs to the active player, so
  elimination remains available throughout the countdown window — consistent with the spec.
- `EliminatedIds` is a `HashSet<int>` on `PlayerState` — O(1) add and contains checks, safe
  double-click protection. Server-side `Add()` returns false if already present.
- No separate "opponent sync" code was needed — iteration 9 (opponent elimination sync) is effectively
  done as a free consequence of how the event pattern works.

---

## Iteration 7 — Turn end mechanics
**Completed**: 2026-02-21

### What was done
- Added `CountdownStartedAt` (`DateTime?`, private set) and derived `CountdownActive` (`bool`) to
  `GameSession`. Set in `AnswerQuestion()` immediately after posting the answer; cleared in
  `StartNextTurn()`. Exposed `CountdownSeconds = 10` as a public constant so clients can mirror it.
- Added `_countdownTimer` (`System.Threading.Timer?`) and `CountdownRemaining` (computed, ceil-clamped)
  to `Game.razor`.
- `StartCountdownTimer()` creates a 500ms-interval timer. Each tick:
  1. Checks `CountdownActive`; if false, disposes the timer and triggers a final render.
  2. Computes elapsed time. If ≥ 10s **and** `_isMyTurn`, calls `StartNextTurn` (only the active
     player's circuit fires this — prevents double-fire from both circuits).
  3. Calls `InvokeAsync(StateHasChanged)` to update the countdown display.
  - Wrapped in try/catch for `ObjectDisposedException` and general exceptions — safe if component
    is disposed while timer is running.
- `OnSessionStateChanged` calls `StartCountdownTimer()` whenever `_session.CountdownActive == true`.
  Because the timer is disposed-and-recreated on each call, and `CountdownStartedAt` is server-side
  truth, the display is always accurate regardless of how many times the timer is restarted.
- `Dispose()` now disposes `_countdownTimer` before unsubscribing `StateChanged`.
- **Score bar**: a `countdown-bar` div appears below the turn-status row when `CountdownActive` is
  true. Shows "⏱ Turn ending in Xs…" to **both** players simultaneously (gold-tinted, animated popIn).
- **Chat input area**: when the active player's question has been answered and countdown is ticking,
  the awaiting message shows "Turn ends in Xs — or end it now." (gold-coloured inline count).
- Build result: **0 errors, 0 warnings**.

### Notes
- `CountdownStartedAt` is written under `_lock` (inside `AnswerQuestion` / `StartNextTurn`) but read
  freely from the timer callback — safe because `DateTime?` reads are atomic on 64-bit CLR.
- `StartNextTurn` retains its no-op guard (`callerToken != ActivePlayerToken`) which prevents any
  accidental double-fire even if both circuits' timers reach elapsed ≥ 10 at the same instant.
- The inactive player's circuit also starts the countdown timer for display. It never calls
  `StartNextTurn` because `_isMyTurn` is false for them.
- The `dueTime: 0` causes the timer to fire immediately on creation — first render of the countdown
  display therefore happens with no perceivable delay after the answer arrives.
