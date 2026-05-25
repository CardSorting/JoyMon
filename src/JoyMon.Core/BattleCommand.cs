namespace JoyMon.Core;

/// <summary>
/// A command issued by a trainer during battle.
/// </summary>
public abstract record BattleCommand
{
    /// <summary>
    /// Use the move at the given index in the JoyMon's move list.
    /// </summary>
    public sealed record Fight(int MoveIndex) : BattleCommand;

    /// <summary>
    /// Attempt to flee the battle. Always succeeds in the current implementation.
    /// </summary>
    public sealed record Run : BattleCommand;
}