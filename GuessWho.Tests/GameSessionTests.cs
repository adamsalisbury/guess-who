using FluentAssertions;
using GuessWho.Models;

namespace GuessWho.Tests;

/// <summary>
/// Unit tests for <see cref="GameSession"/> — covers player management, turn flow,
/// question/answer mechanics, face elimination, guess resolution, and post-round consensus.
/// </summary>
public sealed class GameSessionTests
{
    // ── Shared tokens ─────────────────────────────────────────────────────

    private const string P1Token = "test-p1-token";
    private const string P2Token = "test-p2-token";

    // P1's mystery IDs: 1, 2  |  P2's mystery IDs: 3, 4
    private const int P1Mystery1 = 1;
    private const int P1Mystery2 = 2;
    private const int P2Mystery1 = 3;
    private const int P2Mystery2 = 4;

    // ── Helpers ───────────────────────────────────────────────────────────

    private static GameSession CreateLobbySession() =>
        new() { Code = "TEST" };

    private static GameSession CreateFullSession()
    {
        var session = CreateLobbySession();
        session.AddPlayer(P1Token, "Alice");
        session.AddPlayer(P2Token, "Bob");
        return session;
    }

    /// <summary>
    /// Returns a session in <see cref="GamePhase.Playing"/> state.
    /// P1's mystery people are IDs 1 and 2; P2's are 3 and 4. P1 takes the first turn.
    /// </summary>
    private static GameSession CreatePlayingSession()
    {
        var session = CreateFullSession();
        session.SelectMysteryPeople(P1Token, P1Mystery1, P1Mystery2);
        session.SelectMysteryPeople(P2Token, P2Mystery1, P2Mystery2);
        return session;
    }

    /// <summary>
    /// Returns a session in <see cref="GamePhase.RoundEnd"/> state — P1 correctly guessed P2's people.
    /// </summary>
    private static GameSession CreateRoundEndSession()
    {
        var session = CreatePlayingSession();
        session.MakeGuess(P1Token, P2Mystery1, P2Mystery2);
        return session;
    }

    // ── AddPlayer ─────────────────────────────────────────────────────────

    [Fact]
    public void AddPlayer_FirstPlayer_ReturnsSuccess()
    {
        var session = CreateLobbySession();
        session.AddPlayer(P1Token, "Alice").Should().Be(JoinResult.Success);
    }

    [Fact]
    public void AddPlayer_FirstPlayer_SetsPlayer1WithCorrectValues()
    {
        var session = CreateLobbySession();
        session.AddPlayer(P1Token, "Alice");

        session.Player1.Should().NotBeNull();
        session.Player1!.Token.Should().Be(P1Token);
        session.Player1.Name.Should().Be("Alice");
        session.Player1.Slot.Should().Be(1);
    }

    [Fact]
    public void AddPlayer_SecondPlayer_ReturnsSuccess()
    {
        var session = CreateLobbySession();
        session.AddPlayer(P1Token, "Alice");
        session.AddPlayer(P2Token, "Bob").Should().Be(JoinResult.Success);
    }

    [Fact]
    public void AddPlayer_SecondPlayer_AdvancesPhaseToCharacterSelection()
    {
        var session = CreateFullSession();
        session.Phase.Should().Be(GamePhase.CharacterSelection);
    }

    [Fact]
    public void AddPlayer_ThirdPlayer_ReturnsFull()
    {
        var session = CreateFullSession();
        session.AddPlayer("third-token", "Charlie").Should().Be(JoinResult.Full);
    }

    [Fact]
    public void AddPlayer_SameTokenAsPlayer1_ReturnsAlreadyJoined()
    {
        var session = CreateLobbySession();
        session.AddPlayer(P1Token, "Alice");
        session.AddPlayer(P1Token, "Alice").Should().Be(JoinResult.AlreadyJoined);
    }

    [Fact]
    public void AddPlayer_SameTokenAsPlayer2_ReturnsAlreadyJoined()
    {
        var session = CreateFullSession();
        session.AddPlayer(P2Token, "Bob").Should().Be(JoinResult.AlreadyJoined);
    }

