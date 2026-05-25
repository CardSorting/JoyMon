using System.Text.Json;
using System.Text.Json.Serialization;

namespace JoyMon.Content;

/// <summary>
/// JSON converter that trims any extra whitespace/indent from layer arrays.
/// Not needed for System.Text.Json default parsing, but ensures robust
/// handling of the 2D grid format.
/// </summary>
internal class LayerConverter : JsonConverter<List<List<int>>>
{
    public override List<List<int>> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var result = new List<List<int>>();
        using var doc = JsonDocument.ParseValue(ref reader);
        foreach (var row in doc.RootElement.EnumerateArray())
        {
            var list = new List<int>();
            foreach (var val in row.EnumerateArray())
                list.Add(val.GetInt32());
            result.Add(list);
        }
        return result;
    }

    public override void Write(Utf8JsonWriter writer, List<List<int>> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}

/// <summary>
/// Loads and validates tilemap JSON files from the maps/ content directory.
/// </summary>
public class MapLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly string _mapsDirectory;

    public MapLoader(string mapsDirectory)
    {
        _mapsDirectory = mapsDirectory;
    }

    /// <summary>
    /// Loads a single map file by its filename. Returns the validated map or throws.
    /// </summary>
    public MapContent Load(string filename)
    {
        var validation = new ContentValidationResult();
        var path = Path.Combine(_mapsDirectory, filename);

        MapContent? map;
        try
        {
            var json = File.ReadAllText(path);
            map = JsonSerializer.Deserialize<MapContent>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidContentException($"Invalid JSON in map file '{filename}': {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            throw new InvalidContentException($"Could not read map file '{filename}': {ex.Message}", ex);
        }

        if (map is null)
            throw new InvalidContentException($"Map file '{filename}' deserialized to null.");

        Validate(map, filename, validation);

        if (map.Transitions is not null)
        {
            foreach (var trans in map.Transitions)
            {
                if (!string.IsNullOrWhiteSpace(trans.ToMapId))
                {
                    var targetPath = Path.Combine(_mapsDirectory, $"{trans.ToMapId}.json");
                    var allowsMissingTarget = !string.IsNullOrWhiteSpace(trans.OneWayReason);
                    if (!File.Exists(targetPath) && !allowsMissingTarget)
                    {
                        validation.AddError($"Map '{filename}' transitions to missing map '{trans.ToMapId}'");
                    }
                }
            }
        }

        ValidateTriggers(map, filename, validation);

        validation.ThrowIfInvalid();

        return map;
    }

    /// <summary>
    /// Validates a parsed map. Mutates <paramref name="validation"/> with errors.
    /// </summary>
    public static void Validate(MapContent map, string filename, ContentValidationResult validation)
    {
        // Required fields
        if (string.IsNullOrWhiteSpace(map.Id))
            validation.AddError($"Map '{filename}' missing 'id'");
        if (string.IsNullOrWhiteSpace(map.Name))
            validation.AddError($"Map '{filename}' missing 'name'");

        // Dimensions
        if (map.Width <= 0)
            validation.AddError($"Map '{filename}' has invalid width ({map.Width}); must be > 0");
        if (map.Height <= 0)
            validation.AddError($"Map '{filename}' has invalid height ({map.Height}); must be > 0");

        // Spawn point
        if (map.SpawnPoint is null)
            validation.AddError($"Map '{filename}' missing 'spawnPoint'");
        else
        {
            if (map.SpawnPoint.X < 0 || map.SpawnPoint.X >= map.Width)
                validation.AddError($"Map '{filename}' spawnPoint.x ({map.SpawnPoint.X}) out of bounds (0-{map.Width - 1})");
            if (map.SpawnPoint.Y < 0 || map.SpawnPoint.Y >= map.Height)
                validation.AddError($"Map '{filename}' spawnPoint.y ({map.SpawnPoint.Y}) out of bounds (0-{map.Height - 1})");
        }

        // Layer size validation
        if (map.Width > 0 && map.Height > 0)
        {
            ValidateLayer(map.Layers?.Ground, "ground", map.Width, map.Height, filename, validation);
            ValidateLayer(map.Layers?.Decoration, "decoration", map.Width, map.Height, filename, validation);
            ValidateLayer(map.Layers?.Collision, "collision", map.Width, map.Height, filename, validation);
            ValidateMovementEffectLayer(map.Layers?.MovementEffect, map.Width, map.Height, filename, validation);
        }

        // Collision values must be 0 or 1
        if (map.Layers?.Collision is not null)
        {
            for (int y = 0; y < map.Layers.Collision.Count && y < map.Height; y++)
            {
                var row = map.Layers.Collision[y];
                for (int x = 0; x < row.Count && x < map.Width; x++)
                {
                    if (row[x] != 0 && row[x] != 1)
                        validation.AddError($"Map '{filename}' collision layer at ({x},{y}) has invalid value {row[x]}; must be 0 or 1");
                }
            }
        }

        // Transition validation
        if (map.Transitions is not null)
        {
            foreach (var trans in map.Transitions)
            {
                if (string.IsNullOrWhiteSpace(trans.ToMapId))
                    validation.AddError($"Map '{filename}' has transition with missing 'toMapId'");
                if (string.IsNullOrWhiteSpace(trans.FromMapId))
                    validation.AddError($"Map '{filename}' has transition with missing 'fromMapId'");
                
                if (map.Width > 0 && map.Height > 0)
                {
                    if (trans.FromTile.X < 0 || trans.FromTile.X >= map.Width || trans.FromTile.Y < 0 || trans.FromTile.Y >= map.Height)
                        validation.AddError($"Map '{filename}' transition fromTile ({trans.FromTile.X},{trans.FromTile.Y}) out of bounds");
                }

                if (trans.ToTile.X < 0 || trans.ToTile.Y < 0)
                    validation.AddError($"Map '{filename}' transition toTile ({trans.ToTile.X},{trans.ToTile.Y}) has invalid negative coordinates");
            }
        }
    }

    private static void ValidateTriggers(MapContent map, string filename, ContentValidationResult validation)
    {
        if (map.Triggers is null) return;

        foreach (var trigger in map.Triggers)
        {
            if (string.IsNullOrWhiteSpace(trigger.Id))
                validation.AddError($"Map '{filename}' has trigger missing 'id'");
            if (string.IsNullOrWhiteSpace(trigger.Kind))
                validation.AddError($"Map '{filename}' trigger '{trigger.Id}' missing 'kind'");

            if (map.Width > 0 && map.Height > 0)
            {
                if (trigger.Tile.X < 0 || trigger.Tile.X >= map.Width || trigger.Tile.Y < 0 || trigger.Tile.Y >= map.Height)
                    validation.AddError($"Map '{filename}' trigger '{trigger.Id}' tile out of bounds");
            }

            foreach (var tile in trigger.BridgeTiles.Concat(trigger.TrackTiles).Concat(trigger.RockTiles).Concat(trigger.DoorTiles))
            {
                if (map.Width > 0 && map.Height > 0
                    && (tile.X < 0 || tile.X >= map.Width || tile.Y < 0 || tile.Y >= map.Height))
                {
                    validation.AddError($"Map '{filename}' trigger '{trigger.Id}' references out-of-bounds tile ({tile.X},{tile.Y})");
                }
            }
        }
    }

    private static void ValidateMovementEffectLayer(
        List<List<string>>? layer,
        int w,
        int h,
        string filename,
        ContentValidationResult validation)
    {
        if (layer is null)
            return;

        if (layer.Count != h)
        {
            validation.AddError($"Map '{filename}' layer 'movementEffect' has {layer.Count} rows but height is {h}");
            return;
        }

        for (int y = 0; y < layer.Count; y++)
        {
            var row = layer[y];
            if (row is null)
            {
                validation.AddError($"Map '{filename}' layer 'movementEffect' row {y} is null");
                continue;
            }

            if (row.Count != w)
                validation.AddError($"Map '{filename}' layer 'movementEffect' row {y} has {row.Count} columns but width is {w}");

            for (int x = 0; x < row.Count && x < w; x++)
            {
                if (!MovementEffect.IsValid(row[x]))
                {
                    validation.AddError(
                        $"Map '{filename}' movementEffect at ({x},{y}) has invalid value '{row[x]}'; must be 'normal', 'ice', or 'pollen_wind_<direction>'");
                }
            }
        }
    }

    private static void ValidateLayer(List<List<int>>? layer, string name, int w, int h, string filename, ContentValidationResult validation)
    {
        if (layer is null)
        {
            validation.AddError($"Map '{filename}' missing layer '{name}'");
            return;
        }

        if (layer.Count != h)
        {
            validation.AddError($"Map '{filename}' layer '{name}' has {layer.Count} rows but height is {h}");
            return;
        }

        for (int y = 0; y < layer.Count; y++)
        {
            if (layer[y] is null)
            {
                validation.AddError($"Map '{filename}' layer '{name}' row {y} is null");
                continue;
            }

            if (layer[y].Count != w)
                validation.AddError($"Map '{filename}' layer '{name}' row {y} has {layer[y].Count} columns but width is {w}");
        }
    }
}