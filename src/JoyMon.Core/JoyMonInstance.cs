namespace JoyMon.Core;

/// <summary>
/// An individual JoyMon in a battle — tracks mutable runtime state (HP, XP, remaining PP).
/// Belongs entirely in Domain: no I/O, no rendering, no framework dependency.
/// </summary>
public class JoyMonInstance
{
    public JoyMonSpecies Species { get; }
    public int Level { get; private set; }
    public int CurrentHp { get; set; }
    public int MaxHp { get; private set; }
    public int Attack { get; private set; }
    public int Defense { get; private set; }
    public int Speed { get; private set; }
    public int Xp { get; set; }
    public int[] RemainingUses { get; }

    public JoyMonInstance(
        JoyMonSpecies species,
        int level,
        int currentHp,
        int maxHp,
        int attack,
        int defense,
        int speed,
        int xp,
        int[] remainingUses)
    {
        Species = species;
        Level = level;
        CurrentHp = currentHp;
        MaxHp = maxHp;
        Attack = attack;
        Defense = defense;
        Speed = speed;
        Xp = xp;
        RemainingUses = remainingUses;
    }

    /// <summary>
    /// Returns true when CurrentHp ≤ 0.
    /// </summary>
    public bool IsFainted => CurrentHp <= 0;

    /// <summary>
    /// XP required to reach the next level.
    /// </summary>
    public int NextLevelXpThreshold => Level * 10;

    /// <summary>
    /// Levels up: increments level, applies stat growth, resets HP to new max.
    /// </summary>
    public void LevelUp()
    {
        Level++;
        MaxHp += 3;
        Attack += 1;
        Defense += 1;
        Speed += 1;
        CurrentHp = MaxHp;
    }

    /// <summary>
    /// Adds XP and triggers any pending level-ups. Returns number of levels gained.
    /// </summary>
    public int GrantXp(int amount)
    {
        Xp += amount;
        int levelsGained = 0;

        while (Xp >= Level * 10)
        {
            Xp -= Level * 10;
            LevelUp();
            levelsGained++;
        }

        return levelsGained;
    }
}