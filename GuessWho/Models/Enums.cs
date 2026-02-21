namespace GuessWho.Models;

public enum HairColor { Black, Brown, Blonde, Red, White }

public enum EyeColor { Blue, Brown }

public enum HairLength { Short, Long }

public enum GamePhase { Lobby, CharacterSelection, Playing, RoundEnd, GameEnd }

public enum JoinResult { Success, NotFound, Full, AlreadyJoined }

public enum ChatMessageKind { Question, Answer, System }

public enum RoundEndReason { CorrectGuess, WrongGuess }
