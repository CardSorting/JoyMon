using System.Text.Json.Serialization;

namespace JoyMon.Content;

/// <summary>
/// Raw JSON-deserialized model for a move definition.
/// </summary>
public class MoveContent
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("power")]
    public int Power { get; init; }

    [JsonPropertyName("accuracy")]
    public int Accuracy { get; init; }

    [JsonPropertyName("maxUses")]
    public int MaxUses { get; init; }

    [JsonPropertyName("effect")]
    public string? Effect { get; init; }

    [JsonPropertyName("effectChance")]
    public int EffectChance { get; init; }
}