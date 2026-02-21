# Guess Who? — Feature Backlog

Items are listed in priority order. Pick the top item each iteration.

---

## 1. Championship scoring display ← NEXT
Championship score already tracked (`RoundWins`). Ensure:
- Score bar on the game board always shows correct values. (Already done — both players' RoundWins
  render in the score bar in real time.)
- Match champion heading in round-end overlay is shown correctly. (Done in Iteration 10.)
- "Play Again" resets `RoundWins` to 0 for both players. (Done in Iteration 10.)
- **What remains**: verify score updates correctly in the score bar during a match (not just on the
  overlay). This is a smoke-test item; implementation is already in place.

## 2. Chat log readability
Visual distinction between: questions (named, styled as question), answers (named, styled as answer),
system events (e.g. "Alex eliminated a face"), and turn boundaries (dividers between turns).

## 3. Suggested questions UI
A collapsible panel or inline chip list of attribute-based yes/no questions ("Does your person wear
glasses?", "Does your person have blonde hair?", etc.) that, when clicked, populate the chat input.
Speeds up gameplay and helps new players.

## 4. Face card visual polish
Iterate on face card rendering for more character and distinctiveness — more expressive facial
features, colour fills, accessory details. Still SVG/CSS only.

## 5. Challenge mode
Each player picks two Mystery People. Questions may have "Both"/"Either" answers. Modified win
condition: correctly identify both of the opponent's Mystery People.

---

### Completed / removed
- **Face elimination (own board)** — done in Iteration 8.
- **Opponent elimination sync (top panel)** — free consequence of Iteration 8.
- **Guessing mechanic** — done in Iteration 9.
- **End-of-round overlay consensus & post-game flow** — done in Iteration 10. Full consensus
  mechanism: both players must agree, 60-second timeout, match champion (5 wins), Play Again.
