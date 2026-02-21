# Guess Who? — Technical Backlog

Non-feature tasks: refactoring, bugs, performance, code quality.

---

## Session lifecycle management
`GameSessionService` currently holds sessions indefinitely (until process restart). Implement a
cleanup mechanism: e.g. a background `IHostedService` that removes sessions where both players
have disconnected or the session is older than N hours (2h suggested).

## Player reconnect / circuit recovery
If a Blazor circuit drops and the player reloads, they lose their URL params (token). Options:
- Store token in `sessionStorage` via JS interop on first load, read it back on reconnect.
- Or derive a stable token from the session code + player name (simpler, slightly less secure).
Implement before the game board iteration so disconnects don't break active games.

## Remove Bootstrap dependency
The template includes Bootstrap CSS. We're not using it (custom CSS in `app.css`). Strip it from
`App.razor` and remove the `bootstrap/` folder from `wwwroot/` to reduce payload.

## Suppress or replace `favicon.png`
Replace the default Blazor favicon with a custom one matching the game theme.

## HTTP redirect configuration
`app.UseHttpsRedirection()` was removed for simplicity. Evaluate whether to re-add with a self-
signed dev cert or just leave HTTP-only for development. Document the decision.

## Unit tests for game logic
Add an xUnit project (`GuessWho.Tests`) and write unit tests for:
- `GameSessionService.CreateSession` — generates valid codes, adds player 1
- `GameSessionService.JoinSession` — NotFound / Full / AlreadyJoined cases
- `GameSession.AddPlayer` — concurrency (parallel joins on same slot)
- `GameSession.StartNextTurn` — no-op when caller is not active; alternates correctly
- `GameSession.IsActivePlayer` — correct token resolution
- Win/loss/round-end state transitions (when added)

## Input sanitisation
Player names and game codes are passed through URL query parameters. Ensure they are properly
decoded and trimmed. Add server-side validation in `GameSessionService` rather than relying solely
on client-side maxlength.
