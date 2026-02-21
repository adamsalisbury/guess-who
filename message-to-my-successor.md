# Message to My Successor

## Status after Iteration 5
Turn management is fully wired. Both players see the correct named turn indicator in real time.
The active player has an enabled chat input + Send button + "End Turn" button. The inactive
player has everything disabled and sees "Waiting for [opponent]…" in muted italic.
`GameSession.ActivePlayerToken`, `QuestionAsked`, `IsActivePlayer()`, and `StartNextTurn()` are
all live. Build: 0 errors, 0 warnings.

## What to do next
Pick up **Iteration 6: Chat panel & question flow** — item 1 in `to-do.md`.

### Goal
Make chat functional: active player sends a question (free text); it appears in both logs;
inactive player sees Yes/No buttons inline; their answer is appended to the log with their name.
All in real time via the existing `StateChanged` pattern.

### Server-side changes needed

Add `ChatMessage` model (in `Models/` or inline in GameSession):
```csharp
public sealed class ChatMessage
{
    public string SenderName { get; init; } = "";
    public string Text      { get; init; } = "";
    public ChatMessageKind Kind { get; init; }
}

public enum ChatMessageKind { Question, Answer, System }
```

Add to `GameSession`:
```csharp
public IReadOnlyList<ChatMessage> ChatLog => _chatLog.AsReadOnly();
private readonly List<ChatMessage> _chatLog = [];

/// <summary>
/// Posts a question from the active player. Sets QuestionAsked = true.
/// No-ops if: caller is not active player, QuestionAsked is already true,
/// or text is empty/whitespace.
/// </summary>
public void AskQuestion(string callerToken, string text)
{
    lock (_lock)
    {
        if (callerToken != ActivePlayerToken) return;
        if (QuestionAsked) return;
        var trimmed = text.Trim();
        if (string.IsNullOrEmpty(trimmed)) return;

        var sender = GetPlayer(callerToken)!;
        _chatLog.Add(new ChatMessage { SenderName = sender.Name, Text = trimmed, Kind = ChatMessageKind.Question });
        QuestionAsked = true;
        NotifyStateChanged();
    }
}

/// <summary>
/// Records the inactive player's yes/no answer to the pending question.
/// No-ops if: caller IS the active player, or no question has been asked.
/// </summary>
public void AnswerQuestion(string callerToken, bool yes)
{
    lock (_lock)
    {
        if (callerToken == ActivePlayerToken) return;  // active player can't answer their own question
        if (!QuestionAsked) return;                   // nothing to answer

        var responder = GetPlayer(callerToken)!;
        var answerText = yes ? "Yes" : "No";
        _chatLog.Add(new ChatMessage { SenderName = responder.Name, Text = answerText, Kind = ChatMessageKind.Answer });
        // QuestionAsked stays true — this locks the chat input until the turn ends
        NotifyStateChanged();
    }
}
```

Add a helper to expose whether a pending question is awaiting an answer:
```csharp
/// <summary>True when a question has been asked but not yet answered (no Answer entry after last Question).</summary>
public bool AwaitingAnswer => QuestionAsked &&
    (_chatLog.Count == 0 || _chatLog[^1].Kind != ChatMessageKind.Answer);
```

Add to `GameSessionService`:
```csharp
public void AskQuestion(string code, string token, string text) =>
    GetSession(code)?.AskQuestion(token, text);

public void AnswerQuestion(string code, string token, bool yes) =>
    GetSession(code)?.AnswerQuestion(token, yes);
```

### Client-side changes needed

In `Game.razor` `@code`:
```csharp
private string _chatInput = "";
private ElementReference _chatLogRef;  // for auto-scroll

private async Task SendQuestion()
{
    if (!_isMyTurn || string.IsNullOrWhiteSpace(_chatInput)) return;
    GameSessionService.AskQuestion(Code, MyToken!, _chatInput.Trim());
    _chatInput = "";
    await ScrollChatToBottom();
}

private async Task AnswerQuestion(bool yes)
{
    if (_isMyTurn || _session?.AwaitingAnswer != true) return;
    GameSessionService.AnswerQuestion(Code, MyToken!, yes);
    await ScrollChatToBottom();
}

private async Task ScrollChatToBottom()
{
    // JS interop — simplest approach: set scrollTop via IJSRuntime
}
```

In the chat panel markup, replace the placeholder with a real log render:
```razor
<div class="chat-log" @ref="_chatLogRef">
    @if (_session?.ChatLog.Count == 0)
    {
        <p class="chat-placeholder">The game is afoot…</p>
    }
    else
    {
        @foreach (var msg in _session!.ChatLog)
        {
            <div class="chat-msg chat-msg--@msg.Kind.ToString().ToLower()">
                <span class="chat-msg-sender">@msg.SenderName</span>
                <span class="chat-msg-text">@msg.Text</span>
            </div>
        }
    }
</div>
```

The chat input area needs to branch on three states:
1. **Active player, no question asked yet** — text input + Send button enabled
2. **Active player, question asked** — input locked (QuestionAsked=true); show "Waiting for answer…"
3. **Inactive player, question awaiting answer** — Yes/No buttons; no text input
4. **Inactive player, no question yet** — "Waiting for your turn…" disabled input

End Turn button: already shown for active player in all states (from Iteration 5).

### Auto-scroll chat log
Inject `IJSRuntime` and after each message add:
```csharp
await JS.InvokeVoidAsync("eval",
    "document.querySelector('.chat-log')?.scrollTo({top:999999,behavior:'smooth'})");
```
(quick approach — a proper JS module can be introduced in a later polish iteration)

### CSS additions needed
`.chat-msg` (row), `.chat-msg--question` (player-accent, left-leaning),
`.chat-msg--answer` (secondary, right-leaning or inline answer pill),
`.chat-msg-sender` (bold, gold), `.chat-msg-text`,
`.chat-yn-row` (Yes/No button row), `.btn-yes` (green), `.btn-no` (red).

### Things to remember
- `AwaitingAnswer` is a derived property — no extra server state needed.
- The Yes/No buttons must be shown to the INACTIVE player (not the active one).
  `!_isMyTurn && _session?.AwaitingAnswer == true` is the condition.
- When the active player has `QuestionAsked=true`, lock the text input with `disabled`.
  The "End Turn" button remains available so they can still end without answering.
- Both circuits re-render on `NotifyStateChanged()`, so both players see new messages instantly.
- `OnSessionStateChanged` already calls `InvokeAsync(StateHasChanged)` — no new wiring needed.
- After answer is received, the 10-second countdown (Iteration 3 of turn mechanics) will begin here.
  For now, just lock the input and let the active player click End Turn manually.
