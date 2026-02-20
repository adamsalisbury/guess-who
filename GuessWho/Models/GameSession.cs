using GuessWho.Data;

namespace GuessWho.Models;

/// <summary>
/// Server-side game session. Singleton service holds all active sessions.
/// State changes are broadcast to subscribed Blazor components via the StateChanged event.
/// </summary>
public sealed class GameSession
{
    private readonly object _lock = new();

    public required string Code { get; init; }
    public PlayerState? Player1 { get; private set; }
    public PlayerState? Player2 { get; private set; }
    public GamePhase Phase { get; set; } = GamePhase.Lobby;
    public int RoundNumber { get; set; }

    /// <summary>Raised whenever session state changes. Subscribers call StateHasChanged.</summary>
    public event EventHandler? StateChanged;

    public bool IsFull => Player1 is not null && Player2 is not null;

    /// <summary>
    /// Attempts to add a player to the session.
    /// Returns AlreadyJoined if the token is recognised (safe reconnect).
    /// Returns Full if both slots are taken by different players.
    /// </summary>
    public JoinResult AddPlayer(string token, string name)
    {
        lock (_lock)
        {
            // Safe reconnect — same token re-joins
            if (Player1?.Token == token || Player2?.Token == token)
                return JoinResult.AlreadyJoined;

            if (Player1 is null)
            {
                Player1 = new PlayerState { Token = token, Name = name, Slot = 1 };
                NotifyStateChanged();
                return JoinResult.Success;
            }

            if (Player2 is null)
            {
                Player2 = new PlayerState { Token = token, Name = name, Slot = 2 };
                Phase = GamePhase.CharacterSelection;
                NotifyStateChanged();
                return JoinResult.Success;
            }

            return JoinResult.Full;
        }
    }

    /// <summary>
    /// Records a player's Mystery Person choice. Once both players have selected,
    /// advances the session to the Playing phase and sets RoundNumber to 1.
    /// No-ops if the token is unrecognised or the player has already confirmed.
    /// </summary>
    public void SelectMysteryPerson(string token, int characterId)
    {
        lock (_lock)
        {
            var player = GetPlayer(token);
            if (player is null || player.MysteryPersonId.HasValue) return;

            player.MysteryPersonId = characterId;

            if (Player1?.MysteryPersonId.HasValue == true && Player2?.MysteryPersonId.HasValue == true)
            {
                Phase = GamePhase.Playing;
                RoundNumber = 1;
                ShuffleBoardOrders();
            }

            NotifyStateChanged();
        }
    }

    public PlayerState? GetPlayer(string token) =>
        Player1?.Token == token ? Player1
        : Player2?.Token == token ? Player2
        : null;

    public PlayerState? GetOpponent(string token) =>
        Player1?.Token == token ? Player2
        : Player2?.Token == token ? Player1
        : null;

    /// <summary>
    /// Assigns each player a unique randomly-shuffled ordering of all 24 character IDs.
    /// Called once when the session transitions from CharacterSelection to Playing.
    /// Must be called inside _lock.
    /// </summary>
    private void ShuffleBoardOrders()
    {
        var allIds = CharacterData.All.Select(c => c.Id).ToArray();

        // Player 1's board — shuffle a fresh copy
        var p1Ids = (int[])allIds.Clone();
        Random.Shared.Shuffle(p1Ids);
        Player1!.BoardOrder.Clear();
        Player1.BoardOrder.AddRange(p1Ids);

        // Player 2's board — shuffle another independent copy
        var p2Ids = (int[])allIds.Clone();
        Random.Shared.Shuffle(p2Ids);
        Player2!.BoardOrder.Clear();
        Player2.BoardOrder.AddRange(p2Ids);
    }

    internal void NotifyStateChanged() =>
        StateChanged?.Invoke(this, EventArgs.Empty);
}
