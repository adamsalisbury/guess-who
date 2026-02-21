# Message to My Successor

## Status after Iteration 11
Chat log readability has been fully implemented. The chat panel now has three visually distinct
message kinds and labelled turn-boundary dividers:

- **Question bubbles**: gold background, left accent border, flat top-left corner (`border-radius:
  2px 8px 8px 8px`), sender name + "T1/T2/…" turn tag in a header row, question text below.
- **Answer bubbles**: green background, right accent border, flat top-right corner, large bold
  "Yes"/"No" (1.5rem, 900 weight), small sender name below, right-aligned.
- **System messages**: pill-shaped, near-invisible border, muted italic text, no sender label.
- **Turn dividers**: `<div class="chat-turn-divider">` with CSS `::before`/`::after` pseudo-
  element rules creates horizontal lines flanking the centred "Turn N" label. Rendered before
  every Question message after the first. Purely client-side — no server changes.
- `GetChatEntries()` (in `@code`) annotates each `ChatMessage` with a `TurnNumber` and returns
  a `List<ChatEntry>`. This avoids stateful counters in the Razor loop.

Build: 0 errors, 0 warnings.

## What to do next

Pick up **to-do.md item 1: Suggested questions UI**.

### Goal
Give players a one-click way to populate the chat input with common attribute-based yes/no
questions. This speeds up gameplay significantly, especially for new players.

### Suggested approach

A collapsible list of question chips below the chat input, visible only when it's the active
player's turn and no question has been asked yet (same conditions as the question input itself).

#### Question list to include (one per distinct attribute value):
- "Does your person wear glasses?"
- "Does your person have a hat?"
- "Does your person have facial hair?"
- "Does your person have long hair?"
- "Is your person bald?"
- "Does your person have rosy cheeks?"
- "Does your person have a big nose?"
- "Does your person have blue eyes?"
- "Does your person have blonde hair?"
- "Does your person have red hair?"
- "Does your person have white hair?"
- "Does your person have black hair?"
- "Does your person have brown hair?"
- "Is your person a woman?"  ← note: current Character model has no gender field; skip this
  OR add a gender attribute to CharacterData if it fits the character set.

#### UI options (pick one):
1. **Inline chip row**: small pill buttons below the chat input, horizontally scrollable if they
   overflow. Clicking a chip sets `_chatInput` to that question text. The input remains editable
   so the player can tweak it before sending.
2. **Collapsible panel**: a "💡 Suggest a question" toggle button that reveals a scrollable list
   of question chips. Takes less vertical space when collapsed.

Option 1 is simpler to implement; option 2 is neater UX when there are 10+ chips.

#### Implementation notes
- The question list is a static `string[]` constant in the component — no server changes needed.
- The chips should only appear when `_isMyTurn && !_session.QuestionAsked && !_guessModeActive`
  (same conditions as the question input row).
- Clicking a chip sets `_chatInput = questionText` and optionally focuses the input via JS interop.
- Style chips to look consistent with the gold accent palette — small, rounded, tappable but not
  as prominent as the Send button.

No messages. (No special blockers or warnings.)
