# Guess Who? — Completed Work Log

## Iteration 12 — Suggested questions UI
**Completed**: 2026-02-21

### What was done

#### Game.razor — markup
- Added a **"💡 Suggest a question"** toggle button in the chat input area, visible only when it is the
  active player's turn, no question has been asked, and guess mode is not active (same conditions as the
  question input itself).
- Toggle button sits between the chat input row and the "Make a Guess Instead" button.
- Clicking the toggle sets `_suggestionsOpen = !_suggestionsOpen`. Button label changes between collapsed
  and open states via the `--open` modifier class; a small ▲/▼ arrow and `title` attribute reinforce the
  state visually.
- When open, renders a `<div class="suggestions-panel">` containing 14 `<button class="question-chip">`
  elements — one per attribute value in the character set:
  - Glasses, hat, facial hair, long hair, bald, rosy cheeks, big nose, blue eyes, brown eyes, and all
    five hair colours (blonde, red, white, black, brown).
- Clicking any chip calls `SelectSuggestedQuestion(text)`: sets `_chatInput = text` and closes the panel.
  The input stays editable so the player can tweak the wording before sending.
- `_suggestionsOpen` is reset to `false` in three places: `SendQuestion()` (after send), `ToggleSuggestions()`
  (toggled off by the user), and `OnSessionStateChanged()` (alongside guess mode on turn change).

#### Game.razor — @code
- Added `private bool _suggestionsOpen` state variable.
- Added `private static readonly string[] SuggestedQuestions` — a static 14-item array covering all
  player-visible character attributes. Static so it is allocated once and never re-created on re-render.
- Added `ToggleSuggestions()` — flips `_suggestionsOpen`.
- Added `SelectSuggestedQuestion(string questionText)` — sets `_chatInput` and closes the panel.
- `SendQuestion()` now resets `_suggestionsOpen = false` after submitting.
- `OnSessionStateChanged()` now resets `_suggestionsOpen = false` alongside `_guessModeActive`.

#### Game.razor.css — new rules
- `.suggestions-toggle` — full-width dashed-border toggle button; neutral colour, gold on hover/open.
- `.suggestions-toggle--open` — solid border + gold tint when expanded.
- `.suggestions-toggle-icon` / `.suggestions-toggle-arrow` — emoji and chevron fragments.
- `.suggestions-panel` — flex-wrap container; `popIn` animation on entry.
- `.question-chip` — small 999px-radius pill; gold background + border tint at rest; deeper gold on hover
  with a 1px lift; snaps back on active.

- Build result: **0 errors, 0 warnings**.

### Notes
- No server-side changes were required — the feature is entirely client-side UI.
- `SuggestedQuestions` is `static readonly` to ensure it's not re-allocated on every component render.
- `_suggestionsOpen` is intentionally not persisted across turn changes; it resets for a fresh UX each turn.
- Chips use `white-space: nowrap` so long question strings don't wrap mid-chip; the flex container wraps
  at chip boundaries instead, giving a natural newspaper-style layout.
- "Brown eyes" was added to complement "blue eyes" — both eye colours in the character set now have a chip.

---

## Iteration 11 — Chat log readability
**Completed**: 2026-02-21

### What was done

#### Game.razor — chat log rendering
- Replaced the simple `@foreach` message loop with a richer rendering driven by `GetChatEntries()`.
- `GetChatEntries()` annotates each `ChatMessage` with a `TurnNumber` (1-based count of `Question`
  messages seen so far; shared by Q and the A that follows it). Computed once per render pass — no
  stateful counter in Razor markup.
- `ChatEntry` is a private `sealed record` inside the component's `@code` block.
- **Turn boundary dividers**: before every `Question` message after the first, a
  `<div class="chat-turn-divider">` is rendered. It shows "Turn N" as a centred label flanked by
  two `::before`/`::after` horizontal rules. Purely client-side — no server changes needed.
- **Question bubble**: header row shows sender name (left, gold uppercase) + compact turn tag
  `T1 / T2 / …` (right, dim gold mono). Text below. `border-radius: 2px 8px 8px 8px` gives a
  subtle top-left notch that implies a left-pointing tail.
- **Answer bubble**: large bold "Yes" / "No" (`1.5rem`, 900 weight, green) on top; sender name
  (small, green, centred) below. Right-aligned. `border-radius: 8px 2px 8px 8px` — mirrored notch.
- **System messages**: pill-shaped (`border-radius: 999px`), very subtle border, muted italic text.
  No sender label shown — system events speak for themselves.

#### Game.razor.css — new/updated rules
- **New**: `.chat-turn-divider`, `.chat-turn-divider::before`/`::after`, `.chat-turn-label` —
  the "Turn N" horizontal-rule separator.
- **New**: `.chat-msg-header` — flex header for Q bubble (sender left, turn tag right).
- **New**: `.chat-msg-turn-tag` — dim gold mono "T1" label.
- **Updated**: `.chat-msg--question` — gold border + tint, flat top-left corner, `max-width: 94%`.
- **Updated**: `.chat-msg--answer` — green border + tint, flat top-right corner, `max-width: 50%`,
  centred padding.
