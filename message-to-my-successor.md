# Message to My Successor

## Status after Iteration 4
Iteration 4 complete and pushed. The full two-column game board layout is live for the Playing
phase. Both boards render correctly with the players' independently shuffled card orders.
The right column shows score bar, Mystery Person display, and a placeholder chat panel.
Build: 0 errors, 0 warnings.

## What to do next
Pick up **Iteration 5: Turn management** from `to-do.md`.

### Goal
Wire up alternating turn logic so the game knows whose turn it is, and the UI reflects this
clearly. No gameplay mechanics yet — just turn tracking and UI state locking.

### Server-side changes needed

Add to `GameSession`:

```csharp
/// <summary>Token of the player whose turn it is. Set when phase → Playing.</summary>
public string ActivePlayerToken { get; private set; } = "";

/// <summary>Whether a question has already been asked this turn.</summary>
public bool QuestionAsked { get; set; }

/// <summary>Returns true if the given token belongs to the active player.</summary>
public bool IsActivePlayer(string token) => token == ActivePlayerToken;
```

In `SelectMysteryPerson`, when both players confirm and phase transitions to Playing,
also set `ActivePlayerToken = Player1!.Token` (Player 1 always goes first in round 1).

Add a `StartNextTurn(string currentToken)` method:
```csharp
public void StartNextTurn(string currentToken)
{
    lock (_lock)
    {
        // Pass turn to the other player
        ActivePlayerToken = (ActivePlayerToken == Player1?.Token)
            ? Player2!.Token
            : Player1!.Token;
        QuestionAsked = false;
        NotifyStateChanged();
    }
}
```

### Client-side changes needed

In `Game.razor`, add a helper:
```csharp
private bool _isMyTurn => _session is not null && MyToken is not null
    && _session.IsActivePlayer(MyToken);
```

In the score bar section, replace "Game in progress…" with:
```razor
<div class="turn-status @(_isMyTurn ? "turn-status--active" : "")">
    @if (_isMyTurn)
    {
        <span>Your turn</span>
    }
    else
    {
        <span>@(_opponent?.Name ?? "Opponent")'s turn</span>
    }
</div>
```

The full turn indicator should use player names everywhere:
- Active player: "Alex's turn" (gold colour)
- Inactive player: "Waiting for Bernard…" (muted)

Add an "End Turn" button to the chat input area (visible to active player only):
```razor
@if (_isMyTurn)
{
    <button class="btn btn-secondary end-turn-btn" @onclick="EndTurn">End Turn</button>
}
```

Wire `EndTurn()` to call `GameSessionService.StartNextTurn(Code, MyToken!)`.

Add to `GameSessionService`:
```csharp
public void StartNextTurn(string code, string playerToken) =>
    GetSession(code)?.StartNextTurn(playerToken);
```

### CSS additions
`.turn-status--active { color: var(--accent-gold); font-weight: 700; }`
`.end-turn-btn { ... }` — secondary button style, fits in chat input area.

### Locking inactive player controls
- Chat input: `disabled="@(!_isMyTurn)"` (already disabled; now conditionally enabled)
- Send button: same logic
- End Turn button: only rendered for active player

### Things to remember
- `_isMyTurn` must be recomputed after every `StateHasChanged()` call (it reads `_session.ActivePlayerToken`,
  which is a reference type property, so it auto-updates).
- `StartNextTurn` fires `StateChanged`, so both circuits re-render and the inactive player sees
  "Your turn" as soon as the active player ends their turn.
- Turn management has NO countdown yet — that's Iteration 4 of turn mechanics (item 4 in to-do.md).
  For now, only the "End Turn" button passes the turn.

No messages.
