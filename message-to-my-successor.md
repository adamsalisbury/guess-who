# Message to My Successor

## Status after Iteration 3
Iteration 3 complete and pushed. Mystery Person selection is fully functional:
both players can secretly pick their character, confirm, wait for the opponent,
and the phase transitions to Playing (RoundNumber = 1). Build: 0 errors, 0 warnings.

## What to do next
Pick up **Iteration 4: Game board layout** from `to-do.md`.

### Goal
Replace the "game board placeholder" shown during the `Playing` phase with the
actual two-column game board layout. No gameplay mechanics yet — just the layout
shell with all the right sections in the right places.

### Required layout (desktop landscape ≥1280px)
```
┌─────────────────────────────┬──────────────────────────┐
│ LEFT COLUMN                 │ RIGHT COLUMN (fixed ~340px)
│                             │                          │
│  ┌── Opponent board ──────┐ │  Score / status bar      │
│  │ compact (sm) grid      │ │  "Alex 0 – 0 Bernard"    │
│  │ 24 cards, read-only    │ │  Round 1                 │
│  └────────────────────────┘ │  "Alex's turn"           │
│                             │                          │
│  ┌── Own board ───────────┐ │  Mystery Person          │
│  │ full (md) grid         │ │  (lg card, gold glow,    │
│  │ player can flip later  │ │  "Your Mystery Person"   │
│  │ mystery immune         │ │  label, visible to self) │
│  └────────────────────────┘ │                          │
│                             │  Chat panel              │
│                             │  (empty log placeholder, │
│                             │  disabled input + Send)  │
└─────────────────────────────┴──────────────────────────┘
```

### Suggested approach

1. **Board order**: each player sees cards in a randomised order. Add
   `List<int> BoardOrder { get; } = new()` to `PlayerState`. Populate it in
   `GameSession.SelectMysteryPerson` when phase advances: call
   `player.BoardOrder.AddRange(CharacterData.All.Select(c => c.Id))`
   then shuffle with `Random.Shared.Shuffle(CollectionsMarshal.AsSpan(player.BoardOrder))`.
   Alternatively, populate both players' orders at the moment phase becomes Playing.

2. **Game.razor Playing branch**: replace the placeholder `<div>` with a proper two-column layout div.

3. **Left column — Opponent board (top, ~40% height)**:
   - Header label: opponent's name + "Board"
   - 6-column grid of 24 `sm` FaceCard components rendered in *opponent's* `BoardOrder`
   - All face-up for now (elimination sync comes in Iteration 7)
   - No `OnClick` — read only

4. **Left column — Own board (bottom, ~60% height)**:
   - Header label: own name + "Board" or "Your Board"
   - 6-column grid of 24 `md` FaceCard components in *own* `BoardOrder`
   - Mystery Person card: `IsMystery="true"` — visually distinguished
   - No `OnClick` yet (flip mechanic is Iteration 6)

5. **Right column — three stacked sections**:
   - **Score bar**: round number, championship score ("Alex 0 – 0 Bernard"), turn status.
     Use placeholder strings for now — these will be wired up in turn management iteration.
   - **Mystery Person**: `lg` FaceCard with `IsMystery="true"`, labelled "Your Mystery Person".
     Uses `_me.MysteryPersonId` to look up the character.
   - **Chat panel**: scrollable `<div>` with placeholder text "The game is afoot…", and below it
     a disabled text input and disabled Send button. Wire up in Iteration 3 of chat features.

6. **CSS**: create `Game.razor.css` additions or a new section in the existing file for:
   - `.game-board` — `display: flex; height: 100vh; overflow: hidden`
   - `.game-left` — `flex: 1; display: flex; flex-direction: column; overflow: hidden`
   - `.game-right` — `width: 340px; flex-shrink: 0; display: flex; flex-direction: column`
   - `.board-section` — `flex: 1; overflow: hidden; display: flex; flex-direction: column`
   - `.board-grid` — `display: grid; grid-template-columns: repeat(6, ...)` — sm for opponent, md for own
   - `.board-grid-area` — `flex: 1; overflow-y: auto; padding: 8px`
   - `.score-bar`, `.mystery-panel`, `.chat-panel` — right column sections

### Things to remember
- The overall layout must NOT introduce page-level scrolling (`overflow: hidden` on root).
- Individual board grid areas scroll independently (`overflow-y: auto`).
- The right column has three flex sections — use `flex: 1` on the chat panel so it expands
  to fill remaining space after score bar and mystery person panel.
- Both boards show the *shuffled* order specific to each player. The opponent board shows
  the opponent's order (use `_opponent.BoardOrder`).
- Use `_me` and `_opponent` references already resolved in the component.
- The `BoardOrder` property needs to be populated before the board renders — populate it
  as part of the SelectMysteryPerson phase-transition logic in GameSession.

No messages.
