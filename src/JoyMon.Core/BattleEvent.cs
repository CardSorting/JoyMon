namespace JoyMon.Core;

/// <summary>
/// Immutable event recorded during battle resolution.
/// The full event log provides a complete audit trail of each turn.
/// </summary>
public abstract record BattleEvent
{
    /// <summary>A JoyMon used a move (logged before accuracy check).</summary>
    public sealed record MoveUsed(string SpeciesName, string MoveName) : BattleEvent;

    /// <summary>A move missed due to accuracy failure.</summary>
    public sealed record MoveMissed(string SpeciesName, string MoveName) : BattleEvent;

    /// <summary>Damage was dealt from one monster to another.</summary>
    public sealed record DamageDealt(string SourceName, string TargetName, int Damage) : BattleEvent;

    /// <summary>A JoyMon fainted (HP reached 0).</summary>
    public sealed record JoyMonFainted(string SpeciesName) : BattleEvent;

    /// <summary>The player won the battle.</summary>
    public sealed record BattleWon : BattleEvent;

    /// <summary>The player lost the battle (or fled).</summary>
    public sealed record BattleLost : BattleEvent;

    /// <summary>XP was awarded to the player's JoyMon.</summary>
    public sealed record XpGained(int Amount, int NewTotal) : BattleEvent;

    /// <summary>The player's JoyMon gained a level.</summary>
    public sealed record LevelUp(string SpeciesName, int NewLevel) : BattleEvent;

    /// <summary>A status effect was applied to a JoyMon.</summary>
    public sealed record StatusApplied(string SpeciesName, string StatusName) : BattleEvent;

    /// <summary>A status effect wore off.</summary>
    public sealed record StatusExpired(string SpeciesName, string StatusName) : BattleEvent;

    /// <summary>Residual damage from a status (e.g. Burn).</summary>
    public sealed record StatusDamage(string SpeciesName, string StatusName, int Damage) : BattleEvent;
}