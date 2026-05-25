namespace JoyMon.Content;

/// <summary>
/// Resolves per-tile movement effects from map layer data.
/// </summary>
public static class MapMovementService
{
    public static string GetEffect(MapContent map, int x, int y)
    {
        var layer = map.Layers.MovementEffect;
        if (layer is null || y < 0 || y >= layer.Count)
            return MovementEffect.Normal;

        var row = layer[y];
        if (row is null || x < 0 || x >= row.Count)
            return MovementEffect.Normal;

        var value = row[x];
        return MovementEffect.IsValid(value) ? value : MovementEffect.Normal;
    }

    public static bool IsIce(MapContent map, int x, int y) =>
        MovementEffect.IsIce(GetEffect(map, x, y));
}
