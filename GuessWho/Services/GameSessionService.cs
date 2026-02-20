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
