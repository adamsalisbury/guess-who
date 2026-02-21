# Message to My Successor

## Status after Iteration 14
Challenge Mode is fully implemented. Build: 0 errors, 0 warnings.

The feature backlog (`to-do.md`) is now empty — all planned game features have been shipped.

## What changed (summary)
Seven files modified:

- `PlayerState.cs` — `int? MysteryPersonId` → `List<int> MysteryPersonIds` + `HasSelectedMysteryPeople`
- `GameSession.cs` — `SelectMysteryPeople(id1, id2)`, `AnswerQuestion(string)`, `MakeGuess(id1, id2)`, updated guards
- `GameSessionService.cs` — updated passthrough signatures
- `FaceCard.razor` — new `IsSelected` parameter (solid blue glow, "Click to deselect" title)
- `FaceCard.razor.css` — `.face-card--selected` style
- `Game.razor` — full rewrite of challenge-affected sections (see done.md Iteration 14 for detail)
- `Game.razor.css` — new styles: mystery cards row, selection pair footer, 3-button answers, 4-card reveal

## What to do next

The feature backlog is empty. You have three options:

### Option A — Technical debt
Pick from `to-do-technical.md`:
- Session lifecycle cleanup (IHostedService to remove dead sessions)
- Player reconnect / sessionStorage token persistence
- Remove unused Bootstrap dependency
- Unit tests (xUnit + bUnit)
- Input sanitisation on server side

### Option B — New game modes / enhancements
Consider adding ideas not in the original spec:
- **Sound effects** (Web Audio API via JS interop — short clips on answer/guess/win)
- **Spectator mode** — read-only third-party viewing of an active game
- **Timer-per-turn** — visible countdown pressure (could reuse existing countdown infrastructure)
- **Profile / avatar** — let players choose a colour or icon to display beside their name

### Option C — UX polish pass
- Keyboard accessibility (focus management, ARIA labels on game buttons)
- Animation pass — flip transitions for eliminated face cards (CSS 3D transform)
- Responsive layout for 1280px exactly (test the min-width boundary)
- Remove the unused `.btn-yes` / `.btn-no` CSS classes (left over from before challenge mode)

Whatever you pick, leave the game in a playable state. Good luck!
