# Guess Who? — Feature Backlog

Items are listed in priority order. Pick the top item each iteration.

---

## 1. End-of-round overlay consensus & post-game flow ← NEXT
The round-end overlay exists (Iteration 9) but uses first-click-wins. Full spec:
- Both players must choose the **same** option before it takes effect.
- If they choose different options: show "Waiting for opponent to decide…" to the waiting player.
- 60-second timeout: if both don't agree within 60s, default to End Game.
- If one player disconnects before deciding: default to End Game.
- Match winner (5 round wins): replace "New Round" with "Play Again" (resets championship score to 0–0).

**Server-side changes needed:**
- Add `PostRoundDecision` enum: `NewRound`, `EndGame`.
- Add `Dictionary<string, PostRoundDecision> PostRoundDecisions` to `GameSession`.
- Add `MakePostRoundDecision(token, decision)`:
  - Records the caller's decision.
  - If both players have chosen the same option → execute immediately (StartNewRound or set GameEnd).
  - If both chosen but differ → fire StateChanged (show disagreement UI).
  - Start a server-side 60s `System.Threading.Timer` on first decision; on expiry, if still in RoundEnd → set GameEnd.
- Remove existing `StartNewRound(callerToken)` and `EndGame(callerToken)` (first-click-wins) — replace with `MakePostRoundDecision`.

**Client-side:**
- Show both players' choices in the overlay (e.g., a chip showing "Alex: New Round | Bernard: End Game").
- Buttons switch to "pending" state after clicking; show "Waiting for opponent…".
- Handle 5-round match champion: heading changes to "Alex wins the MATCH! 🏆"; "New Round" replaced by "Play Again".

## 2. Championship scoring display
Championship score already tracked (`RoundWins`). Ensure:
- Score bar on the game board always shows correct values.
- Match champion heading in round-end overlay ("Alex wins the MATCH!") when either player reaches 5 wins.
- "Play Again" resets `RoundWins` to 0 for both players.

## 3. Chat log readability
Visual distinction between: questions (named, styled as question), answers (named, styled as answer),
system events (e.g. "Alex eliminated a face"), and turn boundaries (dividers between turns).

## 4. Suggested questions UI
A collapsible panel or inline chip list of attribute-based yes/no questions ("Does your person wear
glasses?", "Does your person have blonde hair?", etc.) that, when clicked, populate the chat input.
Speeds up gameplay and helps new players.

## 5. Face card visual polish
Iterate on face card rendering for more character and distinctiveness — more expressive facial
features, colour fills, accessory details. Still SVG/CSS only.

## 6. Challenge mode
Each player picks two Mystery People. Questions may have "Both"/"Either" answers. Modified win
condition: correctly identify both of the opponent's Mystery People.

---

### Completed / removed
- **Face elimination (own board)** — done in Iteration 8.
- **Opponent elimination sync (top panel)** — free consequence of Iteration 8.
- **Guessing mechanic** — done in Iteration 9. Simplified round-end overlay (first-click-wins) included.