    [Fact]
    public void IsFull_OnePlayer_ReturnsFalse()
    {
        var session = CreateLobbySession();
        session.AddPlayer(P1Token, "Alice");
        session.IsFull.Should().BeFalse();
    }

    [Fact]
    public void IsFull_TwoPlayers_ReturnsTrue()
    {
        CreateFullSession().IsFull.Should().BeTrue();
    }

    // ── IsActivePlayer ────────────────────────────────────────────────────

    [Fact]
    public void IsActivePlayer_MatchingActiveToken_ReturnsTrue()
    {
        // P1 always gets the first turn after mystery selection
        CreatePlayingSession().IsActivePlayer(P1Token).Should().BeTrue();
    }

    [Fact]
    public void IsActivePlayer_NonActiveToken_ReturnsFalse()
    {
        CreatePlayingSession().IsActivePlayer(P2Token).Should().BeFalse();
    }

    [Fact]
    public void IsActivePlayer_EmptyString_ReturnsFalse()
    {
        CreatePlayingSession().IsActivePlayer("").Should().BeFalse();
    }

    // ── SelectMysteryPeople ───────────────────────────────────────────────

    [Fact]
    public void SelectMysteryPeople_BothPlayersConfirm_AdvancesToPlaying()
    {
        CreatePlayingSession().Phase.Should().Be(GamePhase.Playing);
    }

    [Fact]
    public void SelectMysteryPeople_BothPlayersConfirm_Player1TakesFirstTurn()
    {
        CreatePlayingSession().ActivePlayerToken.Should().Be(P1Token);
    }

    [Fact]
    public void SelectMysteryPeople_BothPlayersConfirm_BoardOrdersContainAll24Ids()
    {
        var session = CreatePlayingSession();
        session.Player1!.BoardOrder.Should().HaveCount(24);
        session.Player2!.BoardOrder.Should().HaveCount(24);
    }

    [Fact]
    public void SelectMysteryPeople_SetsRoundNumberToOne()
    {
        CreatePlayingSession().RoundNumber.Should().Be(1);
    }

    [Fact]
    public void SelectMysteryPeople_IdenticalIds_IsNoOp()
    {
        var session = CreateFullSession();
        session.SelectMysteryPeople(P1Token, 5, 5);  // same ID twice — invalid

        session.Player1!.HasSelectedMysteryPeople.Should().BeFalse();
        session.Phase.Should().Be(GamePhase.CharacterSelection);
    }

    [Fact]
    public void SelectMysteryPeople_AlreadyConfirmed_IsNoOp()
    {
        var session = CreateFullSession();
        session.SelectMysteryPeople(P1Token, 1, 2);
        session.SelectMysteryPeople(P1Token, 5, 6);  // second attempt ignored

        session.Player1!.MysteryPersonIds.Should().Equal(1, 2);
    }

    [Fact]
    public void SelectMysteryPeople_WrongPhase_IsNoOp()
    {
        // Only one player joined → still in Lobby phase, not CharacterSelection
        var session = CreateLobbySession();
        session.AddPlayer(P1Token, "Alice");
        session.SelectMysteryPeople(P1Token, 1, 2);

        session.Player1!.HasSelectedMysteryPeople.Should().BeFalse();
    }

    // ── StartNextTurn ─────────────────────────────────────────────────────

    [Fact]
    public void StartNextTurn_ActivePlayer_PassesTurnToOpponent()
    {
        var session = CreatePlayingSession();
        session.StartNextTurn(P1Token);
        session.ActivePlayerToken.Should().Be(P2Token);
    }

    [Fact]
    public void StartNextTurn_CalledTwice_ReturnsTurnToOriginalPlayer()
    {
        var session = CreatePlayingSession();
        session.StartNextTurn(P1Token);
        session.StartNextTurn(P2Token);
        session.ActivePlayerToken.Should().Be(P1Token);
    }

    [Fact]
    public void StartNextTurn_InactivePlayer_IsNoOp()
    {
        var session = CreatePlayingSession();
        session.StartNextTurn(P2Token);  // P2 is not active
        session.ActivePlayerToken.Should().Be(P1Token);
    }

