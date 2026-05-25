using System.Text.Json.Serialization;

namespace JoyMon.Content;

public class BossContent
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    [JsonPropertyName("mapId")]
    public string MapId { get; init; } = string.Empty;

    [JsonPropertyName("gateTile")]
    public BossGateTileContent GateTile { get; init; } = new();

    [JsonPropertyName("level")]
    public int Level { get; init; }

    [JsonPropertyName("creatureId")]
    public string CreatureId { get; init; } = string.Empty;

    [JsonPropertyName("clearedFlag")]
    public string ClearedFlag { get; init; } = "trial_grove_cleared";

    [JsonPropertyName("introDialogue")]
    public TrainerDialogueContent IntroDialogue { get; init; } = new();
}

public class BossGateTileContent
{
    [JsonPropertyName("x")]
    public int X { get; init; }

    [JsonPropertyName("y")]
    public int Y { get; init; }
}
