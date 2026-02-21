using FluentAssertions;
using GuessWho.Models;
using GuessWho.Services;

namespace GuessWho.Tests;

/// <summary>
/// Unit tests for <see cref="GameSessionService"/> — covers session creation, joining,
/// retrieval, stale-session cleanup, and input normalisation.
/// </summary>
public sealed class GameSessionServiceTests
{
    private const string P1Token = "svc-p1-token";
    private const string P2Token = "svc-p2-token";

    // Regex matching the code alphabet (O, 0, I, 1 excluded)
    private const string CodePattern = "^[A-HJ-NP-Z2-9]{4}$";

    // ── CreateSession ─────────────────────────────────────────────────────

    [Fact]
    public void CreateSession_ReturnsSessionWithPlayer1Set()
    {
        var service = new GameSessionService();
        var session = service.CreateSession(P1Token, "Alice");

        session.Player1.Should().NotBeNull();
        session.Player1!.Token.Should().Be(P1Token);
        session.Player1.Name.Should().Be("Alice");
    }

    [Fact]
    public void CreateSession_GeneratesValidCode()
    {
        var service = new GameSessionService();
        var session = service.CreateSession(P1Token, "Alice");

        session.Code.Should().HaveLength(4);
        session.Code.Should().MatchRegex(CodePattern);
    }

    [Fact]
    public void CreateSession_SessionIsRetrievableByCode()
    {
        var service = new GameSessionService();
        var session = service.CreateSession(P1Token, "Alice");

        service.GetSession(session.Code).Should().BeSameAs(session);
    }

    [Fact]
    public void CreateSession_MultipleCallsProduceUniqueCodes()
    {
        var service = new GameSessionService();
        var codes = Enumerable.Range(0, 20)
            .Select(i => service.CreateSession($"token-{i}", $"Player{i}").Code)
            .ToList();

        codes.Distinct().Should().HaveCount(20);
    }

    // ── JoinSession ───────────────────────────────────────────────────────

    [Fact]
    public void JoinSession_ValidCode_ReturnsSuccess()
    {
        var service = new GameSessionService();
        var created = service.CreateSession(P1Token, "Alice");

        var (result, session) = service.JoinSession(created.Code, P2Token, "Bob");

        result.Should().Be(JoinResult.Success);
        session.Should().NotBeNull();
    }

    [Fact]
    public void JoinSession_ValidCode_SessionBecomesFullAfterJoin()
    {
        var service = new GameSessionService();
        var created = service.CreateSession(P1Token, "Alice");
        service.JoinSession(created.Code, P2Token, "Bob");

        created.IsFull.Should().BeTrue();
    }

    [Fact]
    public void JoinSession_InvalidCode_ReturnsNotFound()
    {
        var service = new GameSessionService();
        var (result, session) = service.JoinSession("ZZZZ", P2Token, "Bob");

        result.Should().Be(JoinResult.NotFound);
        session.Should().BeNull();
    }

    [Fact]
    public void JoinSession_FullSession_ReturnsFull()
    {
        var service = new GameSessionService();
        var created = service.CreateSession(P1Token, "Alice");
        service.JoinSession(created.Code, P2Token, "Bob");

        var (result, session) = service.JoinSession(created.Code, "third-token", "Charlie");

        result.Should().Be(JoinResult.Full);
        session.Should().BeNull();
    }

    [Fact]
    public void JoinSession_SameTokenAsExistingPlayer_ReturnsAlreadyJoined()
    {
        var service = new GameSessionService();
        var created = service.CreateSession(P1Token, "Alice");
        service.JoinSession(created.Code, P2Token, "Bob");

        var (result, session) = service.JoinSession(created.Code, P1Token, "Alice");

        result.Should().Be(JoinResult.AlreadyJoined);
        session.Should().NotBeNull();
    }

    [Fact]
    public void JoinSession_NormalisesCodeToUppercase()
    {
        var service = new GameSessionService();
        var created = service.CreateSession(P1Token, "Alice");

        var (result, _) = service.JoinSession(created.Code.ToLowerInvariant(), P2Token, "Bob");

        result.Should().Be(JoinResult.Success);
    }

    [Fact]
    public void JoinSession_TrimsWhitespaceFromCode()
    {
        var service = new GameSessionService();
        var created = service.CreateSession(P1Token, "Alice");

        var (result, _) = service.JoinSession($"  {created.Code}  ", P2Token, "Bob");

        result.Should().Be(JoinResult.Success);
    }

    // ── GetSession ────────────────────────────────────────────────────────

    [Fact]
    public void GetSession_ExistingCode_ReturnsSession()
    {
        var service = new GameSessionService();
        var created = service.CreateSession(P1Token, "Alice");

        service.GetSession(created.Code).Should().BeSameAs(created);
    }