    [Fact]
    public void StartNextTurn_ResetsQuestionAsked()
    {
        var session = CreatePlayingSession();
        session.AskQuestion(P1Token, "Any glasses?");
        session.QuestionAsked.Should().BeTrue();

        session.StartNextTurn(P1Token);
        session.QuestionAsked.Should().BeFalse();
    }

    [Fact]
    public void StartNextTurn_CancelsRunningCountdown()
    {
        var session = CreatePlayingSession();
        session.AskQuestion(P1Token, "Any glasses?");
        session.AnswerQuestion(P2Token, "Yes");
        session.CountdownActive.Should().BeTrue();

        session.StartNextTurn(P1Token);
        session.CountdownActive.Should().BeFalse();
    }

    // ── AskQuestion ───────────────────────────────────────────────────────

    [Fact]
    public void AskQuestion_ActivePlayer_AppendsQuestionToLog()
    {
        var session = CreatePlayingSession();
        session.AskQuestion(P1Token, "Does your person have a hat?");

        session.ChatLog.Should().HaveCount(1);
        session.ChatLog[0].Kind.Should().Be(ChatMessageKind.Question);
        session.ChatLog[0].Text.Should().Be("Does your person have a hat?");
        session.ChatLog[0].SenderName.Should().Be("Alice");
    }

    [Fact]
    public void AskQuestion_SetsQuestionAsked()
    {
        var session = CreatePlayingSession();
        session.AskQuestion(P1Token, "Any hat?");
        session.QuestionAsked.Should().BeTrue();
    }

    [Fact]
    public void AskQuestion_InactivePlayer_IsNoOp()
    {
        var session = CreatePlayingSession();
        session.AskQuestion(P2Token, "Question from inactive player");

        session.ChatLog.Should().BeEmpty();
        session.QuestionAsked.Should().BeFalse();
    }

    [Fact]
    public void AskQuestion_SecondQuestionSameTurn_IsNoOp()
    {
        var session = CreatePlayingSession();
        session.AskQuestion(P1Token, "First question?");
        session.AskQuestion(P1Token, "Second question?");  // locked after first

        session.ChatLog.Should().HaveCount(1);
    }

    [Fact]
    public void AskQuestion_EmptyOrWhitespaceText_IsNoOp()
    {
        var session = CreatePlayingSession();
        session.AskQuestion(P1Token, "   ");

        session.ChatLog.Should().BeEmpty();
        session.QuestionAsked.Should().BeFalse();
    }

    [Fact]
    public void AskQuestion_TrimsLeadingAndTrailingWhitespace()
    {
        var session = CreatePlayingSession();
        session.AskQuestion(P1Token, "  Any glasses?  ");

        session.ChatLog[0].Text.Should().Be("Any glasses?");
    }

    // ── AwaitingAnswer ────────────────────────────────────────────────────

    [Fact]
    public void AwaitingAnswer_BeforeAnyQuestion_IsFalse()
    {
        CreatePlayingSession().AwaitingAnswer.Should().BeFalse();
    }

    [Fact]
    public void AwaitingAnswer_AfterQuestionBeforeAnswer_IsTrue()
    {
        var session = CreatePlayingSession();
        session.AskQuestion(P1Token, "Any hat?");
        session.AwaitingAnswer.Should().BeTrue();
    }

    [Fact]
    public void AwaitingAnswer_AfterAnswer_IsFalse()
    {
        var session = CreatePlayingSession();
        session.AskQuestion(P1Token, "Any hat?");
        session.AnswerQuestion(P2Token, "Both");
        session.AwaitingAnswer.Should().BeFalse();
    }

    // ── AnswerQuestion ────────────────────────────────────────────────────

    [Fact]
    public void AnswerQuestion_InactivePlayer_AppendsAnswerToLog()
    {
        var session = CreatePlayingSession();
        session.AskQuestion(P1Token, "Any hat?");
        session.AnswerQuestion(P2Token, "Neither");

        session.ChatLog.Should().HaveCount(2);
        session.ChatLog[1].Kind.Should().Be(ChatMessageKind.Answer);
        session.ChatLog[1].Text.Should().Be("Neither");
        session.ChatLog[1].SenderName.Should().Be("Bob");
    }

