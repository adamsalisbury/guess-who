# Message to My Successor

## Status after Iteration 9
The guessing mechanic is fully implemented. The active player can click "🎯 Make a Guess Instead" at
any point on their turn (before asking a question), which activates guess mode. In guess mode, hovering
over the opponent's face-up cards shows a blue glow. Clicking a card shows an inline confirmation panel
with a clear warning ("A wrong guess means you lose the round immediately"). Confirming calls
`GameSessionService.MakeGuess` → server transitions to `RoundEnd` phase → both circuits re-render and
see the round-end overlay simultaneously.

The round-end overlay reveals both Mystery People, shows the championship score, and gives players
"New Round" and "End Game" buttons. It is currently **first-click-wins** (any player can click
"New Round" to immediately start the next round). The full consensus mechanism is the next to-do item.

Build: 0 errors, 0 warnings.

## What to do next
Pick up **to-do.md item 1: End-of-round overlay consensus & post-game flow**.

### Goal
Replace the first-click-wins round-end buttons with a proper consensus mechanism:
- Both players must choose the **same** option before it takes effect.
- Display both players' current choices in the overlay.
- 60-second server-side timeout defaults to End Game.
- When either player reaches 5 round wins, declare them match champion and replace "New Round" with "Play Again".

### Server-side changes needed

1. **Add `PostRoundDecision` enum** to `Enums.cs`:
   ```csharp
   public enum PostRoundDecision { NewRound, EndGame }
   ```

2. **Remove** `StartNewRound(callerToken)` and `EndGame(callerToken)` from `GameSession` (the current
   first-click-wins implementations). Replace with:

   ```csharp
   // On GameSession:
   private readonly Dictionary<string, PostRoundDecision> _postRoundDecisions = new();
   private System.Threading.Timer? _postRoundTimeoutTimer;

   public IReadOnlyDictionary<string, PostRoundDecision> PostRoundDecisions =>
       _postRoundDecisions.AsReadOnly() ... // or just expose the dict

   public void MakePostRoundDecision(string callerToken, PostRoundDecision decision)
   {
       lock (_lock)
       {
           if (Phase != GamePhase.RoundEnd) return;
           if (GetPlayer(callerToken) is null) return;

           _postRoundDecisions[callerToken] = decision;

           // Start the 60s timeout timer on the first decision (if not already running)
           _postRoundTimeoutTimer ??= new System.Threading.Timer(_ =>
           {
               lock (_lock)
               {
                   if (Phase == GamePhase.RoundEnd)
                   {
                       _postRoundDecisions.Clear();
                       Phase = GamePhase.GameEnd;
                       NotifyStateChanged();
                   }
               }
           }, null, TimeSpan.FromSeconds(60), Timeout.InfiniteTimeSpan);

           // Check if both players have decided the same thing
           if (_postRoundDecisions.Count == 2)
           {
               var p1Decision = _postRoundDecisions.GetValueOrDefault(Player1!.Token);
               var p2Decision = _postRoundDecisions.GetValueOrDefault(Player2!.Token);
               if (p1Decision == p2Decision)
               {
                   _postRoundTimeoutTimer?.Dispose();
                   _postRoundTimeoutTimer = null;
                   if (p1Decision == PostRoundDecision.NewRound)
                       ExecuteNewRound();
                   else
                       ExecuteEndGame();
                   return;
               }
           }

           NotifyStateChanged();
       }
   }

   private void ExecuteNewRound()
   {
       // Same logic as current StartNewRound (increment round, reset state, CharacterSelection)
       // Also reset _postRoundDecisions
   }

   private void ExecuteEndGame()
   {
       Phase = GamePhase.GameEnd;
   }
   ```

3. **Also handle disconnection** in `AddPlayer` or a `PlayerDisconnected(token)` method: if the session
   is in `RoundEnd` and one player disconnects, set `Phase = GameEnd`.

4. **Match champion logic** — add to `MakeGuess` (or at end of `MakeGuess`):
   ```csharp
   const int WinsToWin = 5;
   if (GetPlayer(RoundWinnerToken!)!.RoundWins >= WinsToWin)
       IsMatchOver = true;  // new bool property
   ```
   Expose `IsMatchOver` and `MatchWinnerToken` on the session. Client reads these in the overlay.

5. **Update `GameSessionService`**: replace `StartNewRound`/`EndGame` with `MakePostRoundDecision`.

### Client-side changes needed (Game.razor)

1. **Replace `NewRound()` and `LeaveGame()` methods** with a single:
   ```csharp
   private void MakeDecision(PostRoundDecision decision)
   {
       if (MyToken is not null)
           GameSessionService.MakePostRoundDecision(Code, MyToken, decision);
   }
   ```

2. **Update overlay buttons** to call `MakeDecision(PostRoundDecision.NewRound)` and
   `MakeDecision(PostRoundDecision.EndGame)`.

3. **Show both players' choices** in the overlay UI:
   ```
   [Alex: New Round ✓]   [Bernard: —]
   ```
   After clicking, your button becomes "chosen" (dimmed/checked), other button disabled.
   Show "Waiting for opponent to decide…" if you've chosen but they haven't.

4. **Match champion**: if `_session.IsMatchOver`:
   - Show "Alex wins the MATCH! 🏆" as a second heading (gold/large).
   - Replace "New Round" button with "Play Again" (same `PostRoundDecision.NewRound` internally,
     but `ExecuteNewRound` also resets both players' `RoundWins` to 0).
   - OR use a separate `PostRoundDecision.PlayAgain` enum value.

### Suggested approach for Play Again vs New Round distinction
The simplest approach: keep `PostRoundDecision.NewRound` for both, but in `ExecuteNewRound()`:
- If `IsMatchOver`, also reset `Player1.RoundWins = 0; Player2.RoundWins = 0`.
- Client renders the button label as "Play Again" when `IsMatchOver`, "New Round" otherwise.
- No need for a separate enum value.

### Things to remember
- The 60s timer fires on a thread-pool thread — must use `lock (_lock)` before reading `Phase`.
- `_postRoundDecisions` must be cleared in `ResetRoundState()` (for when a new round begins).
- `_postRoundTimeoutTimer` must be disposed in `ResetRoundState()` and in any cleanup path.
- Only expose `IsMatchOver` as a read-only property (set in `MakeGuess` when winner hits 5).
- `IsMatchOver` must be reset in `ResetRoundState()` (for Play Again).
