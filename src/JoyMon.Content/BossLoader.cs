using System.Text.Json;

namespace JoyMon.Content;

public class BossLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly string _bossesDirectory;

    public BossLoader(string bossesDirectory)
    {
        _bossesDirectory = bossesDirectory;
    }

    public BossContent Load(string filename, IReadOnlySet<string> validCreatureIds)
    {
        var validation = new ContentValidationResult();
        var path = Path.Combine(_bossesDirectory, filename);

        BossContent? boss;
        try
        {
            var json = File.ReadAllText(path);
            boss = JsonSerializer.Deserialize<BossContent>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidContentException($"Invalid JSON in boss file '{filename}': {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            throw new InvalidContentException($"Could not read boss file '{filename}': {ex.Message}", ex);
        }

        if (boss is null)
            throw new InvalidContentException($"Boss file '{filename}' deserialized to null.");

        Validate(boss, filename, validCreatureIds, validation);
        validation.ThrowIfInvalid();

        return boss;
    }

    public static void Validate(
        BossContent boss,
        string filename,
        IReadOnlySet<string> validCreatureIds,
        ContentValidationResult validation)
    {
        if (string.IsNullOrWhiteSpace(boss.Id))
            validation.AddError($"Boss '{filename}' missing 'id'");
        if (string.IsNullOrWhiteSpace(boss.DisplayName))
            validation.AddError($"Boss '{boss.Id}' missing 'displayName'");
        if (string.IsNullOrWhiteSpace(boss.MapId))
            validation.AddError($"Boss '{boss.Id}' missing 'mapId'");
        if (boss.GateTile is null)
            validation.AddError($"Boss '{boss.Id}' missing 'gateTile'");
        if (boss.Level <= 0)
            validation.AddError($"Boss '{boss.Id}' has invalid level ({boss.Level}); must be > 0");
        if (string.IsNullOrWhiteSpace(boss.CreatureId))
            validation.AddError($"Boss '{boss.Id}' missing 'creatureId'");
        else if (!validCreatureIds.Contains(boss.CreatureId))
            validation.AddError($"Boss '{boss.Id}' references unknown creature ID '{boss.CreatureId}'");
        if (string.IsNullOrWhiteSpace(boss.ClearedFlag))
            validation.AddError($"Boss '{boss.Id}' missing 'clearedFlag'");

        if (boss.IntroDialogue is null)
        {
            validation.AddError($"Boss '{boss.Id}' missing 'introDialogue'");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(boss.IntroDialogue.Speaker))
                validation.AddError($"Boss '{boss.Id}' introDialogue missing 'speaker'");
            if (boss.IntroDialogue.Lines is null || boss.IntroDialogue.Lines.Count == 0)
                validation.AddError($"Boss '{boss.Id}' introDialogue has empty or missing 'lines'");
        }
    }
}
