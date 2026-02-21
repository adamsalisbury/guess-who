using GuessWho.Data;

namespace GuessWho.Models;

/// <summary>
/// Server-side game session. Singleton service holds all active sessions.
/// State changes are broadcast to subscribed Blazor components via the StateChanged event.
/// </summary>
public sealed class GameSession
{
    private readonly object _lock = new();

    /// <summary>Number of round wins required to win the match.</summary>
    private const int WinsToWin = 5;

    public required string Code { get; init; }
    public PlayerState? Player1 { get; private set; }
    public PlayerState? Player2 { get; private set; }
    public GamePhase Phase { get; set; } = GamePhase.Lobby;
    public int RoundNumber { get; set; }

    /// <summary>Token of the player whose turn it is. Empty string before the Playing phase begins.</summary>
    public string ActivePlayerToken { get; private set; } = "";

    /// <summary>
    /// Whether a question has already been asked this turn (answered or not).
    /// When true, the chat input is locked for the active player until the turn passes.
    /// </summary>
    public bool QuestionAsked { get; private set; }

    /// <summary>
    /// True when a question has been asked this turn but the opponent has not yet answered.
    /// Drives the Yes/No button display for the inactive player.
    /// </summary>
    public bool AwaitingAnswer =>
        QuestionAsked && (_chatLog.Count == 0 || _chatLog[^1].Kind != ChatMessageKind.Answer);

    /// <summary>How the current round ended. Null while the round is still in progress.</summary>
    public RoundEndReason? EndReason { get; private set; }

    /// <summary>Token of the player who won the most-recently-completed round. Null during play.</summary>
    public string? RoundWinnerToken { get; private set; }

    /// <summary>
    /// True when either player has accumulated enough wins to end the match.
    /// Set in MakeGuess; cleared in ResetRoundState (Play Again resets scores before clearing).
    /// </summary>
    public bool IsMatchOver { get; private set; }

    /// <summary>Token of the player who won the match. Null until a match winner is determined.</summary>
    public string? MatchWinnerToken { get; private set; }

    /// <summary>
    /// UTC time when the post-answer countdown started. Null when no countdown is running.
    /// Read from any thread for display purposes (no lock required for reads).
    /// </summary>
    public DateTime? CountdownStartedAt { get; private set; }

    /// <summary>True while the post-answer 10-second countdown is ticking.</summary>
    public bool CountdownActive => CountdownStartedAt.HasValue;

    /// <summary>Duration of the post-answer turn-end countdown in seconds.</summary>
    public const int CountdownSeconds = 10;

    private readonly List<ChatMessage> _chatLog = [];

    /// <summary>Ordered list of all messages in the current round's chat log.</summary>
    public IReadOnlyList<ChatMessage> ChatLog => _chatLog.AsReadOnly();

    // ── Post-round consensus state ────────────────────────────────────────

    /// <summary>
    /// Records each player's post-round decision (NewRound or EndGame).
    /// Populated as players click buttons in the round-end overlay.
    /// </summary>
    private readonly Dictionary<string, PostRoundDecision> _postRoundDecisions = new();

    /// <summary>
    /// Server-side timer that fires 60 seconds after the first player makes a post-round decision.
    /// On expiry defaults to EndGame if consensus has not been reached.
    /// </summary>
    private System.Threading.Timer? _postRoundTimeoutTimer;

    // ── Core properties ───────────────────────────────────────────────────

    /// <summary>Returns true if the specified token belongs to the currently active player.</summary>
    public bool IsActivePlayer(string token) =>
        !string.IsNullOrEmpty(token) && token == ActivePlayerToken;

    /// <summary>Raised whenever session state changes. Subscribers call StateHasChanged.</summary>
    public event EventHandler? StateChanged;

    public bool IsFull => Player1 is not null && Player2 is not null;

    // ── Player management ─────────────────────────────────────────────────

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

