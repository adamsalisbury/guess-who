namespace GuessWho.Models;

/// <summary>
/// Represents a player within a game session: identity, connection, and per-round game data.
/// </summary>
public sealed class PlayerState
{
    public required string Token { get; init; }
    public required string Name { get; init; }
    public required int Slot { get; init; }   // 1 or 2
    public bool IsConnected { get; set; } = true;

    // Round state — populated once CharacterSelection is complete
    public int? MysteryPersonId { get; set; }

    /// <summary>
    /// The shuffled order in which this player sees face cards on their board.
    /// Populated when the session transitions to Playing phase.
    /// Each element is a Character.Id.
    /// </summary>
    public List<int> BoardOrder { get; } = [];

    public HashSet<int> EliminatedIds { get; } = [];

    // Championship scoring
    public int RoundWins { get; set; }
}
