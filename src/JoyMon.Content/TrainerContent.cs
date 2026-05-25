using System.Text.Json.Serialization;

namespace JoyMon.Content;

public class TrainerContent
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    [JsonPropertyName("mapId")]
    public string MapId { get; init; } = string.Empty;

    [JsonPropertyName("tilePosition")]
    public TrainerTilePositionContent TilePosition { get; init; } = new();

    [JsonPropertyName("sightRange")]
    public int SightRange { get; init; }

    [JsonPropertyName("facingDirection")]
    public string FacingDirection { get; init; } = "down";

    [JsonPropertyName("spriteId")]
    public string SpriteId { get; init; } = "rival";

    [JsonPropertyName("dialogueBefore")]
    public TrainerDialogueContent DialogueBefore { get; init; } = new();

    [JsonPropertyName("dialogueAfter")]
    public TrainerDialogueContent DialogueAfter { get; init; } = new();

    [JsonPropertyName("party")]
    public List<TrainerPartyMemberContent> Party { get; init; } = new();
}

public class TrainerFileContent
{
    [JsonPropertyName("trainers")]
    public List<TrainerContent> Trainers { get; init; } = new();
}

public class TrainerTilePositionContent
{
    [JsonPropertyName("x")]
    public int X { get; init; }

    [JsonPropertyName("y")]
    public int Y { get; init; }
}

public class TrainerDialogueContent
{
    [JsonPropertyName("speaker")]
    public string Speaker { get; init; } = string.Empty;

    [JsonPropertyName("lines")]
    public List<string> Lines { get; init; } = new();
}

public class TrainerPartyMemberContent
{
    [JsonPropertyName("creatureId")]
    public string CreatureId { get; init; } = string.Empty;

    [JsonPropertyName("level")]
    public int Level { get; init; }

    [JsonPropertyName("moves")]
    public List<string>? Moves { get; init; }
}
