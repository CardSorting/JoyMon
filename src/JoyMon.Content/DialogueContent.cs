using System.Text.Json.Serialization;

namespace JoyMon.Content;

public class DialogueFileContent
{
    [JsonPropertyName("npcs")]
    public List<NpcContent> Npcs { get; init; } = new();

    [JsonPropertyName("dialogues")]
    public List<DialogueContent> Dialogues { get; init; } = new();
}

public class NpcContent
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("mapId")]
    public string MapId { get; init; } = string.Empty;

    [JsonPropertyName("tilePosition")]
    public NpcTilePositionContent TilePosition { get; init; } = new();

    [JsonPropertyName("facingDirection")]
    public string FacingDirection { get; init; } = string.Empty;

    [JsonPropertyName("dialogueId")]
    public string DialogueId { get; init; } = string.Empty;

    [JsonPropertyName("spriteId")]
    public string SpriteId { get; init; } = string.Empty;
}

public class NpcTilePositionContent
{
    [JsonPropertyName("x")]
    public int X { get; init; }

    [JsonPropertyName("y")]
    public int Y { get; init; }
}

public class DialogueContent
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("speaker")]
    public string Speaker { get; init; } = string.Empty;

    [JsonPropertyName("lines")]
    public List<string> Lines { get; init; } = new();
}
