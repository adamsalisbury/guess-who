namespace GuessWho.Models;

/// <summary>
/// A single entry in the game's chat log.
/// Represents a question from the active player, a yes/no answer from their opponent,
/// or a system event (e.g. game started, turn changed).
/// </summary>
public sealed class ChatMessage
{
    public required string SenderName { get; init; }
    public required string Text { get; init; }
    public required ChatMessageKind Kind { get; init; }
}
