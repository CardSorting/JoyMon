namespace JoyMon.Core;

/// <summary>
/// Defines a JoyMon species — its base stats, type, and available moves.
/// Create a battle-ready instance via <see cref="CreateInstance"/>.
/// </summary>
public class JoyMonSpecies
{
    public string Name { get; }
    public JoyMonType Type { get; }
    public string TypeDisplay { get; }
    public int BaseMaxHp { get; }
    public int BaseAttack { get; }
    public int BaseDefense { get; }
    public int BaseSpeed { get; }
    public IReadOnlyList<MoveDefinition> Moves { get; }

    public JoyMonSpecies(
        string name,
        JoyMonType type,
        int baseMaxHp,
        int baseAttack,
        int baseDefense,
        int baseSpeed,
        IReadOnlyList<MoveDefinition> moves,
        string? typeDisplay = null)
    {
        Name = name;
        Type = type;
        TypeDisplay = typeDisplay ?? type.ToString();
        BaseMaxHp = baseMaxHp;
        BaseAttack = baseAttack;
        BaseDefense = baseDefense;
        BaseSpeed = baseSpeed;
        Moves = moves;
    }

    /// <summary>
    /// Creates a <see cref="JoyMonInstance"/> at the given level with stats scaled from species base values.
    /// </summary>
    public JoyMonInstance CreateInstance(int level)
    {
        int hp = BaseMaxHp + level * 3;
        int atk = BaseAttack + level;
        int def = BaseDefense + level;
        int spd = BaseSpeed + level;

        return new JoyMonInstance(
            this,
            level,
            currentHp: hp,
            maxHp: hp,
            attack: atk,
            defense: def,
            speed: spd,
            xp: 0,
            Moves.Select(m => m.MaxUses).ToArray());
    }
}