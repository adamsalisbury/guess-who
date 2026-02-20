namespace GuessWho.Models;

/// <summary>
/// Represents a Guess Who character with all visible attributes.
/// Immutable — character definitions never change at runtime.
/// </summary>
public sealed record Character(
    int Id,
    string Name,
    HairColor HairColor,
    EyeColor EyeColor,
    bool Glasses,
    bool Hat,
    bool FacialHair,
    HairLength HairLength,
    bool Bald,
    bool RosyCheeks,
    bool BigNose
);
