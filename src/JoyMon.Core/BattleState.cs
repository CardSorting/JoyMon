namespace JoyMon.Core;

/// <summary>
/// Mutable snapshot of an in-progress battle. Mutated by <see cref="BattleSystem.ExecuteTurn"/>.
/// </summary>
public class BattleState
{
    public JoyMonInstance PlayerJoyMon { get; }
    public JoyMonInstance OpponentJoyMon { get; }
    public bool IsOver { get; set; }
    public bool PlayerWon { get; set; }
    public List<BattleEvent> Events { get; }

    public BattleState(JoyMonInstance player, JoyMonInstance opponent)
    {
        PlayerJoyMon = player;
        OpponentJoyMon = opponent;
        IsOver = false;
        PlayerWon = false;
        Events = new List<BattleEvent>();
    }

    /// <summary>Convenience: true when the battle is still ongoing.</summary>
    public bool IsActive => !IsOver;
}