- **New**: `.chat-msg-answer-text` — `1.5rem / 900 weight` Yes/No in green.
- **Updated**: `.chat-msg--system` — pill shape, near-invisible border, muted italic.
- **Updated**: `.chat-msg-sender` — `display: block` added; answer sender now `0.63rem` centred.
- **Updated**: `.chat-msg--system .chat-msg-text` — scoped muted colour rule.

- Build result: **0 errors, 0 warnings**.

### Notes
- `GetChatEntries()` runs O(n) over the chat log (max ~24–50 entries per round) — negligible cost.
- `TurnNumber` for `Answer` and `System` messages reflects the current question count at the time
  the message was added (i.e. the turn whose question was just answered). This is fine for display
  — answers and dividers are associated with the correct exchange.
- No server-side `ChatMessage` model changes were made; all annotations are UI-layer only.

---

## Iteration 10 — End-of-round overlay consensus & post-game flow
**Completed**: 2026-02-21

### What was done

#### Server-side
- Added `PostRoundDecision` enum (`NewRound`, `EndGame`) to `Enums.cs`.
- Added to `GameSession`:
  - `const int WinsToWin = 5` — match win threshold.
  - `IsMatchOver` (bool), `MatchWinnerToken` (string?) — set in `MakeGuess` when the round winner
    reaches 5 wins.
  - `_postRoundDecisions` (Dictionary<string, PostRoundDecision>) — private per-player decision store.
  - `_postRoundTimeoutTimer` (System.Threading.Timer?) — fires 60s after the first decision; defaults
    to EndGame if consensus not reached.
  - `MakePostRoundDecision(callerToken, decision)`: records the caller's decision; starts the 60s timer
    on the first call; executes immediately if both players chose the same option.
  - `GetPostRoundDecision(token)` → `PostRoundDecision?`: read-only accessor for the overlay UI.
  - `ExecuteNewRound()` (private): resets both `RoundWins` to 0 if `IsMatchOver` (Play Again), then
    increments `RoundNumber`, calls `ResetRoundState()`, advances to `CharacterSelection`.
  - `ExecuteEndGame()` (private): sets `Phase = GameEnd`, fires `StateChanged`.
  - Updated `ResetRoundState()`: clears `_postRoundDecisions`, disposes the timer, resets `IsMatchOver`
    and `MatchWinnerToken`.
  - Updated `MakeGuess()`: after incrementing `RoundWins`, checks if winner >= `WinsToWin` and sets
    `IsMatchOver = true` / `MatchWinnerToken`.
  - Removed `StartNewRound(callerToken)` and `EndGame(callerToken)` (first-click-wins — replaced).
- Updated `GameSessionService`:
  - Added `MakePostRoundDecision(code, token, decision)` passthrough.
  - Removed `StartNewRound` and `EndGame` passthroughs.

#### Game.razor — code section
- Replaced `NewRound()` and `LeaveGame()` with `MakeDecision(PostRoundDecision decision)`:
  calls `GameSessionService.MakePostRoundDecision(Code, MyToken, decision)`.

#### Game.razor — round-end overlay markup
- **Match champion banner**: shown at top of overlay when `_session.IsMatchOver`. Winner sees green
  "🏆 You win the match!"; loser sees gold "🏆 [WinnerName] wins the match!".
- **Button label**: "Play Again" when `IsMatchOver`, "New Round" otherwise (same underlying
  `PostRoundDecision.NewRound` value — server handles score reset).
- **Decision chips** (`round-end-decisions`): two chips appear once either player has clicked.
  Each chip shows the player's name and their chosen option (or "—"). Chosen chips glow gold.
- **Status messages**: "Waiting for [opponent] to decide…" (you decided, they haven't) or
  "Waiting to agree… game ends automatically in 60s if unresolved" (both decided, different choices).
- **Button state**: both buttons disabled after this player clicks. Unchosen button fades to 28%
  opacity; chosen button stays at full opacity.

#### Game.razor.css — new rules
- `.round-end-match-banner` / `--win` / `--loss` — champion banner with popIn animation.
- `.round-end-decisions`, `.decision-chip` / `--chosen` — per-player decision chip row.
- `.decision-chip-name`, `.decision-chip-choice` — chip content typography.
- `.round-end-waiting` — consensus status message (italic, muted).
- `.round-end-actions .btn:disabled:not(.btn--chosen)` — unchosen disabled button at 28% opacity.

- Build result: **0 errors, 0 warnings**.

### Notes
- The 60s timeout callback acquires `_lock` before checking `Phase` — safe for timer thread-pool use.
- `GetPostRoundDecision` reads the private dictionary without a lock. Reads happen on the render thread
  after `StateChanged` fires (post-mutation) so race conditions are not a concern at this scale.
- Play Again does not need a separate enum value: `ExecuteNewRound` detects `IsMatchOver` and resets
  scores before calling `ResetRoundState`, which clears `IsMatchOver` for the fresh match.
- If a player disconnects during RoundEnd before deciding, the 60s timer will eventually fire and
  default to EndGame. This is acceptable for now; a `PlayerDisconnected` signal would be the upgrade path.

---

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
