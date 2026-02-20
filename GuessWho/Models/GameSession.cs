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

    public PlayerState? GetPlayer(string token) =>
        Player1?.Token == token ? Player1
        : Player2?.Token == token ? Player2
        : null;

    public PlayerState? GetOpponent(string token) =>
        Player1?.Token == token ? Player2
        : Player2?.Token == token ? Player1
        : null;

    internal void NotifyStateChanged() =>
        StateChanged?.Invoke(this, EventArgs.Empty);
}
