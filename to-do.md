# Guess Who? — Feature Backlog

Items are listed in priority order. Pick the top item each iteration.

---

## 1. Character data & face card rendering ← NEXT
Define and render each of the 24 characters as a styled face card built entirely from CSS/SVG
derived from their attributes (hair colour, eye colour, glasses, hat, facial hair, hair length,
bald, rosy cheeks, big nose). No external image assets. Cards must be clearly readable and
meaningfully distinct from one another. All 24 should be visible in a simple gallery page so the
rendering can be verified before gameplay layers are added.

## 2. Mystery Person selection
At game start, each player is presented with all 24 face cards and secretly selects one as their
Mystery Person. A confirmation step before the board is revealed. Selected card is stored in
`PlayerState.MysteryPersonId`. Both players must confirm before the game phase advances.

## 3. Game board layout
Two-column desktop layout (≥1280px):
- **Left column**: top half = opponent's board (compact, read-only, labelled with opponent name);
  bottom half = own board (full-size, active player can flip faces).
- **Right column**: score/status bar (championship score, round number, whose turn);
  Mystery Person display (own card, clearly labelled, hidden from opponent);
  chat panel (scrollable log + input area).

## 4. Turn management
Enforce alternating turns. Turn indicator uses player names ("Alex's turn" / "Waiting for
Bernard…"). All interactive controls locked for the inactive player. Turn state stored
server-side in `GameSession`.

## 5. Chat panel & question flow
Active player types and sends a question (free text). The message appears in both players' chat
logs. The inactive player sees Yes/No buttons inline with that message. Their answer is appended
to the log with their name ("Bernard: Yes"). Both players see the exchange in real time.

## 6. One-question-per-turn lock
After the active player sends a question, the chat input is locked for the remainder of that turn.
Unlocks on turn change. Only one question per turn is permitted.

## 7. Turn end mechanics
- A **10-second countdown** begins after the inactive player answers (visible to both players).
- An **"End Turn"** button is available to the active player throughout their entire turn (not just post-answer).
- Turn passes either when the countdown expires or the active player clicks "End Turn".
- Clicking faces to flip them remains available during the countdown window.

## 8. Face elimination (own board)
The active player can click any face card on their own board to flip it face-down (eliminate it).
This action is permanent for the round. The Mystery Person card is immune and cannot be eliminated.

## 9. Opponent elimination sync (top panel)
When a player eliminates a face from their own board, the corresponding card in the opponent's
top-panel view updates in real time (flipped down). Uses the existing `StateChanged` event pattern.

## 10. Guessing mechanic
The active player can, instead of asking a question, make a direct guess: they name the opponent's
Mystery Person. If correct → win the round. If wrong → immediate loss. A "Make a Guess" button
or special input mode initiates this. Requires a confirmation step to prevent accidental guesses.

## 11. End-of-round overlay & post-game flow
When a round ends:
- Overlay shown to both players: outcome, both Mystery People revealed, current championship score.
- Both players choose **New Round** or **End Game**. Both must agree before action is taken.
- Disagreement: show "Waiting for opponent…"; resolve when both agree; 60-second timeout defaults to End Game.
- If one player disconnects before deciding → End Game.
- Match winner (5 round wins): replace "New Round" with "Play Again" (resets championship score).

## 12. Championship scoring
Track round wins per player across the session. Score displayed persistently (e.g. "Alex 2 – 1 Bernard").
First to 5 round wins → match champion. Reset on Play Again.

## 13. Chat log readability
Visual distinction between: questions (named, styled as question), answers (named, styled as answer),
system events (e.g. "Alex eliminated a face"), and turn boundaries (dividers between turns).

## 14. Suggested questions UI
A collapsible panel or inline chip list of attribute-based yes/no questions ("Does your person wear
glasses?", "Does your person have blonde hair?", etc.) that, when clicked, populate the chat input.
Speeds up gameplay and helps new players.

## 15. Face card visual polish
Iterate on face card rendering for more character and distinctiveness — more expressive facial
features, colour fills, accessory details. Still SVG/CSS only.

## 16. Challenge mode
Each player picks two Mystery People. Questions may have "Both"/"Either" answers. Modified win
condition: correctly identify both of the opponent's Mystery People.
