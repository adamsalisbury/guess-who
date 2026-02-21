# Guess Who? — Technical Backlog

Non-feature tasks: refactoring, bugs, performance, code quality.

---

## Player reconnect / circuit recovery
If a Blazor circuit drops and the player reloads, they lose their URL params (token). Options:
- Store token in `sessionStorage` via JS interop on first load, read it back on reconnect.
- Or derive a stable token from the session code + player name (simpler, slightly less secure).
Implement before the game board iteration so disconnects don't break active games.

## Unit tests for game logic
Add an xUnit project (`GuessWho.Tests`) and write unit tests for:
- `GameSessionService.CreateSession` — generates valid codes, adds player 1
- `GameSessionService.JoinSession` — NotFound / Full / AlreadyJoined cases
- `GameSession.AddPlayer` — concurrency (parallel joins on same slot)
- `GameSession.StartNextTurn` — no-op when caller is not active; alternates correctly
- `GameSession.IsActivePlayer` — correct token resolution
- `GameSession.LastActivityAt` — updated on every state change
- `GameSessionService.RemoveStaleSessions` — removes GameEnd and idle sessions, disposes timers

## Input sanitisation
Player names and game codes are passed through URL query parameters. Ensure they are properly
decoded and trimmed. Add server-side validation in `GameSessionService` rather than relying solely
on client-side maxlength.

## Suppress or replace `favicon.png`
Replace the default Blazor favicon with a custom one matching the game theme.

## HTTP redirect configuration
`app.UseHttpsRedirection()` was removed for simplicity. Evaluate whether to re-add with a self-
signed dev cert or just leave HTTP-only for development. Document the decision.

---

## Done

### Session lifecycle management (Iteration 16)
`GameSession` now implements `IDisposable` (disposes `_postRoundTimeoutTimer`).
`LastActivityAt` (UTC) is stamped on every `NotifyStateChanged` call.
`GameSessionService.RemoveStaleSessions()` removes sessions in `GameEnd` phase or idle > 2 hours.
`SessionCleanupService` (new `BackgroundService`) runs every 10 minutes and calls `RemoveStaleSessions`.
Registered in `Program.cs` via `AddHostedService<SessionCleanupService>()`.
`RemoveSession` now also disposes the session it removes.