    [Fact]
    public void AnswerQuestion_ActivePlayer_IsNoOp()
    {
        var session = CreatePlayingSession();
        session.AskQuestion(P1Token, "Any hat?");
        session.AnswerQuestion(P1Token, "Yes");  // cannot answer own question

        session.ChatLog.Should().HaveCount(1);  // only the question
    }

    [Fact]
    public void AnswerQuestion_WithoutPendingQuestion_IsNoOp()
    {
        var session = CreatePlayingSession();
        session.AnswerQuestion(P2Token, "Yes");

        session.ChatLog.Should().BeEmpty();
    }

    [Fact]
    public void AnswerQuestion_StartsPostAnswerCountdown()
    {
        var session = CreatePlayingSession();
        session.AskQuestion(P1Token, "Any hat?");
        session.AnswerQuestion(P2Token, "One of them");

        session.CountdownActive.Should().BeTrue();
        session.CountdownStartedAt.Should().NotBeNull();
    }

    // ── EliminateCharacter ────────────────────────────────────────────────

    [Fact]
    public void EliminateCharacter_ActivePlayer_AddsCharacterToEliminatedSet()
    {
        var session = CreatePlayingSession();
        session.EliminateCharacter(P1Token, 7);
        session.Player1!.EliminatedIds.Should().Contain(7);
    }

    [Fact]
    public void EliminateCharacter_InactivePlayer_IsNoOp()
    {
        var session = CreatePlayingSession();
        session.EliminateCharacter(P2Token, 7);
        session.Player2!.EliminatedIds.Should().BeEmpty();
    }

    [Fact]
    public void EliminateCharacter_MysteryPersonId_IsNoOp()
    {
        // P1's mystery people (IDs 1 and 2) are immune to elimination
        var session = CreatePlayingSession();
        session.EliminateCharacter(P1Token, P1Mystery1);
        session.EliminateCharacter(P1Token, P1Mystery2);
        session.Player1!.EliminatedIds.Should().BeEmpty();
    }

    [Fact]
    public void EliminateCharacter_AlreadyEliminated_DoesNotDuplicate()
    {
        var session = CreatePlayingSession();
        session.EliminateCharacter(P1Token, 7);
        session.EliminateCharacter(P1Token, 7);  // second elimination of same character
        session.Player1!.EliminatedIds.Should().HaveCount(1);
    }

    [Fact]
    public void EliminateCharacter_WrongPhase_IsNoOp()
    {
        var session = CreateFullSession();  // CharacterSelection phase — not Playing
        session.EliminateCharacter(P1Token, 7);
        session.Player1!.EliminatedIds.Should().BeEmpty();
    }

    // ── MakeGuess ─────────────────────────────────────────────────────────

    [Fact]
    public void MakeGuess_CorrectGuess_RoundWinnerIsGuesser()
    {
        var session = CreatePlayingSession();
        session.MakeGuess(P1Token, P2Mystery1, P2Mystery2);
        session.RoundWinnerToken.Should().Be(P1Token);
    }

    [Fact]
    public void MakeGuess_CorrectGuess_IncrementsGuesserRoundWins()
    {
        var session = CreatePlayingSession();
        session.MakeGuess(P1Token, P2Mystery1, P2Mystery2);
        session.Player1!.RoundWins.Should().Be(1);
    }

    [Fact]
    public void MakeGuess_CorrectGuess_OrderIndependent()
    {
        // Guessing IDs in reverse order should still count as correct
        var session = CreatePlayingSession();
        session.MakeGuess(P1Token, P2Mystery2, P2Mystery1);
        session.RoundWinnerToken.Should().Be(P1Token);
    }

    [Fact]
    public void MakeGuess_CorrectGuess_SetsEndReasonToCorrectGuess()
    {
        var session = CreatePlayingSession();
        session.MakeGuess(P1Token, P2Mystery1, P2Mystery2);
        session.EndReason.Should().Be(RoundEndReason.CorrectGuess);
    }

    [Fact]
    public void MakeGuess_WrongGuess_OpponentWinsRound()
    {
        var session = CreatePlayingSession();
        session.MakeGuess(P1Token, 5, 6);  // IDs 5 and 6 are not P2's mystery people
        session.RoundWinnerToken.Should().Be(P2Token);
        session.Player2!.RoundWins.Should().Be(1);
    }

