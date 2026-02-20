using GuessWho.Models;

namespace GuessWho.Data;

/// <summary>
/// Defines the canonical set of 24 Guess Who characters.
/// Attributes are designed for good deductive variety — roughly balanced distributions
/// across hair colour, glasses, hat, facial hair, hair length, bald, rosy cheeks, big nose.
/// </summary>
public static class CharacterData
{
    public static IReadOnlyList<Character> All { get; } = new List<Character>
    {
        //          Id  Name       Hair            Eyes           Glass  Hat    FacHair  Length        Bald   Rosy   BigN
        new(  1, "Alex",    HairColor.Brown,  EyeColor.Blue,   false, false, false, HairLength.Short, false, true,  false),
        new(  2, "Bernard", HairColor.Black,  EyeColor.Brown,  false, true,  true,  HairLength.Short, false, false, true ),
        new(  3, "Claire",  HairColor.Blonde, EyeColor.Blue,   false, false, false, HairLength.Long,  false, false, false),
        new(  4, "David",   HairColor.White,  EyeColor.Brown,  true,  false, true,  HairLength.Short, false, false, false),
        new(  5, "Emma",    HairColor.Red,    EyeColor.Blue,   false, true,  false, HairLength.Long,  false, true,  false),
        new(  6, "Felix",   HairColor.Brown,  EyeColor.Brown,  true,  false, true,  HairLength.Short, false, false, true ),
        new(  7, "Grace",   HairColor.Blonde, EyeColor.Blue,   false, false, false, HairLength.Long,  false, true,  false),
        new(  8, "Henry",   HairColor.Black,  EyeColor.Brown,  false, false, true,  HairLength.Short, false, false, false),
        new(  9, "Iris",    HairColor.Red,    EyeColor.Brown,  false, false, false, HairLength.Long,  false, false, false),
        new( 10, "Jake",    HairColor.Brown,  EyeColor.Blue,   true,  true,  false, HairLength.Short, false, false, false),
        new( 11, "Kate",    HairColor.Blonde, EyeColor.Blue,   false, true,  false, HairLength.Long,  false, true,  false),
        new( 12, "Leo",     HairColor.Black,  EyeColor.Brown,  false, false, true,  HairLength.Short, false, false, true ),
        new( 13, "Maria",   HairColor.Brown,  EyeColor.Brown,  false, false, false, HairLength.Long,  false, false, false),
        new( 14, "Nick",    HairColor.White,  EyeColor.Blue,   true,  false, true,  HairLength.Short, false, false, true ),
        new( 15, "Olivia",  HairColor.Blonde, EyeColor.Brown,  false, true,  false, HairLength.Long,  false, true,  false),
        new( 16, "Peter",   HairColor.Red,    EyeColor.Brown,  true,  false, true,  HairLength.Short, false, false, false),
        new( 17, "Quinn",   HairColor.Black,  EyeColor.Blue,   false, false, false, HairLength.Short, false, false, false),
        new( 18, "Rachel",  HairColor.Brown,  EyeColor.Blue,   false, false, false, HairLength.Long,  false, true,  true ),
        new( 19, "Sam",     HairColor.White,  EyeColor.Brown,  false, true,  true,  HairLength.Short, true,  false, false),
        new( 20, "Tara",    HairColor.Blonde, EyeColor.Blue,   true,  false, false, HairLength.Long,  false, false, false),
        new( 21, "Uma",     HairColor.Red,    EyeColor.Brown,  false, false, false, HairLength.Long,  false, true,  false),
        new( 22, "Victor",  HairColor.Black,  EyeColor.Brown,  true,  true,  true,  HairLength.Short, false, false, true ),
        new( 23, "Wendy",   HairColor.Brown,  EyeColor.Blue,   false, true,  false, HairLength.Long,  false, false, false),
        new( 24, "Zack",    HairColor.White,  EyeColor.Brown,  false, false, true,  HairLength.Short, true,  false, true ),
    };

    /// <summary>Returns a character by its ID, or null if not found.</summary>
    public static Character? GetById(int id) =>
        All.FirstOrDefault(c => c.Id == id);
}
