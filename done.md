# Guess Who? — Done Log

Append-only chronological record. Most recent entry at the bottom.

---

## Iteration 1 — Landing page & lobby
**Completed**: 2026-02-20

### What was done
- Scaffolded ASP.NET Core 8 Blazor Server project (`GuessWho/GuessWho.csproj`) with a solution file.
- Defined all domain models: `Character`, `PlayerState`, `GameSession`, and supporting enums
  (`HairColor`, `EyeColor`, `HairLength`, `GamePhase`, `JoinResult`).
- Defined all 24 characters in `CharacterData.All` with full attribute sets (hair colour, eye colour,
  glasses, hat, facial hair, hair length, bald, rosy cheeks, big nose). Character data is ready for
  face card rendering in iteration 2.
- Implemented `GameSessionService` (singleton): creates sessions with unique 4-character codes
  (alphabet excludes O, 0, I, 1), thread-safe player slot assignment via `GameSession._lock`.
- Built **Landing page** (`/`): name entry (required, max 20 chars), "New Game" button (creates session,
  navigates to lobby), "Join Game" button (reveals code input, validates code exists, joins session,
  navigates to lobby). Error states for empty name, bad code, full session.
- Built **Lobby page** (`/lobby/{Code}`): displays both player names with connection status
  (green "Connected" / grey "Waiting…"), game code prominently displayed for sharing, animated
  waiting state with dot-pulse, instant "Starting…" confirmation when both players connect,
  auto-navigation to game page after 1.2s.
- Built **Game page** (`/game/{Code}`): placeholder showing player names and session state.
  Full board implementation deferred to later iterations.
- Wrote custom CSS (`wwwroot/app.css`): dark theme (#0d1117 base), gold accent (#f0a500),
  card-based layouts, smooth animations (spin, fadeSlideDown, dotPulse, popIn), no Bootstrap.
- Removed all template boilerplate (Counter, Weather, NavMenu).
- Build result: **0 errors, 0 warnings**.

### Notes
- Player identity uses URL query params (`?name=…&token=<guid>`). Token is generated on the landing
  page and passed forward. No sessionStorage or cookies needed for iteration 1.
- Inter-player real-time: `GameSession.StateChanged` C# event; both Blazor circuits subscribe.
  Player 2's `Lobby.razor` re-renders when Player 1's session state changes, then auto-navigates.
- Session cleanup not implemented (sessions live until process restart). Tracked in `to-do-technical.md`.

---

## Iteration 2 — Character data & face card rendering
**Completed**: 2026-02-20

### What was done
- Created **`FaceCard.razor`** (`Components/FaceCard.razor` + `FaceCard.razor.css`): a reusable Blazor
  component that renders any of the 24 characters as a stylised SVG portrait derived entirely from
  attribute data — no external image files.
  - **Hair**: non-bald characters get a hair-cap ellipse (colour-matched to attribute) with the face
    ellipse overlaid on top. Long-haired characters also get narrow side panels extending below the face.
    Bald characters skip the hair cap entirely.
  - **Eyes**: white sclera ellipses, coloured iris circles, dark pupils, and bright highlights.
  - **Eyebrows**: arched path coloured by hair colour.
  - **Nose**: subtle curved path normally; wider curved path + tinted oval for big-nose characters.
  - **Mouth**: drawn only when no facial hair (beard visually covers the mouth area).
  - **Rosy cheeks**: semi-transparent pink ellipses on both cheeks.
  - **Facial hair**: closed moustache path + beard ellipse covering lower face, coloured by hair attribute.
  - **Glasses**: two rounded-rect lens frames with bridge and arms in silvery blue.
  - **Hat**: dark crown rect + contrasting band + wider brim rect, drawn last so it sits on top of hair.
  - **Face-down state**: dark card with a grey cross — used for eliminated characters.
  - Three CSS size variants: `sm` (72 px art, for opponent grid), `md` (100 px, own board),
    `lg` (148 px, Mystery Person display).
  - `IsMystery="true"` adds a gold border glow. Optional `OnClick` callback for future interactivity.
- Created **`Gallery.razor`** (`/gallery`): dev-utility page showing all 24 characters in a 6×4 `md`
  grid with small attribute-chip summaries per card, a size-comparison section (sm/md/lg of same
  character), and a card-states section (normal / mystery / face-down).
- Build result: **0 errors, 0 warnings**.

### Notes
- The gallery page is accessible at `/gallery` by URL — no navigation link in the game UI.
  It exists only for visual QA and developer inspection.
- All visual information encoded purely in SVG. Characters are meaningfully distinct at all three sizes.
- FaceCard uses Blazor CSS isolation (`.razor.css`). The SVG scales via `width:100%; height:auto`.
- Hat is always drawn last in SVG painter order, so it correctly sits on top of hair.
