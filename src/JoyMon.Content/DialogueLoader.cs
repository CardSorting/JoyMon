using System.Text.Json;

namespace JoyMon.Content;

/// <summary>
/// Loads and validates dialogue and NPC definitions.
/// </summary>
public class DialogueLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly string _dialogueDirectory;

    public DialogueLoader(string dialogueDirectory)
    {
        _dialogueDirectory = dialogueDirectory;
    }

    public DialogueFileContent Load(string filename)
    {
        var validation = new ContentValidationResult();
        var path = Path.Combine(_dialogueDirectory, filename);

        DialogueFileContent? content;
        try
        {
            var json = File.ReadAllText(path);
            content = JsonSerializer.Deserialize<DialogueFileContent>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidContentException($"Invalid JSON in dialogue file '{filename}': {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            throw new InvalidContentException($"Could not read dialogue file '{filename}': {ex.Message}", ex);
        }

        if (content is null)
            throw new InvalidContentException($"Dialogue file '{filename}' deserialized to null.");

        Validate(content, filename, validation);
        validation.ThrowIfInvalid();

        return content;
    }

    public static void Validate(DialogueFileContent content, string filename, ContentValidationResult validation)
    {
        var npcIds = new HashSet<string>();
        foreach (var npc in content.Npcs)
        {
            if (string.IsNullOrWhiteSpace(npc.Id))
                validation.AddError($"NPC in '{filename}' missing 'id'");
            else if (!npcIds.Add(npc.Id))
                validation.AddError($"Duplicate NPC ID: '{npc.Id}' in '{filename}'");

            if (string.IsNullOrWhiteSpace(npc.Name))
                validation.AddError($"NPC '{npc.Id}' missing 'name'");
            if (string.IsNullOrWhiteSpace(npc.MapId))
                validation.AddError($"NPC '{npc.Id}' missing 'mapId'");
            if (npc.TilePosition is null)
                validation.AddError($"NPC '{npc.Id}' missing 'tilePosition'");
            if (string.IsNullOrWhiteSpace(npc.FacingDirection))
                validation.AddError($"NPC '{npc.Id}' missing 'facingDirection'");
            if (string.IsNullOrWhiteSpace(npc.DialogueId))
                validation.AddError($"NPC '{npc.Id}' missing 'dialogueId'");
        }

        var dialogueIds = new HashSet<string>();
        foreach (var dlg in content.Dialogues)
        {
            if (string.IsNullOrWhiteSpace(dlg.Id))
                validation.AddError($"Dialogue in '{filename}' missing 'id'");
            else if (!dialogueIds.Add(dlg.Id))
                validation.AddError($"Duplicate dialogue ID: '{dlg.Id}' in '{filename}'");

            if (string.IsNullOrWhiteSpace(dlg.Speaker))
                validation.AddError($"Dialogue '{dlg.Id}' missing 'speaker'");
            if (dlg.Lines is null || dlg.Lines.Count == 0)
                validation.AddError($"Dialogue '{dlg.Id}' has empty or missing 'lines'");
        }

        // Validate references
        foreach (var npc in content.Npcs)
        {
            if (!string.IsNullOrWhiteSpace(npc.DialogueId) && !dialogueIds.Contains(npc.DialogueId))
                validation.AddError($"NPC '{npc.Id}' references unknown dialogue ID '{npc.DialogueId}'");
        }
    }
}
