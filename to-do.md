# Guess Who? — Feature Backlog

Items are listed in priority order. Pick the top item each iteration.

---

## 1. Guessing mechanic ← NEXT
The active player can, instead of asking a question, make a direct guess: they name the opponent's
Mystery Person. If correct → win the round. If wrong → immediate loss. A "Make a Guess" button
or special input mode initiates this. Requires a confirmation step to prevent accidental guesses.

## 2. End-of-round overlay & post-game flow
When a round ends:
- Overlay shown to both players: outcome, both Mystery People revealed, current championship score.
- Both players choose **New Round** or **End Game**. Both must agree before action is taken.
- Disagreement: show "Waiting for opponent…"; resolve when both agree; 60-second timeout defaults to End Game.
- If one player disconnects before deciding → End Game.
- Match winner (5 round wins): replace "New Round" with "Play Again" (resets championship score).

## 3. Championship scoring
Track round wins per player across the session. Score displayed persistently (e.g. "Alex 2 – 1 Bernard").
First to 5 round wins → match champion. Reset on Play Again.

## 4. Chat log readability
Visual distinction between: questions (named, styled as question), answers (named, styled as answer),
system events (e.g. "Alex eliminated a face"), and turn boundaries (dividers between turns).

## 5. Suggested questions UI
A collapsible panel or inline chip list of attribute-based yes/no questions ("Does your person wear
glasses?", "Does your person have blonde hair?", etc.) that, when clicked, populate the chat input.
Speeds up gameplay and helps new players.

## 6. Face card visual polish
Iterate on face card rendering for more character and distinctiveness — more expressive facial
features, colour fills, accessory details. Still SVG/CSS only.

## 7. Challenge mode
Each player picks two Mystery People. Questions may have "Both"/"Either" answers. Modified win
condition: correctly identify both of the opponent's Mystery People.

---

### Completed / removed
- **Face elimination (own board)** — done in Iteration 8.
- **Opponent elimination sync (top panel)** — free consequence of Iteration 8: `_opponent.EliminatedIds`
  is read on every render; `StateChanged` fires on each elimination, so both circuits re-render and
  the opponent top board flips in real time automatically.
