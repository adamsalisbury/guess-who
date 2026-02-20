# Message to My Successor

## Status after Iteration 2
Iteration 2 is complete and pushed. All 24 character face cards render correctly from attribute data
using inline SVG. The gallery page at `/gallery` lets you visually verify all cards. Build: 0 errors,
0 warnings.

## What to do next
Pick up **Iteration 3: Mystery Person selection** from `to-do.md`.

### Goal
When both players navigate to `/game/{Code}`, instead of the placeholder, they should each be shown a
selection screen presenting all 24 face cards. Each player secretly picks one character as their Mystery
Person. After confirming, both players wait until the opponent also confirms. Once both players have
confirmed their selection, the game phase advances to `Playing` and the main game board is shown
(still a placeholder at this point).

### Suggested approach
1. **Add `MysteryPersonId` to `PlayerState`** (it already exists as `int?` — verify this and add if
   not present). Add a method like `PlayerState.SetMysteryPerson(int characterId)`.
2. **Add a `CharacterSelection` phase** to the `GamePhase` enum if not already there (it is — check).
   The `GameSession` should track that both players must confirm before advancing to `Playing`.
3. **On `GameSession`**, add a method `SelectMysteryPerson(string token, int characterId)`:
   - Validates the token matches a player in the session
   - Sets that player's `MysteryPersonId`
   - If both players have selected, advances `Phase` to `GamePhase.Playing` and fires `StateChanged`
4. **Update `GameSessionService`**: expose `SelectMysteryPerson` or just pass through to the session.
5. **Update `Game.razor`** to show different UI based on `GameSession.Phase`:
   - `GamePhase.CharacterSelection`: show the Mystery Person picker (full-screen grid of 24 cards)
   - `GamePhase.Playing`: show the game board (still placeholder — just "Game in progress" for now)
   - `GamePhase.Lobby`: redirect to `/lobby/{Code}` if somehow landed here early
6. **Mystery Person picker UI**:
   - Headline: "Choose your Mystery Person" (secret — do not show opponent's pick)
   - Grid of all 24 `FaceCard` components, `Size="md"`, with `OnClick` callback
   - Clicking a card selects it (visually highlight it with `IsMystery=true`, others dimmed)
   - A confirmation button: "Confirm — I choose [Name]!" — submitting calls `SelectMysteryPerson`
   - After confirming, show a waiting screen: "Waiting for [OpponentName] to choose…" with animation
   - Waiting screen updates in real time via `StateChanged` subscription (same pattern as Lobby)
   - When both players confirm, both auto-advance to the game board view
7. **Lobby should advance phase on start**: when the Lobby auto-navigates both players to `/game/{Code}`,
   the session phase should move from `Lobby` to `CharacterSelection`. Add a method on `GameSession`
   like `StartCharacterSelection()` and call it when the second player joins (in `AddPlayer`). Actually,
   it might be cleaner to call it when the Game page initialises — check which player arrived first.
   The safest approach: in `Game.razor.OnInitialized`, if both players are present and phase is still
   `Lobby`, call a `BeginCharacterSelection()` method on the session.

### Things to remember
- Each player should only see their OWN selection — do not show what the opponent chose.
- The selection is done simultaneously (both pick at the same time), not turn-based.
- The confirmation must be two-way: both must select before advancing. The phase transition
  (`CharacterSelection` → `Playing`) only fires once both `MysteryPersonId` values are set.
- Use the existing `StateChanged` event pattern for real-time updates. The `Game.razor` component
  subscribes in `OnInitializedAsync` and unsubscribes in `Dispose()`.
- `PlayerState` already has a `Token` field for identifying which player is which in the component.
- **Important UX**: make the picking grid feel exciting — a "choosing your champion" moment. Each
  card should respond to hover. The selected card should be prominently highlighted before confirmation.

No messages.
