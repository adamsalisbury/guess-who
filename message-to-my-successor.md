# Message to My Successor

## Status after Iteration 6
Chat panel & question flow is fully live. Both players see real-time questions and answers in the
chat log. The active player types, hits Send (or Enter), and the question appears styled with a
gold left border in both logs. The inactive player immediately sees Yes/No buttons; clicking one
appends the styled answer (green right-aligned pill). The chat input is locked (`QuestionAsked=true`)
after a question is sent and only unlocks at `StartNextTurn()`. The End Turn button remains
available to the active player throughout. Build: 0 errors, 0 warnings.

## What to do next
Pick up **Iteration 1 in to-do.md: Turn end mechanics**.

### Goal
After the inactive player answers, a 10-second countdown starts — visible to both players.
The countdown fires `StartNextTurn` when it reaches zero. The active player can still click
"End Turn" at any point to skip the countdown. Face-flip interactions remain available during
the countdown window (those come in the next iteration, but the architecture should not block them).

### Server-side changes needed

Add to `GameSession`:
```csharp
/// <summary>UTC time when the countdown started. Null when no countdown is running.</summary>
public DateTime? CountdownStartedAt { get; private set; }

/// <summary>True when the countdown is actively ticking (question answered, not yet turn-ended).</summary>
public bool CountdownActive => CountdownStartedAt.HasValue;

private const int CountdownSeconds = 10;
```

In `AnswerQuestion()`, after posting the answer to the log, start the countdown:
```csharp
CountdownStartedAt = DateTime.UtcNow;
```

In `StartNextTurn()`, reset the countdown:
```csharp
CountdownStartedAt = null;
```

### Client-side countdown timer

In `Game.razor`, add a `System.Threading.Timer` that ticks every ~500ms during Playing phase.
On each tick:
1. If `_session.CountdownActive` is false, stop the timer.
2. Compute remaining = 10 - (DateTime.UtcNow - _session.CountdownStartedAt).TotalSeconds.
3. If remaining <= 0, call `GameSessionService.StartNextTurn(Code, MyToken!)` (only from the active player's circuit to avoid double-fire).
4. Call `await InvokeAsync(StateHasChanged)` to update the countdown display.

**Only the active player's timer should call StartNextTurn** — check `_isMyTurn` before the call.
Both circuits should still tick (for display), but only one fires the turn-end.

Example pattern:
```csharp
private System.Threading.Timer? _countdownTimer;

private void StartCountdownTimer()
{
    _countdownTimer?.Dispose();
    _countdownTimer = new System.Threading.Timer(async _ =>
    {
        if (_session?.CountdownActive != true)
        {
            _countdownTimer?.Dispose();
            return;
        }

        var elapsed = (DateTime.UtcNow - _session.CountdownStartedAt!.Value).TotalSeconds;
        if (elapsed >= CountdownSeconds && _isMyTurn)
            GameSessionService.StartNextTurn(Code, MyToken!);

        await InvokeAsync(StateHasChanged);
    }, null, dueTime: 0, period: 500);
}
```

Call `StartCountdownTimer()` inside `OnSessionStateChanged` when `_session.CountdownActive` becomes true.
Dispose the timer in `Dispose()`.

### UI changes needed

In the score bar (or chat-input-area), when `_session.CountdownActive` is true, show:
```
Turn ending in Xs…
```
where X is computed as `Math.Max(0, CountdownSeconds - elapsed)`.

Show this to BOTH players. The active player also retains the End Turn button.

### Things to remember
- `CountdownStartedAt` is read from both circuits for display — no lock needed for reads.
- Only the active player's circuit calls `StartNextTurn` to avoid double-fire.
- Dispose the timer before the component is garbage-collected.
- The timer uses a thread-pool thread — always use `InvokeAsync(StateHasChanged)` to get back
  onto the Blazor render thread, never call `StateHasChanged()` directly from the timer callback.
