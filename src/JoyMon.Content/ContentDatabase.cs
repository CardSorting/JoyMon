using JoyMon.Core;

namespace JoyMon.Content;

/// <summary>
/// Holds the fully loaded and validated content, including converted Core domain types.
/// </summary>
public class ContentDatabase
{
    /// <summary>Raw creature content models, keyed by creature ID.</summary>
    public IReadOnlyDictionary<string, CreatureContent> Creatures { get; }

    /// <summary>Raw move content models, keyed by move ID.</summary>
    public IReadOnlyDictionary<string, MoveContent> Moves { get; }

    /// <summary>Converted Core domain species, keyed by creature ID.</summary>
    public IReadOnlyDictionary<string, JoyMonSpecies> Species { get; }

    /// <summary>Converted Core domain move definitions, keyed by move ID.</summary>
    public IReadOnlyDictionary<string, MoveDefinition> MoveDefinitions { get; }

    public ContentDatabase(
        IReadOnlyDictionary<string, CreatureContent> creatures,
        IReadOnlyDictionary<string, MoveContent> moves,
        IReadOnlyDictionary<string, JoyMonSpecies> species,
        IReadOnlyDictionary<string, MoveDefinition> moveDefinitions)
    {
        Creatures = creatures;
        Moves = moves;
        Species = species;
        MoveDefinitions = moveDefinitions;
    }
}