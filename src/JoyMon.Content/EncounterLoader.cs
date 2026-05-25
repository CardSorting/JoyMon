using System.Text.Json;

namespace JoyMon.Content;

public class EncounterLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly string _encountersDirectory;

    public EncounterLoader(string encountersDirectory)
    {
        _encountersDirectory = encountersDirectory;
    }

    public EncounterTableContent Load(string filename, IReadOnlySet<string> validCreatureIds)
    {
        var validation = new ContentValidationResult();
        var path = Path.Combine(_encountersDirectory, filename);

        EncounterTableContent? table;
        try
        {
            var json = File.ReadAllText(path);
            table = JsonSerializer.Deserialize<EncounterTableContent>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidContentException($"Invalid JSON in encounter file '{filename}': {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            throw new InvalidContentException($"Could not read encounter file '{filename}': {ex.Message}", ex);
        }

        if (table is null)
            throw new InvalidContentException($"Encounter file '{filename}' deserialized to null.");

        Validate(table, filename, validCreatureIds, validation);
        validation.ThrowIfInvalid();

        return table;
    }

    public static void Validate(EncounterTableContent table, string filename, IReadOnlySet<string> validCreatureIds, ContentValidationResult validation)
    {
        if (string.IsNullOrWhiteSpace(table.Id))
            validation.AddError($"Encounter table '{filename}' missing 'id'");
        if (string.IsNullOrWhiteSpace(table.MapId))
            validation.AddError($"Encounter table '{filename}' missing 'mapId'");
        if (string.IsNullOrWhiteSpace(table.ZoneId))
            validation.AddError($"Encounter table '{filename}' missing 'zoneId'");
        if (table.TileIds == null || table.TileIds.Count == 0)
            validation.AddError($"Encounter table '{filename}' missing or empty 'tileIds'");
        if (table.EncounterRate < 0.0 || table.EncounterRate > 1.0)
            validation.AddError($"Encounter table '{filename}' has invalid encounterRate ({table.EncounterRate}); must be 0.0 to 1.0");

        if (table.Entries == null || table.Entries.Count == 0)
        {
            validation.AddError($"Encounter table '{filename}' has no entries");
        }
        else
        {
            foreach (var entry in table.Entries)
            {
                if (string.IsNullOrWhiteSpace(entry.CreatureId))
                    validation.AddError($"Encounter table '{filename}' entry missing 'creatureId'");
                else if (!validCreatureIds.Contains(entry.CreatureId))
                    validation.AddError($"Encounter table '{filename}' entry references unknown creature ID '{entry.CreatureId}'");

                if (entry.MinLevel <= 0)
                    validation.AddError($"Encounter table '{filename}' entry '{entry.CreatureId}' has invalid minLevel ({entry.MinLevel}); must be > 0");
                if (entry.MaxLevel < entry.MinLevel)
                    validation.AddError($"Encounter table '{filename}' entry '{entry.CreatureId}' has maxLevel ({entry.MaxLevel}) less than minLevel ({entry.MinLevel})");
                if (entry.Weight <= 0)
                    validation.AddError($"Encounter table '{filename}' entry '{entry.CreatureId}' has invalid weight ({entry.Weight}); must be > 0");
            }
        }
    }
}