    // ── Mystery Person selection ──────────────────────────────────────────

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
                // Player 1 always takes the first turn
                ActivePlayerToken = Player1!.Token;
            }

            NotifyStateChanged();
        }
    }

    // ── Turn management ───────────────────────────────────────────────────

    /// <summary>
    /// Passes the turn to the opposing player and resets per-turn state.
    /// No-ops if the caller is not the current active player (prevents double-fire).
    /// </summary>
    public void StartNextTurn(string callerToken)
    {
        lock (_lock)
        {
            if (callerToken != ActivePlayerToken) return;

            ActivePlayerToken = ActivePlayerToken == Player1?.Token
                ? Player2!.Token
                : Player1!.Token;

            QuestionAsked = false;
            CountdownStartedAt = null;  // cancel any running countdown
            NotifyStateChanged();
        }
    }

    // ── Chat / question flow ──────────────────────────────────────────────

    /// <summary>
    /// Posts a question from the active player into the chat log and locks further questions
    /// for this turn. No-ops if the caller is not the active player, a question has already
    /// been asked this turn, or the text is empty.
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
            _chatLog.Add(new ChatMessage
            {
                SenderName = sender.Name,
                Text = trimmed,
                Kind = ChatMessageKind.Question
            });
            QuestionAsked = true;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Records the inactive player's yes or no answer to the pending question.
    /// No-ops if the caller is the active player or no question is awaiting an answer.
    /// </summary>
    public void AnswerQuestion(string callerToken, bool yes)
    {
        lock (_lock)
        {
            if (callerToken == ActivePlayerToken) return;  // active player cannot answer their own question
            if (!AwaitingAnswer) return;                   // nothing pending

            var responder = GetPlayer(callerToken)!;
            _chatLog.Add(new ChatMessage
            {
                SenderName = responder.Name,
                Text = yes ? "Yes" : "No",
                Kind = ChatMessageKind.Answer
            });
            // QuestionAsked stays true — input remains locked until the turn ends
            // Start the post-answer countdown so the active player sees the timer ticking
            CountdownStartedAt = DateTime.UtcNow;
            NotifyStateChanged();
        }
    }

    // ── Face elimination ──────────────────────────────────────────────────

    /// <summary>
    /// Records that the active player has eliminated a character from their board.
    /// No-ops if: session is not in Playing phase, caller is not the active player,
    /// the character is the caller's Mystery Person (immune), or already eliminated.
    /// </summary>
    public void EliminateCharacter(string callerToken, int characterId)
    {
        lock (_lock)
        {
            if (Phase != GamePhase.Playing) return;
            if (callerToken != ActivePlayerToken) return;   // only active player can eliminate

            var player = GetPlayer(callerToken);
            if (player is null) return;
            if (player.MysteryPersonId == characterId) return;  // Mystery Person is immune
            if (!player.EliminatedIds.Add(characterId)) return; // already eliminated — no-op

            NotifyStateChanged();
        }
    }

    // ── Guessing & round resolution ───────────────────────────────────────

    /// <summary>
    /// Resolves the round by the active player guessing the opponent's Mystery Person.
    /// A correct guess wins the round; wrong loses it immediately.
    /// Sets IsMatchOver when either player reaches WinsToWin.
    /// No-ops if phase is not Playing or caller is not the active player.
    /// </summary>
    public void MakeGuess(string callerToken, int guessedCharacterId)
    {
        lock (_lock)
        {
            if (Phase != GamePhase.Playing) return;
            if (callerToken != ActivePlayerToken) return;

            var opponent = GetOpponent(callerToken);
            if (opponent is null) return;

            var guesser = GetPlayer(callerToken)!;
            var isCorrect = guessedCharacterId == opponent.MysteryPersonId;

            if (isCorrect)
            {
                RoundWinnerToken = callerToken;
                guesser.RoundWins++;
            }
            else
            {
                // Wrong guess — opponent wins the round
                RoundWinnerToken = opponent.Token;
                opponent.RoundWins++;
            }

            // Check for match winner
            var roundWinner = GetPlayer(RoundWinnerToken!)!;
            if (roundWinner.RoundWins >= WinsToWin)
            {
                IsMatchOver = true;
                MatchWinnerToken = RoundWinnerToken;
            }

            EndReason = isCorrect ? RoundEndReason.CorrectGuess : RoundEndReason.WrongGuess;
            CountdownStartedAt = null;  // cancel any running countdown
            Phase = GamePhase.RoundEnd;
            NotifyStateChanged();
        }
    }

    // ── Post-round consensus ──────────────────────────────────────────────

    /// <summary>
    /// Records a player's post-round decision (NewRound or EndGame).
    /// If both players choose the same option it executes immediately.
    /// Starts a 60-second server-side timeout on the first decision;
    /// if consensus is not reached in time the game ends.
    /// No-ops if phase is not RoundEnd or the caller is not a session participant.
    /// </summary>
    public void MakePostRoundDecision(string callerToken, PostRoundDecision decision)
    {
        lock (_lock)
        {
            if (Phase != GamePhase.RoundEnd) return;
            if (GetPlayer(callerToken) is null) return;

            _postRoundDecisions[callerToken] = decision;

            // Start the 60-second timeout on the first decision (if not already running)
            _postRoundTimeoutTimer ??= new System.Threading.Timer(_ =>
            {
                lock (_lock)
                {
                    if (Phase == GamePhase.RoundEnd)
                        ExecuteEndGame();
                }
            }, null, TimeSpan.FromSeconds(60), Timeout.InfiniteTimeSpan);

            // Check if both players have made the same decision
            if (_postRoundDecisions.Count == 2
                && _postRoundDecisions.TryGetValue(Player1!.Token, out var p1Decision)
                && _postRoundDecisions.TryGetValue(Player2!.Token, out var p2Decision)
                && p1Decision == p2Decision)
            {
                _postRoundTimeoutTimer?.Dispose();
                _postRoundTimeoutTimer = null;

                if (p1Decision == PostRoundDecision.NewRound)
                    ExecuteNewRound();
                else
                    ExecuteEndGame();

                return;  // NotifyStateChanged is called inside Execute*
            }

            // One or both players have decided, but no consensus yet — update display
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Returns the post-round decision made by the specified player, or null if they
    /// have not yet decided. Safe to call from any thread for display purposes.
    /// </summary>
    public PostRoundDecision? GetPostRoundDecision(string token) =>
        _postRoundDecisions.TryGetValue(token, out var d) ? d : null;

    // ── Private execution helpers ─────────────────────────────────────────

    /// <summary>
    /// Begins a new round. If a match just ended (Play Again), resets both players'
    /// round win counts to zero before starting fresh.
    /// Must be called inside _lock.
    /// </summary>
    private void ExecuteNewRound()
    {
        // Play Again — match is over, reset championship scores to start fresh
        if (IsMatchOver)
        {
            Player1!.RoundWins = 0;
            Player2!.RoundWins = 0;
        }

        RoundNumber++;
        ResetRoundState();
        Phase = GamePhase.CharacterSelection;
        NotifyStateChanged();
    }

    /// <summary>
    /// Ends the game session, causing both clients to navigate home.
    /// Must be called inside _lock.
    /// </summary>
    private void ExecuteEndGame()
    {
        Phase = GamePhase.GameEnd;
        NotifyStateChanged();
    }

    /// <summary>
    /// Resets all per-round mutable state on both players and on the session.
    /// Championship scores (RoundWins) are NOT touched here — Play Again resets them
    /// in ExecuteNewRound before calling this method.
    /// Must be called inside _lock.
    /// </summary>
    private void ResetRoundState()
    {
        foreach (var player in new[] { Player1, Player2 })
        {
            if (player is null) continue;
            player.MysteryPersonId = null;
            player.EliminatedIds.Clear();
            player.BoardOrder.Clear();
        }

        _chatLog.Clear();
        QuestionAsked = false;
        CountdownStartedAt = null;
        ActivePlayerToken = "";
        RoundWinnerToken = null;
        EndReason = null;

        // Clear post-round consensus state
        _postRoundDecisions.Clear();
        _postRoundTimeoutTimer?.Dispose();
        _postRoundTimeoutTimer = null;
        IsMatchOver = false;
        MatchWinnerToken = null;
    }

    // ── Lookups ───────────────────────────────────────────────────────────

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
        _chatLog.Clear();
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
