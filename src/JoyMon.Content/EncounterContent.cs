using System.Text.Json.Serialization;

namespace JoyMon.Content;

public class EncounterTableContent
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("mapId")]
    public string MapId { get; init; } = string.Empty;

    [JsonPropertyName("zoneId")]
    public string ZoneId { get; init; } = string.Empty;

    [JsonPropertyName("tileIds")]
    public List<int> TileIds { get; init; } = new();

    [JsonPropertyName("encounterRate")]
    public double EncounterRate { get; init; }

    [JsonPropertyName("entries")]
    public List<EncounterEntryContent> Entries { get; init; } = new();
}

public class EncounterEntryContent
{
    [JsonPropertyName("creatureId")]
    public string CreatureId { get; init; } = string.Empty;

    [JsonPropertyName("minLevel")]
    public int MinLevel { get; init; }

    [JsonPropertyName("maxLevel")]
    public int MaxLevel { get; init; }

    [JsonPropertyName("weight")]
    public int Weight { get; init; }
}
