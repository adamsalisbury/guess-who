# Guess Who? — Completed Work Log

## Iteration 9 — Guessing mechanic & round-end overlay
**Completed**: 2026-02-21

### What was done

#### Server-side
- Added `RoundEndReason` enum (`CorrectGuess`, `WrongGuess`) to `Enums.cs`.
- Added `EndReason` (`RoundEndReason?`) and `RoundWinnerToken` (`string?`) properties to `GameSession`.
  - Property is named `EndReason` (not `RoundEndReason`) to avoid naming conflict with the enum type.
- Added `MakeGuess(callerToken, guessedCharacterId)` to `GameSession`:
  - Guards: phase must be Playing, caller must be active player.
  - Looks up opponent's `MysteryPersonId` and compares to the guessed ID.
  - Correct guess → winner = caller, caller's `RoundWins++`, `EndReason = CorrectGuess`.
  - Wrong guess → winner = opponent, opponent's `RoundWins++`, `EndReason = WrongGuess`.
  - Clears `CountdownStartedAt` (cancels any running countdown), sets `Phase = RoundEnd`, fires `StateChanged`.
- Added `StartNewRound(callerToken)` to `GameSession`:
  - Guards: phase must be RoundEnd, caller must be a session participant.
  - Increments `RoundNumber`, calls `ResetRoundState()`, sets `Phase = CharacterSelection`, fires `StateChanged`.
- Added `EndGame(callerToken)` to `GameSession`:
  - Guards: phase must be RoundEnd, caller must be a session participant.
  - Sets `Phase = GameEnd`, fires `StateChanged` — both clients navigate home in `OnSessionStateChanged`.
- Added private `ResetRoundState()` helper:
  - Clears `MysteryPersonId`, `EliminatedIds`, `BoardOrder` on both players.
  - Clears `_chatLog`, resets `QuestionAsked`, `CountdownStartedAt`, `ActivePlayerToken`, `RoundWinnerToken`, `EndReason`.
  - Championship `RoundWins` are NOT touched.
- Added `MakeGuess`, `StartNewRound`, `EndGame` passthrough methods to `GameSessionService`.

#### FaceCard component
- Added `IsGuessable` (`bool`) parameter to `FaceCard.razor`:
  - Adds `face-card--guessable` CSS class to the wrapper.
  - Updates `title` tooltip: "Click to guess this person".
- Added `.face-card--guessable` CSS to `FaceCard.razor.css`:
  - Blue border (`#58a6ff`) + stronger glow (`0 0 14px rgba(88,166,255,0.55)`) on hover.
  - Name turns blue. Card lifts 3px. Active state resets to 0.

#### Game.razor — Guess Mode
- Added `_guessModeActive` (bool) and `_pendingGuessId` (int?) state variables.
- Added `GetGuessCallback(characterId, canGuess)` helper using `EventCallback.Factory.Create` pattern.
- Opponent board: each face-up card gets `IsGuessable=true` and an `OnClick` → `SetPendingGuess(id)` when `_guessModeActive && _isMyTurn && !isOppEliminated`. Board header shows "— GUESS MODE" label (blue).
- Chat input area — new states:
  - Normal (my turn, no question, not in guess mode): shows question input + **"🎯 Make a Guess Instead"** button below.
  - Guess mode, no pending: shows blue hint text "🎯 Click a face on the opponent's board to guess" + "Cancel Guess Mode" button.
  - Guess mode, pending card: shows confirmation panel (`guess-confirm-panel`) with character name, warning text ("A wrong guess means you lose the round immediately"), **Confirm Guess** and **Cancel** buttons.
- New methods: `ActivateGuessMode()`, `CancelGuessMode()`, `SetPendingGuess(id)`, `ConfirmGuess()`.
- `ConfirmGuess()` calls `GameSessionService.MakeGuess(Code, MyToken, _pendingGuessId)` then clears local guess-mode state.
- `OnSessionStateChanged` clears `_guessModeActive` / `_pendingGuessId` when `!_isMyTurn || phase != Playing`.

