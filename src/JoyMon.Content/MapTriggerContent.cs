using System.Text.Json.Serialization;

namespace JoyMon.Content;

public class MapTriggerContent
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Kind { get; init; } = string.Empty;

    [JsonPropertyName("tile")]
    public TransitionTileContent Tile { get; init; } = new();

    [JsonPropertyName("setsFlag")]
    public string? SetsFlag { get; init; }

    [JsonPropertyName("requiredFlag")]
    public string? RequiredFlag { get; init; }

    [JsonPropertyName("bridgeTiles")]
    public List<TransitionTileContent> BridgeTiles { get; init; } = new();

    [JsonPropertyName("trackTiles")]
    public List<TransitionTileContent> TrackTiles { get; init; } = new();

    [JsonPropertyName("rockTiles")]
    public List<TransitionTileContent> RockTiles { get; init; } = new();

    [JsonPropertyName("doorTiles")]
    public List<TransitionTileContent> DoorTiles { get; init; } = new();

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("alreadyMessage")]
    public string? AlreadyMessage { get; init; }

    [JsonPropertyName("blockedMessage")]
    public string? BlockedMessage { get; init; }

    /// <summary>All flags that must be set before <see cref="PatternSolvedFlag"/> is raised (bell puzzles).</summary>
    [JsonPropertyName("patternFlags")]
    public List<string> PatternFlags { get; init; } = new();

    /// <summary>Flag set when every entry in <see cref="PatternFlags"/> is true.</summary>
    [JsonPropertyName("patternSolvedFlag")]
    public string? PatternSolvedFlag { get; init; }
}