    [Fact]
    public void MakeGuess_WrongGuess_SetsEndReasonToWrongGuess()
    {
        var session = CreatePlayingSession();
        session.MakeGuess(P1Token, 5, 6);
        session.EndReason.Should().Be(RoundEndReason.WrongGuess);
    }

    [Fact]
    public void MakeGuess_AdvancesPhaseToRoundEnd()
    {
        var session = CreatePlayingSession();
        session.MakeGuess(P1Token, P2Mystery1, P2Mystery2);
        session.Phase.Should().Be(GamePhase.RoundEnd);
    }

    [Fact]
    public void MakeGuess_CancelsRunningCountdown()
    {
        var session = CreatePlayingSession();
        session.AskQuestion(P1Token, "Any hat?");
        session.AnswerQuestion(P2Token, "Yes");
        session.CountdownActive.Should().BeTrue();

        session.MakeGuess(P1Token, P2Mystery1, P2Mystery2);
        session.CountdownActive.Should().BeFalse();
    }

    [Fact]
    public void MakeGuess_IdenticalIds_IsNoOp()
    {
        // Must guess two distinct people — same ID twice is invalid
        var session = CreatePlayingSession();
        session.MakeGuess(P1Token, P2Mystery1, P2Mystery1);
        session.Phase.Should().Be(GamePhase.Playing);
    }

    [Fact]
    public void MakeGuess_InactivePlayer_IsNoOp()
    {
        var session = CreatePlayingSession();
        session.MakeGuess(P2Token, P1Mystery1, P1Mystery2);  // P2 is not active
        session.Phase.Should().Be(GamePhase.Playing);
    }

    [Fact]
    public void MakeGuess_CorrectGuessReachesWinThreshold_SetsIsMatchOver()
    {
        var session = CreatePlayingSession();
        session.Player1!.RoundWins = 4;  // one more win needed for the match

        session.MakeGuess(P1Token, P2Mystery1, P2Mystery2);  // correct → 5 wins

        session.IsMatchOver.Should().BeTrue();
        session.MatchWinnerToken.Should().Be(P1Token);
    }

    [Fact]
    public void MakeGuess_WrongGuessOpponentReachesThreshold_SetsIsMatchOver()
    {
        var session = CreatePlayingSession();
        session.Player2!.RoundWins = 4;

        session.MakeGuess(P1Token, 5, 6);  // wrong → P2 wins → 5 wins

        session.IsMatchOver.Should().BeTrue();
        session.MatchWinnerToken.Should().Be(P2Token);
    }

    [Fact]
    public void MakeGuess_BelowWinThreshold_IsMatchOverRemainsFalse()
    {
        var session = CreatePlayingSession();
        session.MakeGuess(P1Token, P2Mystery1, P2Mystery2);
        session.IsMatchOver.Should().BeFalse();
    }

    // ── MakePostRoundDecision ─────────────────────────────────────────────

    [Fact]
    public void MakePostRoundDecision_BothChooseNewRound_AdvancesToCharacterSelection()
    {
        var session = CreateRoundEndSession();
        session.MakePostRoundDecision(P1Token, PostRoundDecision.NewRound);
        session.MakePostRoundDecision(P2Token, PostRoundDecision.NewRound);
        session.Phase.Should().Be(GamePhase.CharacterSelection);
    }

    [Fact]
    public void MakePostRoundDecision_BothChooseNewRound_IncrementsRoundNumber()
    {
        var session = CreateRoundEndSession();
        var roundBefore = session.RoundNumber;

        session.MakePostRoundDecision(P1Token, PostRoundDecision.NewRound);
        session.MakePostRoundDecision(P2Token, PostRoundDecision.NewRound);

        session.RoundNumber.Should().Be(roundBefore + 1);
    }

    [Fact]
    public void MakePostRoundDecision_BothChooseNewRound_ClearsRoundState()
    {
        var session = CreateRoundEndSession();
        session.MakePostRoundDecision(P1Token, PostRoundDecision.NewRound);
        session.MakePostRoundDecision(P2Token, PostRoundDecision.NewRound);

        // Per-round state should be cleared
        session.ChatLog.Should().BeEmpty();
        session.ActivePlayerToken.Should().BeEmpty();
        session.Player1!.MysteryPersonIds.Should().BeEmpty();
        session.Player2!.MysteryPersonIds.Should().BeEmpty();
        session.Player1.EliminatedIds.Should().BeEmpty();
        session.Player2.EliminatedIds.Should().BeEmpty();
    }

