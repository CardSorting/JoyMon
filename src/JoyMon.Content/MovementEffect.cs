namespace JoyMon.Content;

/// <summary>
/// Tile movement behavior for overworld traversal.
/// </summary>
public static class MovementEffect
{
    public const string Normal = "normal";
    public const string Ice = "ice";
    public const string PollenWindNorth = "pollen_wind_north";
    public const string PollenWindSouth = "pollen_wind_south";
    public const string PollenWindEast = "pollen_wind_east";
    public const string PollenWindWest = "pollen_wind_west";

    public static bool IsValid(string? value) =>
        string.Equals(value, Normal, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, Ice, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, PollenWindNorth, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, PollenWindSouth, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, PollenWindEast, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, PollenWindWest, StringComparison.OrdinalIgnoreCase);

    public static bool IsIce(string? value) =>
        string.Equals(value, Ice, StringComparison.OrdinalIgnoreCase);

    public static bool IsPollenWind(string? value) =>
        string.Equals(value, PollenWindNorth, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, PollenWindSouth, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, PollenWindEast, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, PollenWindWest, StringComparison.OrdinalIgnoreCase);
}