#### Game.razor — Round-End Overlay
- Phase branch changed from `== GamePhase.Playing` to `is GamePhase.Playing or GamePhase.RoundEnd`.
- When `Phase == RoundEnd`, a `position: fixed` overlay (`round-end-overlay`) appears on top of the game board:
  - **Outcome heading** ("You win the round! 🎉" in green / "You lose the round" in red).
  - **Subtext** explaining the reason ("Alex guessed correctly!" / "Bernard guessed wrong — Alex wins!").
  - **Mystery Person reveal** — both players' Mystery People shown as `md` FaceCards with gold glow.
  - **Championship score** recap (same layout as score bar).
  - **New Round** (gold primary button) → calls `StartNewRound()` — first click wins (simplified; no consensus yet).
  - **End Game** (secondary button) → calls `LeaveGame()` → server sets `Phase = GameEnd` → both circuits navigate to `/`.
- `OnSessionStateChanged` now navigates home immediately when `Phase == GameEnd`.
- `OnSessionStateChanged` now includes `GamePhase.RoundEnd` in the chat auto-scroll condition.

#### CSS additions (Game.razor.css)
- `.guess-mode-btn`, `.guess-mode-hint`, `.guess-mode-active` — guess mode trigger and hint styles.
- `.guess-confirm-panel`, `.guess-confirm-name`, `.guess-confirm-warning`, `.guess-confirm-buttons` — confirmation panel.
- `.btn-guess-confirm`, `.btn-guess-cancel` — confirmation action buttons.
- `.round-end-overlay`, `.round-end-card` — fixed full-screen backdrop + animated modal card.
- `.round-end-outcome`, `.round-end-outcome--win/.--loss`, `.round-end-subtext`, `.round-end-divider` — overlay content.
- `.round-end-reveal`, `.round-end-reveal-slot`, `.round-end-reveal-label`, `.round-end-reveal-name` — Mystery Person reveal section.
- `.round-end-score`, `.round-end-score-name`, `.round-end-score-value`, `.round-end-score-dash` — score recap.
- `.round-end-actions` — action buttons row.

- Build result: **0 errors, 0 warnings**.

### Notes
- `StartNewRound` is first-click-wins (simplified). The full consensus mechanism (both players must agree, 60-second timeout) is iteration 2 in to-do.md.
- `EndReason` property avoids `RoundEndReason` naming conflict with the enum type in the same namespace.
- `GetGuessCallback` follows the same `EventCallback.Factory.Create` pattern as `GetEliminateCallback` to avoid Razor ternary type-inference issues.
- The overlay uses `position: fixed` and `backdrop-filter: blur(3px)` so it floats above the entire game board — no DOM restructuring needed.
- Face-down cards on the opponent's board are NOT guessable (guarding `!isOppEliminated`).
- The `GameEnd` navigation is handled in `OnSessionStateChanged` for both players' circuits simultaneously.

---

## Iteration 8 — Face elimination (own board)
**Completed**: 2026-02-21

### What was done
- Added `EliminateCharacter(callerToken, characterId)` to `GameSession`:
  - Guards: phase must be Playing, caller must be active player.
  - `MysteryPersonId` is immune (no-op if ID matches).
  - `EliminatedIds.Add()` is idempotent (HashSet returns false if already present).
  - Fires `StateChanged` on every successful elimination.
- Added `EliminateCharacter(code, token, characterId)` passthrough to `GameSessionService`.
- Updated `FaceCard.razor`:
  - New `IsEliminatable` parameter adds `.face-card--eliminatable` CSS class.
  - `title` attribute set to "Click to eliminate" when `IsEliminatable`.
- Added `.face-card--eliminatable` CSS to `FaceCard.razor.css`:
  - Red border + glow on hover overriding default blue.
  - Name tint red. Card lifts 2px. Active state resets.
- Updated `Game.razor`:
  - `GetEliminateCallback(characterId, canEliminate)` returns typed
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
