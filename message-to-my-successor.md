# Message to My Successor

## Status after Iteration 8
Face elimination is fully live. The active player can click any face card on their own board to flip
it face-down. Red hover (border + glow + name tint + lift) on eliminatable cards makes intent clear.
Mystery Person card is immune: gold glow, no click handler, no red hover. Already-eliminated cards are
face-down and unclickable. Board header counts update dynamically ("18 remaining · 6 eliminated").
Opponent's top board also syncs in real time as a free consequence — no additional code needed.
Build: 0 errors, 0 warnings.

## What to do next
Pick up **to-do.md item 1: Guessing mechanic**.

### Goal
The active player can, INSTEAD of asking a question this turn, make a direct guess: they name
the opponent's Mystery Person by clicking their character on the opponent's board (top-left).
- If correct → win the round.
- If wrong → immediate loss (the other player wins the round).
Requires a clear confirmation step to prevent accidental guesses.

### Spec nuances
- The player may EITHER ask a question OR make a guess per turn — not both.
- The guess replaces the chat input: show a "Guess" mode button that the active player can click
  to enter guessing mode. In guessing mode, hovering over the opponent's board highlights cards
  as guessable (different colour from elimination red — use blue or gold).
- Confirmation: after clicking a card on the opponent board, show an inline confirmation panel
  ("Are you sure you want to guess [name]? This is your only guess this turn.") with Confirm / Cancel.
- On confirmation:
  - If correct: set round outcome to "active player wins", transition to RoundEnd phase.
  - If wrong: set round outcome to "active player loses", transition to RoundEnd phase.
- RoundEnd phase can just show a simple overlay for now — a full end-of-round flow is Iteration 2.
  For this iteration, just show who won/lost the round with a "Continue" or "Play Again" stub.
  OR: implement the full end-of-round overlay (it's the next item anyway) — your choice.

### Server-side changes needed

Add to `GameSession`:
- `GamePhase.RoundEnd` enum value (or handle inline with a `RoundResult` property).
- `PlayerState.RoundWins` is already present and tracked.
- `string ActivePlayerToken` is already set during Playing.

```csharp
// New enum in Enums.cs
public enum RoundEndReason { CorrectGuess, WrongGuess }

// New fields on GameSession
public RoundEndReason? RoundEndReason { get; private set; }
public string? RoundWinnerToken { get; private set; }  // token of the winner

public void MakeGuess(string callerToken, int guessedCharacterId)
{
    lock (_lock)
    {
        if (Phase != GamePhase.Playing) return;
        if (callerToken != ActivePlayerToken) return;

        var opponent = GetOpponent(callerToken);
        if (opponent is null) return;

        var isCorrect = guessedCharacterId == opponent.MysteryPersonId;

        if (isCorrect)
        {
            RoundWinnerToken = callerToken;
            GetPlayer(callerToken)!.RoundWins++;
        }
        else
        {
            // Wrong guess — opponent wins
            RoundWinnerToken = opponent.Token;
            opponent.RoundWins++;
        }

        RoundEndReason = isCorrect ? Models.RoundEndReason.CorrectGuess : Models.RoundEndReason.WrongGuess;
        CountdownStartedAt = null;  // cancel any running countdown
        Phase = GamePhase.RoundEnd;
        NotifyStateChanged();
    }
}
```

Add to `GamePhase` enum: `RoundEnd`.

### Client-side changes needed (Game.razor)

Add a **guess mode** to the game board:
- A "Make a Guess" button at the top of the chat input area (only shown when active player,
  no question asked yet, not already in guess mode).
- When clicked, sets a `_guessModeActive` local bool.
- In guess mode, the opponent's board becomes interactive: face-up cards get a gold/blue hover
  (a new `IsGuessable` parameter on FaceCard, or just pass OnClick with a different visual).
- Clicking a card in the opponent board shows an inline confirmation (e.g. below the board or
  a floating panel): "Guess [Name]? This is irreversible." + Confirm + Cancel buttons.
- Confirm → calls `GameSessionService.MakeGuess(Code, MyToken!, guessedCharacterId)`.
- Cancel → clears `_guessModeActive` and `_pendingGuessId`.

For the RoundEnd phase, render a simple overlay (full-screen dark overlay + centred card):
- Outcome: "You win the round!" or "You lose the round — [opponent] guessed correctly!"
- Both Mystery People revealed (sm FaceCards with names).
- Score display.
- "New Round" button → resets the round (clear eliminated IDs, clear mystery person IDs,
  re-enter CharacterSelection phase). Only one player needs to click for now — full consensus
  flow comes in the post-game flow iteration.
- This is intentionally simplified — the full post-game consensus flow is Iteration 2 in to-do.

### Things to remember
- `_opponent?.BoardOrder` drives the top board. To make those cards guessable, you need to pass
  `OnClick` and `IsGuessable=true` (new FaceCard param, similar to IsEliminatable but different color).
- Use a different color than red (elimination) — suggested: `#58a6ff` (blue) or `#f0a500` (gold).
  Gold might conflict with Mystery Person glow. Blue is the safest choice.
- Face-down cards on the opponent board should NOT be guessable (if they're already eliminated,
  the active player knows they're not the mystery person). Guard with `!isEliminated`.
- The `MakeGuess` server method must also handle the countdown: clear `CountdownStartedAt` so
  the countdown timer stops.
- Add `MakeGuess` passthrough to `GameSessionService`.
