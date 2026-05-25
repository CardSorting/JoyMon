namespace JoyMon.Core;

/// <summary>
/// Static definitions for all JoyMon species and their available moves.
/// </summary>
public static class SpeciesLibrary
{
    // ── Moves ───────────────────────────────────────────────────

    private static readonly MoveDefinition VineLash = new(
        "vine_lash", "Vine Lash", JoyMonType.Moss, 40, 95, 15);

    private static readonly MoveDefinition SporeCloud = new(
        "spore_cloud", "Spore Cloud", JoyMonType.Moss, 20, 90, 20);

    private static readonly MoveDefinition ThunderJolt = new(
        "thunder_jolt", "Thunder Jolt", JoyMonType.Spark, 45, 90, 12);

    private static readonly MoveDefinition QuickSparks = new(
        "quick_sparks", "Quick Sparks", JoyMonType.Spark, 25, 95, 18);

    private static readonly MoveDefinition RockToss = new(
        "rock_toss", "Rock Toss", JoyMonType.Stone, 50, 85, 10);

    private static readonly MoveDefinition BoulderCrush = new(
        "boulder_crush", "Boulder Crush", JoyMonType.Stone, 30, 95, 15);

    private static readonly MoveDefinition WaterJet = new(
        "water_jet", "Water Jet", JoyMonType.Tide, 40, 95, 15);

    private static readonly MoveDefinition Splash = new(
        "splash", "Splash", JoyMonType.Tide, 20, 90, 20);

    private static readonly MoveDefinition FireBurst = new(
        "fire_burst", "Fire Burst", JoyMonType.Ember, 50, 85, 12);

    private static readonly MoveDefinition EmberGlow = new(
        "ember_glow", "Ember Glow", JoyMonType.Ember, 30, 95, 18);

    private static readonly MoveDefinition EchoWave = new(
        "echo_wave", "Echo Wave", JoyMonType.Echo, 45, 90, 12);

    private static readonly MoveDefinition SonicPulse = new(
        "sonic_pulse", "Sonic Pulse", JoyMonType.Echo, 25, 95, 20);

    // ── Species ─────────────────────────────────────────────────

    /// <summary>Grass-type. Balanced defenses, moderate speed.</summary>
    public static readonly JoyMonSpecies Moss = new(
        "Moss", JoyMonType.Moss, 45, 8, 8, 7,
        new[] { VineLash, SporeCloud });

    /// <summary>Electric-type. Fragile but very fast.</summary>
    public static readonly JoyMonSpecies Spark = new(
        "Spark", JoyMonType.Spark, 35, 6, 5, 12,
        new[] { ThunderJolt, QuickSparks });

    /// <summary>Rock-type. Slow and tanky.</summary>
    public static readonly JoyMonSpecies Stone = new(
        "Stone", JoyMonType.Stone, 55, 5, 10, 4,
        new[] { RockToss, BoulderCrush });

    /// <summary>Water-type. Well-rounded all-rounder.</summary>
    public static readonly JoyMonSpecies Tide = new(
        "Tide", JoyMonType.Tide, 45, 7, 7, 8,
        new[] { WaterJet, Splash });

    /// <summary>Fire-type. High attack, lower defenses.</summary>
    public static readonly JoyMonSpecies Ember = new(
        "Ember", JoyMonType.Ember, 40, 10, 5, 9,
        new[] { FireBurst, EmberGlow });

    /// <summary>Psychic-type. Fast, hits hard, glass cannon.</summary>
    public static readonly JoyMonSpecies Echo = new(
        "Echo", JoyMonType.Echo, 35, 9, 4, 11,
        new[] { EchoWave, SonicPulse });

    /// <summary>Convenience lookup by species name.</summary>
    public static JoyMonSpecies? Find(string name) =>
        All.FirstOrDefault(s => s.Name == name);

    public static IReadOnlyList<JoyMonSpecies> All { get; } = new[]
    {
        Moss, Spark, Stone, Tide, Ember, Echo
    };
}