# Message to My Successor

## Status after Iteration 13
Face card visual polish is complete. The 24 character cards are now significantly more
expressive and visually distinct. Build: 0 errors, 0 warnings.

## What changed (summary)
All changes were in `FaceCard.razor` only — single file, no server or model changes.

New visual features:
- 3 skin tone variants (light / medium / deeper warm) derived from `Id % 3`
- Subtle card background tint per skin tone
- Neck shape below the chin
- Inner ear detail (small inner ellipse)
- Hair sheen arc on the hair cap and side panels
- Short hair temple patches for extra volume
- Curved SVG path long hair panels (tapered, natural shape)
- Thicker eyebrows (stroke-width 2.2)
- Two-layer rosy cheeks for soft blush
- Big nose now shows two nostrils with shadow holes
- Lip highlight above the smile
- Facial hair: black/brown hair → full beard + moustache; others → moustache only
- Glasses lens glare (tiny diagonal highlights)
- Hat variety: even-Id → fedora; odd-Id → rounded cap (colour matched to hair)

## What to do next

Pick up **to-do.md item 1: Challenge mode**.

### Goal
Each player secretly picks TWO Mystery People instead of one. This changes the question/answer
dynamic (answers can be "Both", "Either", "Neither" for presence/absence attributes), and the
win condition (correctly name both of the opponent's Mystery People).

### Suggested approach

This is a significant design and implementation change. Key decisions to make:

1. **Selection phase**: The CharacterSelection screen currently lets each player pick one character.
   Extend it to allow two selections with a "You've selected 1 of 2" indicator. Confirm only
   when both are chosen.

2. **Answer options**: Replace the Yes/No buttons with **Yes (Both)**, **Yes (One)**, **No** — or
   the simpler **Both / One / Neither** framing. Decide which is more intuitive for players.
   Note: "Both" and "Neither" are clear; "One (but not both)" is slightly awkward — consider
   labelling it "One of them" or just "One".

3. **Guessing**: The guess flow needs to change — active player must name both Mystery People.
   Either via two sequential face clicks, or a "select two and confirm" pattern on the board.
   A wrong guess on either (or failing to name both correctly) is an immediate loss.

4. **MysteryPersonId** on `PlayerState`: Currently `int?`. Change to a `List<int>` or
   `(int First, int Second)?` value type. All downstream logic (`MakeGuess`, overlays, reveals)
   needs to handle two characters.

5. **Board immunity**: Both Mystery Person cards must be immune from elimination on the own board.
   The `IsMystery` flag on `FaceCard` needs to be set for both.

6. **End-of-round reveal overlay**: Show both of each player's Mystery People (4 cards total).
   The layout will need careful arrangement in `Game.razor.css`.

7. **Server methods to update**: `SelectMysteryPerson` → `SelectMysteryPeople(token, id1, id2)`.
   `MakeGuess(token, id1, id2)` → compare both against opponent's two choices.

### Scope note
This is the largest remaining feature. Consider whether to implement it as one iteration or
split into: (a) selection + board immunity, and (b) guessing + round end reveal.

No special blockers. Good luck!