    [Fact]
    public void GetSession_NonExistentCode_ReturnsNull()
    {
        new GameSessionService().GetSession("XXXX").Should().BeNull();
    }

    [Fact]
    public void GetSession_NormalisesCodeCase()
    {
        var service = new GameSessionService();
        var created = service.CreateSession(P1Token, "Alice");

        service.GetSession(created.Code.ToLowerInvariant()).Should().BeSameAs(created);
    }

    // ── Input sanitisation — player names ─────────────────────────────────

    [Fact]
    public void CreateSession_HtmlTagsInName_AreStripped()
    {
        var service = new GameSessionService();
        var session = service.CreateSession(P1Token, "<b>Alice</b>");

        session.Player1!.Name.Should().NotContain("<b>");
        session.Player1.Name.Should().NotContain("</b>");
    }

    [Fact]
    public void CreateSession_NameExceeds20Chars_IsTruncated()
    {
        var service = new GameSessionService();
        var longName = new string('A', 30);
        var session = service.CreateSession(P1Token, longName);

        session.Player1!.Name.Length.Should().BeLessOrEqualTo(20);
    }

    [Fact]
    public void JoinSession_HtmlTagsInName_AreStripped()
    {
        var service = new GameSessionService();
        var created = service.CreateSession(P1Token, "Alice");
        var (_, session) = service.JoinSession(created.Code, P2Token, "<script>alert(1)</script>Bob");

        session!.Player2!.Name.Should().NotContain("<script>");
    }

    // ── RemoveSession ─────────────────────────────────────────────────────

    [Fact]
    public void RemoveSession_ExistingCode_RemovesFromDictionary()
    {
        var service = new GameSessionService();
        var created = service.CreateSession(P1Token, "Alice");

        service.RemoveSession(created.Code);

        service.GetSession(created.Code).Should().BeNull();
    }

    [Fact]
    public void RemoveSession_NonExistentCode_DoesNotThrow()
    {
        var act = () => new GameSessionService().RemoveSession("ZZZZ");
        act.Should().NotThrow();
    }

    // ── RemoveStaleSessions ───────────────────────────────────────────────

    [Fact]
    public void RemoveStaleSessions_GameEndSession_IsRemoved()
    {
        var service = new GameSessionService();
        var created = service.CreateSession(P1Token, "Alice");
        service.JoinSession(created.Code, P2Token, "Bob");

        created.Phase = GamePhase.GameEnd;

        var removed = service.RemoveStaleSessions();

        removed.Should().Be(1);
        service.GetSession(created.Code).Should().BeNull();
    }

    [Fact]
    public void RemoveStaleSessions_ActiveSession_IsKept()
    {
        var service = new GameSessionService();
        service.CreateSession(P1Token, "Alice");

        var removed = service.RemoveStaleSessions();

        removed.Should().Be(0);
    }

    [Fact]
    public void RemoveStaleSessions_IdleSession_IsRemoved()
    {
        var service = new GameSessionService();
        var created = service.CreateSession(P1Token, "Alice");

        // Backdate LastActivityAt beyond the idle timeout using reflection
        var property = typeof(GameSession).GetProperty("LastActivityAt")!;
        property.SetValue(
            created,
            DateTime.UtcNow - GameSessionService.SessionIdleTimeout - TimeSpan.FromMinutes(1));

        var removed = service.RemoveStaleSessions();

        removed.Should().Be(1);
        service.GetSession(created.Code).Should().BeNull();
    }

    [Fact]
    public void RemoveStaleSessions_RecentlyActiveSession_IsNotRemoved()
    {
        var service = new GameSessionService();
        var created = service.CreateSession(P1Token, "Alice");

        // Backdate by less than the timeout — should not be removed
        var property = typeof(GameSession).GetProperty("LastActivityAt")!;
        property.SetValue(
            created,
            DateTime.UtcNow - GameSessionService.SessionIdleTimeout + TimeSpan.FromMinutes(30));

        var removed = service.RemoveStaleSessions();

        removed.Should().Be(0);
        service.GetSession(created.Code).Should().NotBeNull();
    }

    [Fact]
    public void RemoveStaleSessions_EmptyService_ReturnsZero()
    {
        new GameSessionService().RemoveStaleSessions().Should().Be(0);
    }

    [Fact]
    public void RemoveStaleSessions_MixedSessions_OnlyRemovesStale()
    {
        var service = new GameSessionService();

        // Active session — should be kept
        var active = service.CreateSession("active-token", "Player");

        // GameEnd session — should be removed
        var ended = service.CreateSession("ended-token", "OldPlayer");
        ended.Phase = GamePhase.GameEnd;

        var removed = service.RemoveStaleSessions();

        removed.Should().Be(1);
        service.GetSession(active.Code).Should().NotBeNull();
        service.GetSession(ended.Code).Should().BeNull();
    }
}
