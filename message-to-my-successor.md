# Message to My Successor

## Status after Iteration 12
Suggested questions UI is complete. The chat input area in the active player's turn now has a
collapsible "💡 Suggest a question" toggle button between the input row and the Make a Guess
button. Clicking it reveals a flex-wrap panel of 14 pill-shaped chips, one per character attribute
value (glasses, hat, facial hair, long hair, bald, rosy cheeks, big nose, blue eyes, brown eyes,
blonde/red/white/black/brown hair). Clicking any chip populates `_chatInput` and closes the panel;
the player can then edit or send immediately. The panel also closes automatically on turn change
and after a question is sent.

Build: 0 errors, 0 warnings.

## What to do next

Pick up **to-do.md item 1: Face card visual polish**.

### Goal
Make the 24 character face cards more visually distinctive and expressive. Players spend most of
the game looking at these cards, so improving their readability and character directly improves
the gameplay experience.

### Suggested approach

The current `FaceCard.razor` renders an SVG face programmatically from character attributes.
Improve the rendering quality without adding external assets:

1. **Richer facial features**: add eyebrows (colour matching hair), eyelids/pupils on the eye
   circles, a mouth (smile curve via SVG path), and ear shapes.

2. **Skin tone variety**: add a `SkinTone` attribute (light, medium, dark) or derive a subtle
   variation from the character ID so faces don't all look identical in base colour.

3. **Hair with more shape**: current hair is a basic ellipse cap + rectangles for long hair.
   Consider using SVG `path` arcs for a more natural outline — wavy top for curly, straight
   lines for straight, etc.

4. **Accessory polish**: hats currently rendered as simple polygons — add a hat band or brim
   detail line. Glasses rendered as two circle outlines — add a nose bridge connector. Facial
   hair (beard/moustache) currently a single shape — differentiate beard vs. moustache visually.

5. **Rosy cheeks**: currently two small pink ellipses — consider a soft radial gradient fill
   inside the ellipse for a more blush-like look.

6. **Big nose**: currently a small circle — consider a rounded triangle or wider ellipse for
   better distinctiveness.

### Implementation notes
- All changes live in `FaceCard.razor` (SVG markup) — no model or server changes needed.
- The `CharacterData.All` gallery page (`/gallery`) makes it easy to visually review all 24 at once.
- Keep each SVG change incremental and test at all three sizes (sm, md, lg) after each change.
- The coordinate system is `viewBox="0 0 100 120"`. See MEMORY.md for the key coordinate reference.
- Size classes are controlled by the parent — do not hard-code pixel sizes in the SVG itself.

No special blockers. Good luck — this is a fun iteration.
