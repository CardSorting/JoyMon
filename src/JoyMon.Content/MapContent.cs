using System.Text.Json.Serialization;

namespace JoyMon.Content;

/// <summary>
/// Raw JSON-deserialized model for a tilemap definition.
/// </summary>
public class MapContent
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("width")]
    public int Width { get; init; }

    [JsonPropertyName("height")]
    public int Height { get; init; }

    [JsonPropertyName("tileSize")]
    public int TileSize { get; init; } = 16;

    [JsonPropertyName("tilesetId")]
    public string TilesetId { get; init; } = string.Empty;

    [JsonPropertyName("spawnPoint")]
    public SpawnPointContent? SpawnPoint { get; init; } = null;

    [JsonPropertyName("layers")]
    public MapLayersContent Layers { get; init; } = new();

    [JsonPropertyName("transitions")]
    public List<MapTransitionContent> Transitions { get; init; } = new();
}

public class MapTransitionContent
{
    [JsonPropertyName("fromMapId")]
    public string FromMapId { get; init; } = string.Empty;

    [JsonPropertyName("fromTile")]
    public TransitionTileContent FromTile { get; init; } = new();

    [JsonPropertyName("toMapId")]
    public string ToMapId { get; init; } = string.Empty;

    [JsonPropertyName("toTile")]
    public TransitionTileContent ToTile { get; init; } = new();

    [JsonPropertyName("requiredFlag")]
    public string? RequiredFlag { get; init; }
}

public class TransitionTileContent
{
    [JsonPropertyName("x")]
    public int X { get; init; }

    [JsonPropertyName("y")]
    public int Y { get; init; }
}

/// <summary>
/// Player spawn position within a map.
/// </summary>
public class SpawnPointContent
{
    [JsonPropertyName("x")]
    public int X { get; init; }

    [JsonPropertyName("y")]
    public int Y { get; init; }
}

/// <summary>
/// Three tile layers that compose a map.
/// </summary>
public class MapLayersContent
{
    /// <summary>Base terrain tiles (grass, path, water).</summary>
    [JsonPropertyName("ground")]
    public List<List<int>> Ground { get; init; } = new();

    /// <summary>Overlay tiles (buildings, trees, signs).</summary>
    [JsonPropertyName("decoration")]
    public List<List<int>> Decoration { get; init; } = new();

    /// <summary>Walkability mask (0 = passable, 1 = blocked).</summary>
    [JsonPropertyName("collision")]
    public List<List<int>> Collision { get; init; } = new();
}