namespace JoyMon.Core;

/// <summary>Identifiers for battle status effects.</summary>
public static class BattleStatus
{
    public const string Burn = "Burn";
    public const string Guard = "Guard";

    public const int BurnDurationTurns = 3;
    public const int BurnDamagePerTick = 2;
    public const int DefaultBurnChancePercent = 30;
}

/// <summary>Move effect tags loaded from content.</summary>
public static class MoveEffects
{
    public const string Guard = "guard";
    public const string Burn = "burn";
}