    [Fact]
    public void MakePostRoundDecision_BothChooseEndGame_AdvancesToGameEnd()
    {
        var session = CreateRoundEndSession();
        session.MakePostRoundDecision(P1Token, PostRoundDecision.EndGame);
        session.MakePostRoundDecision(P2Token, PostRoundDecision.EndGame);
        session.Phase.Should().Be(GamePhase.GameEnd);
    }

    [Fact]
    public void MakePostRoundDecision_PlayAgain_ResetsBothPlayersRoundWins()
    {
        // Build a session where P1 just won the match (5 wins)
        var session = CreatePlayingSession();
        session.Player1!.RoundWins = 4;
        session.MakeGuess(P1Token, P2Mystery1, P2Mystery2);  // P1 → 5 wins → match over

        session.IsMatchOver.Should().BeTrue();

        // Both choose New Round (= "Play Again" when match is over)
        session.MakePostRoundDecision(P1Token, PostRoundDecision.NewRound);
        session.MakePostRoundDecision(P2Token, PostRoundDecision.NewRound);

        session.Player1.RoundWins.Should().Be(0);
        session.Player2!.RoundWins.Should().Be(0);
        session.Phase.Should().Be(GamePhase.CharacterSelection);
    }

    [Fact]
    public void MakePostRoundDecision_WrongPhase_IsNoOp()
    {
        var session = CreatePlayingSession();  // Playing — not RoundEnd
        session.MakePostRoundDecision(P1Token, PostRoundDecision.EndGame);
        session.Phase.Should().Be(GamePhase.Playing);
    }

    [Fact]
    public void MakePostRoundDecision_DisagreementPending_NeitherResolves()
    {
        var session = CreateRoundEndSession();
        session.MakePostRoundDecision(P1Token, PostRoundDecision.NewRound);
        session.MakePostRoundDecision(P2Token, PostRoundDecision.EndGame);

        // No consensus → stays in RoundEnd
        session.Phase.Should().Be(GamePhase.RoundEnd);
    }

    // ── GetPostRoundDecision ──────────────────────────────────────────────

    [Fact]
    public void GetPostRoundDecision_BeforeDeciding_ReturnsNull()
    {
        var session = CreateRoundEndSession();
        session.GetPostRoundDecision(P1Token).Should().BeNull();
    }

    [Fact]
    public void GetPostRoundDecision_AfterDeciding_ReturnsDecision()
    {
        var session = CreateRoundEndSession();
        session.MakePostRoundDecision(P1Token, PostRoundDecision.NewRound);
        session.GetPostRoundDecision(P1Token).Should().Be(PostRoundDecision.NewRound);
    }

    // ── LastActivityAt ────────────────────────────────────────────────────

    [Fact]
    public void LastActivityAt_IsUpdatedAfterStateChange()
    {
        var session = CreateLobbySession();
        var before = session.LastActivityAt;

        Thread.Sleep(5);
        session.AddPlayer(P1Token, "Alice");

        session.LastActivityAt.Should().BeOnOrAfter(before);
    }

    // ── GetPlayer / GetOpponent ───────────────────────────────────────────

    [Fact]
    public void GetPlayer_KnownToken_ReturnsCorrectPlayer()
    {
        var session = CreateFullSession();
        var player = session.GetPlayer(P1Token);
        player.Should().NotBeNull();
        player!.Name.Should().Be("Alice");
    }

    [Fact]
    public void GetPlayer_UnknownToken_ReturnsNull()
    {
        CreateFullSession().GetPlayer("unknown-token").Should().BeNull();
    }

    [Fact]
    public void GetOpponent_KnownToken_ReturnsOtherPlayer()
    {
        var session = CreateFullSession();
        session.GetOpponent(P1Token)!.Name.Should().Be("Bob");
        session.GetOpponent(P2Token)!.Name.Should().Be("Alice");
    }
}
