# Message to My Successor

## Status after Iteration 1
Iteration 1 is complete and pushed. The landing page, lobby, and a game placeholder are all working.
The build is clean (0 errors, 0 warnings).

## What to do next
Pick up **Iteration 2: Character data & face card rendering** from `to-do.md`.

### Goal
Render all 24 characters as face cards built purely from CSS/inline SVG — no image files.
Each card should be meaningfully distinct and clearly readable at the sizes used by the game board
(both the compact opponent grid and the full-size own board).

### Suggested approach
1. Create a `FaceCard.razor` component that accepts a `Character` parameter (or nullable for face-down).
2. Render a face silhouette in SVG (or styled divs): oval head, skin tone, eyes (with colour), hair
   (shaped by length/colour/bald), glasses overlay, hat overlay, facial hair overlay, rosy cheeks flush,
   big nose shape. Use CSS variables or inline styles driven by attribute values.
3. Add a `CharacterGallery.razor` page at `/gallery` so you can visually inspect all 24 cards at once.
4. Ensure face-down state (eliminated card) is also styled — typically a solid grey/dark card.
5. Display the character's name below the card.

### Key design parameters
- Own board grid: 6×4 cards, each card roughly 120–140px wide within the left column.
- Opponent board grid: 6×4 cards, smaller (compact view), roughly 80–100px wide.
- Mystery Person display (right panel): single large card, ~160px wide.
- Cards must look good at all three sizes.

### Things to know
- All 24 characters are already defined in `CharacterData.All` with every attribute populated.
- The `HairLength`, `Bald`, `HairColor`, `EyeColor`, `Glasses`, `Hat`, `FacialHair`, `RosyCheeks`,
  `BigNose` attributes on `Character` are the inputs for rendering.
- The `Character.Id` field is 1-based (1 through 24).
- The game page (`/game/{Code}`) is a placeholder — don't wire up the full board yet. Just add the
  gallery page and the component, used from there.

## Technical note
No messages pending about bugs or blockers. The session event pattern works correctly —
verified by running both browser tabs locally and watching the lobby update in real time.

No messages.
