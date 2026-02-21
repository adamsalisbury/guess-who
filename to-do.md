# Guess Who? — Feature Backlog

Items are listed in priority order. Pick the top item each iteration.

---

## 1. Face elimination (own board) ← NEXT
The active player can click any face card on their own board to flip it face-down (eliminate it).
This action is permanent for the round. The Mystery Person card is immune and cannot be eliminated.

## 2. Opponent elimination sync (top panel)
When a player eliminates a face from their own board, the corresponding card in the opponent's
top-panel view updates in real time (flipped down). Uses the existing `StateChanged` event pattern.

## 3. Guessing mechanic
The active player can, instead of asking a question, make a direct guess: they name the opponent's
Mystery Person. If correct → win the round. If wrong → immediate loss. A "Make a Guess" button
or special input mode initiates this. Requires a confirmation step to prevent accidental guesses.

## 4. End-of-round overlay & post-game flow
When a round ends:
- Overlay shown to both players: outcome, both Mystery People revealed, current championship score.
- Both players choose **New Round** or **End Game**. Both must agree before action is taken.
- Disagreement: show "Waiting for opponent…"; resolve when both agree; 60-second timeout defaults to End Game.
- If one player disconnects before deciding → End Game.
- Match winner (5 round wins): replace "New Round" with "Play Again" (resets championship score).

## 5. Championship scoring
Track round wins per player across the session. Score displayed persistently (e.g. "Alex 2 – 1 Bernard").
First to 5 round wins → match champion. Reset on Play Again.

## 6. Chat log readability
Visual distinction between: questions (named, styled as question), answers (named, styled as answer),
system events (e.g. "Alex eliminated a face"), and turn boundaries (dividers between turns).

## 7. Suggested questions UI
A collapsible panel or inline chip list of attribute-based yes/no questions ("Does your person wear
glasses?", "Does your person have blonde hair?", etc.) that, when clicked, populate the chat input.
Speeds up gameplay and helps new players.

## 8. Face card visual polish
Iterate on face card rendering for more character and distinctiveness — more expressive facial
features, colour fills, accessory details. Still SVG/CSS only.

## 9. Challenge mode
Each player picks two Mystery People. Questions may have "Both"/"Either" answers. Modified win
condition: correctly identify both of the opponent's Mystery People.
