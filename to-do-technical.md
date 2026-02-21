# Guess Who? — Technical Backlog

Non-feature tasks: refactoring, bugs, performance, code quality.

---

*All planned technical items have been completed. The technical backlog is empty.*

---

## Done

### Unit tests for game logic (Iteration 18)
`GuessWho.Tests` xUnit project added to the solution. 93 tests across two files:
- `GameSessionTests.cs` — `AddPlayer`, `IsActivePlayer`, `SelectMysteryPeople`, `StartNextTurn`,
  `AskQuestion`, `AnswerQuestion`, `AwaitingAnswer`, `EliminateCharacter`, `MakeGuess`,
  `MakePostRoundDecision`, `GetPostRoundDecision`, `LastActivityAt`, `GetPlayer`/`GetOpponent`.
- `GameSessionServiceTests.cs` — `CreateSession`, `JoinSession`, `GetSession`, `RemoveSession`,
  `RemoveStaleSessions` (GameEnd + idle + mixed), input sanitisation coverage.
All 93 tests pass (`dotnet test` green). `FrameworkReference Include="Microsoft.AspNetCore.App"`
added to the test `.csproj` to compile against a `Sdk.Web` project reference.

### Input sanitisation (Iteration 18)
Server-side name validation added to `GameSessionService.CreateSession` and `JoinSession`.
`SanitiseName(string name)`: strips HTML tags (compiled `Regex("<[^>]*>")`), trims whitespace,
truncates to 20 characters, falls back to `"Player"` if empty after stripping.
`using System.Text.RegularExpressions` added to `GameSessionService.cs`.

### Custom favicon (Iteration 18)
`wwwroot/favicon.svg` — SVG face-card silhouette with "?" on a dark `#0d1117` background,
gold (`#f0a500`) head shape. `App.razor` updated to reference the SVG favicon first,
with the original `favicon.png` as a legacy PNG fallback.

### HTTP redirect configuration (Iteration 18)
Decision documented in `project.md`: `app.UseHttpsRedirection()` remains absent.
Game targets development/LAN contexts; HTTPS belongs at the reverse-proxy layer if deployed.

### Player reconnect / circuit recovery (Iteration 17)
`wwwroot/js/storage.js` ES module with `saveSession(code, name, token)`, `loadSession(code)`,
`clearSession()` backed by `sessionStorage`. `Game.razor` imports the module in
`OnAfterRenderAsync(firstRender)`: saves `{code, name, token}` when URL params are present; reads
stored data and re-navigates to the full URL when params are missing. `ClearStoredSession()` helper
clears the entry before navigating home on `GameEnd`. All JS interop errors swallowed silently for
graceful degradation. Module loaded via `await JS.InvokeAsync<IJSObjectReference>("import", "./js/storage.js")`.

### Session lifecycle management (Iteration 16)
`GameSession` now implements `IDisposable` (disposes `_postRoundTimeoutTimer`).
`LastActivityAt` (UTC) is stamped on every `NotifyStateChanged` call.
`GameSessionService.RemoveStaleSessions()` removes sessions in `GameEnd` phase or idle > 2 hours.
`SessionCleanupService` (new `BackgroundService`) runs every 10 minutes and calls `RemoveStaleSessions`.
Registered in `Program.cs` via `AddHostedService<SessionCleanupService>()`.
`RemoveSession` now also disposes the session it removes.
