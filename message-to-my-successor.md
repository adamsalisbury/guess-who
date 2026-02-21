# Message to My Successor

## Status after Iteration 7
Turn end mechanics are fully live. After the inactive player clicks Yes or No, a 10-second countdown
appears in the score bar ("⏱ Turn ending in Xs…") visible to both players simultaneously. The chat
input area also shows "Turn ends in Xs — or end it now." to the active player. When the countdown
reaches zero, the active player's circuit fires `StartNextTurn` automatically. The End Turn button
has always been available to the active player and continues to skip the countdown immediately.
Build: 0 errors, 0 warnings.

## What to do next
Pick up **Iteration 1 in to-do.md: Face elimination (own board)**.

### Goal
The active player can click any face card on their own board (bottom-left section) to flip it
face-down. The flip is permanent for the round. The Mystery Person card is immune (cannot be
eliminated). Eliminated cards already render as `FaceDown="true"` — you just need to wire the
click handler to add the card ID to `_me.EliminatedIds`.

### Server-side changes needed

Add to `GameSession`:
```csharp
/// <summary>
/// Records that the active player has eliminated a character from their board.
/// No-ops if: caller is not active player, character is their Mystery Person, or already eliminated.
/// </summary>
public void EliminateCharacter(string callerToken, int characterId)
{
    lock (_lock)
    {
        var player = GetPlayer(callerToken);
        if (player is null) return;
        if (player.MysteryPersonId == characterId) return;   // immune
        if (!player.EliminatedIds.Add(characterId)) return;  // already eliminated
        NotifyStateChanged();
    }
}
```

Add a passthrough in `GameSessionService`:
```csharp
public void EliminateCharacter(string code, string playerToken, int characterId) =>
    GetSession(code)?.EliminateCharacter(playerToken, characterId);
```

**Design note**: The spec says elimination is allowed during the active player's turn. You can
make it also available during countdown (which it naturally will be since only `_isMyTurn` guards
are needed, not `CountdownActive`). The inactive player should NOT be able to eliminate — guard
with `_isMyTurn` check in the component.

### Client-side changes needed

In `Game.razor`, the own board section already renders cards with `FaceDown="@isEliminated"`.
Now add an `OnClick` callback that calls `EliminateCharacter`:

```razor
@foreach (var charId in _me!.BoardOrder)
{
    var localId = charId;
    var ch = CharacterData.GetById(localId);
    var isMystery = localId == _me!.MysteryPersonId;
    var isEliminated = _me!.EliminatedIds.Contains(localId);
    <FaceCard Character="ch"
              Size="md"
              ShowName="true"
              IsMystery="@isMystery"
              FaceDown="@isEliminated"
              OnClick="@(_isMyTurn && !isMystery && !isEliminated ? _ => EliminateCard(localId) : null)" />
}
```

Add the handler:
```csharp
private void EliminateCard(int characterId)
{
    if (!_isMyTurn || MyToken is null) return;
    GameSessionService.EliminateCharacter(Code, MyToken, characterId);
}
```

### UX considerations
- When the active player hovers over a non-mystery, non-eliminated card, show a cursor pointer and
  a subtle red tint or "×" overlay to indicate it can be eliminated.
- When hovering over the Mystery Person card, show a "lock" cursor to indicate it's immune.
- After clicking, the card flips face-down immediately (optimistic — server confirms via StateChanged).
  Since `_me.EliminatedIds` is a reference type mutated server-side, and `StateChanged` fires on
  every elimination, the flip is near-instant.
- Add a CSS class `.face-card--clickable` for the hover state on eliminatable cards.

### Things to remember
- The guard `!isMystery && !isEliminated` prevents clicking the Mystery Person or already-eliminated cards.
- The inactive player should see their own board in read-only mode (no click handlers). Guard with `_isMyTurn`.
- `EliminatedIds.Add()` returns false if already present — safe double-click protection server-side.
- `StateChanged` fires on every elimination — both circuits re-render, so the opponent's top board
  also flips in real time (the next iteration will verify the opponent view, but it should "just work"
  since `_opponent.EliminatedIds` is read on every render).
