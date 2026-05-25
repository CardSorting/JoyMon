namespace JoyMon.Core;

/// <summary>
/// Persistent flag keys for Snowbell Shrine dungeon state.
/// </summary>
public static class ShrineFlags
{
    public const string BellNorth = "snowbell_bell_north";
    public const string BellWest = "snowbell_bell_west";
    public const string BellEast = "snowbell_bell_east";
    public const string BellPatternSolved = "snowbell_bell_pattern_solved";
    public const string Cleared = "snowbell_shrine_cleared";

    public static readonly string[] AllBellFlags = { BellNorth, BellWest, BellEast };
}
