using System.Collections.Concurrent;
using GuessWho.Models;

namespace GuessWho.Services;

/// <summary>
/// Singleton service that owns all active game sessions.
/// Thread-safe; session-level state changes are coordinated inside GameSession._lock.
/// </summary>
public sealed class GameSessionService
{
    private readonly ConcurrentDictionary<string, GameSession> _sessions = new();

    // Uppercase letters + digits, excluding visually ambiguous characters O, 0, I, 1
    private static readonly char[] CodeAlphabet =
        "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();

    private const int CodeLength = 4;

    /// <summary>
    /// Creates a new session and immediately adds the creator as Player 1.
    /// Returns the fully initialised session with a unique game code.
    /// </summary>
    public GameSession CreateSession(string playerToken, string playerName)
    {
        // Spin until we successfully claim a unique code
        while (true)
        {
            var code = GenerateCode();
            var session = new GameSession { Code = code };

            if (_sessions.TryAdd(code, session))
            {
                session.AddPlayer(playerToken, playerName);
                return session;
            }
        }
    }

    /// <summary>
    /// Attempts to join an existing session by code.
    /// The returned session is non-null on Success or AlreadyJoined.
    /// </summary>
    public (JoinResult Result, GameSession? Session) JoinSession(
        string code,
        string playerToken,
        string playerName)
    {
        var normalised = code.ToUpperInvariant().Trim();

        if (!_sessions.TryGetValue(normalised, out var session))
            return (JoinResult.NotFound, null);

        var result = session.AddPlayer(playerToken, playerName);

        var returnedSession = result is JoinResult.Success or JoinResult.AlreadyJoined
            ? session
            : null;

        return (result, returnedSession);
    }

    public GameSession? GetSession(string code) =>
        _sessions.TryGetValue(code.ToUpperInvariant().Trim(), out var s) ? s : null;

    /// <summary>
    /// Records the specified player's two Mystery People selections for Challenge Mode.
    /// Delegates to GameSession.SelectMysteryPeople; no-ops if session not found.
    /// </summary>
    public void SelectMysteryPeople(string code, string playerToken, int id1, int id2) =>
        GetSession(code)?.SelectMysteryPeople(playerToken, id1, id2);

    /// <summary>
    /// Passes the turn to the opposing player.
    /// No-ops if the session is not found or the caller is not the active player.
    /// </summary>
    public void StartNextTurn(string code, string playerToken) =>
        GetSession(code)?.StartNextTurn(playerToken);

    /// <summary>
    /// Posts a question from the active player into the session's chat log.
    /// No-ops if the session is not found, caller is not active, or question already asked.
    /// </summary>
    public void AskQuestion(string code, string playerToken, string text) =>
        GetSession(code)?.AskQuestion(playerToken, text);

    /// <summary>
    /// Records the inactive player's answer to the pending question.
    /// In Challenge Mode the answer is one of "Both", "One of them", "Neither".
    /// No-ops if the session is not found, caller is active, or nothing is awaiting an answer.
    /// </summary>
    public void AnswerQuestion(string code, string playerToken, string answer) =>
        GetSession(code)?.AnswerQuestion(playerToken, answer);

    /// <summary>
    /// Records that the active player has eliminated a character from their board.
    /// No-ops if the session is not found, caller is not active, or character is immune/already eliminated.
    /// </summary>
    public void EliminateCharacter(string code, string playerToken, int characterId) =>
        GetSession(code)?.EliminateCharacter(playerToken, characterId);

    /// <summary>
    /// Resolves the round by the active player guessing both of the opponent's Mystery People.
    /// Both guessed IDs must match (order-independent). Wrong guess on either = immediate loss.
    /// No-ops if the session is not found, caller is not active, or phase is not Playing.
    /// </summary>
    public void MakeGuess(string code, string playerToken, int guessId1, int guessId2) =>
        GetSession(code)?.MakeGuess(playerToken, guessId1, guessId2);

    /// <summary>
    /// Records a player's post-round decision (NewRound or EndGame).
    /// If both players choose the same option it executes immediately; otherwise waits up to 60 seconds.
    /// No-ops if the session is not found, caller is not a participant, or phase is not RoundEnd.
    /// </summary>
    public void MakePostRoundDecision(string code, string playerToken, PostRoundDecision decision) =>
        GetSession(code)?.MakePostRoundDecision(playerToken, decision);

    /// <summary>Removes sessions that are empty or abandoned (no players for > 2 hours).</summary>
    public void RemoveSession(string code) =>
        _sessions.TryRemove(code.ToUpperInvariant().Trim(), out _);

    private static string GenerateCode()
    {
        var chars = new char[CodeLength];
        for (var i = 0; i < CodeLength; i++)
            chars[i] = CodeAlphabet[Random.Shared.Next(CodeAlphabet.Length)];
        return new string(chars);
    }
}
