# Message to My Successor

## Status after Iteration 10
The full post-round consensus mechanism is implemented. Both players must now actively agree on
"New Round" (or "Play Again") or "End Game" before anything happens. The round-end overlay shows
real-time decision chips for each player, a waiting/disagreement message, and a 60-second server-
side timeout that defaults to End Game.

Match champion logic is complete: the first player to 5 round wins triggers `IsMatchOver = true`.
The overlay shows a match champion banner ("🏆 You win the match!" / "🏆 [Opponent] wins the match!"),
and the "New Round" button becomes "Play Again". When both players agree to Play Again, the server
resets both players' `RoundWins` to 0 and starts a fresh match from character selection.

Build: 0 errors, 0 warnings.

## What to do next

Pick up **to-do.md item 2: Chat log readability**.

The current chat log already has per-kind CSS (question, answer, system) but the visual distinction
could be clearer. Specifically:

### Goal
Make it immediately obvious at a glance which messages are questions (from the active player),
which are answers (Yes/No from the inactive player), and where turn boundaries lie.

### Suggested improvements

1. **Turn boundary dividers** — After each `Answer` message, insert a thin horizontal rule with
   a small "Turn [N]" label between turns. This could be a special `ChatMessageKind.TurnBoundary`
   entry posted by `StartNextTurn`, or rendered purely client-side by scanning adjacent messages.
   Client-side rendering is simpler: between two consecutive questions (or after an answer when
   the turn passes), render a `<div class="turn-divider">Turn [N] / [PlayerName]</div>`.

2. **Question bubbles** — Make question messages look more like a chat bubble with a left-pointing
   tail (CSS pseudo-element). Background: semi-transparent gold. Sender name in gold above.

3. **Answer bubbles** — Right-aligned bubble with green accent. Just "Yes" or "No" in large bold
   text. Sender name below in green.

4. **System messages** — Keep centred, muted, italic but add a subtle icon (⚙️ or similar) or
   colour-coded border to distinguish from game events vs. session events.

5. **Timestamp or turn number** — Consider showing which turn number a question was asked on
   (e.g. "Turn 3") as a small tag on each question message.

### Implementation notes
- `ChatMessageKind` enum already has `Question`, `Answer`, `System`. Adding `TurnBoundary` would
  require `StartNextTurn()` to post a synthetic chat entry — simple but requires passing in the
  new active player's name.
- Alternatively, render the dividers purely in the Razor loop by checking if the previous message
  was an `Answer` (meaning a new turn just started). No server changes needed.
- The chat log is in `Game.razor`'s `chat-log` div. CSS is in `Game.razor.css`.

### Reminder
No messages. (If I had any special notes, I would write them here.)
