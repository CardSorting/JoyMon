using System.Text.Json.Serialization;

namespace JoyMon.Content;

/// <summary>
/// Raw JSON-deserialized model for a creature definition.
/// </summary>
public class CreatureContent
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("baseStats")]
    public BaseStatsContent BaseStats { get; init; } = new();

    [JsonPropertyName("starterEligible")]
    public bool StarterEligible { get; init; }

    [JsonPropertyName("learnset")]
    public List<string> Learnset { get; init; } = new();
}

/// <summary>
/// Raw JSON-deserialized model for creature base stats.
/// </summary>
public class BaseStatsContent
{
    [JsonPropertyName("maxHp")]
    public int MaxHp { get; init; }

    [JsonPropertyName("attack")]
    public int Attack { get; init; }

    [JsonPropertyName("defense")]
    public int Defense { get; init; }

    [JsonPropertyName("speed")]
    public int Speed { get; init; }
}