using System.Text.Json;

namespace JoyMon.Content;

public class TrainerLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly string _trainersDirectory;

    public TrainerLoader(string trainersDirectory)
    {
        _trainersDirectory = trainersDirectory;
    }

    public TrainerContent Load(string filename, IReadOnlySet<string> validCreatureIds, IReadOnlySet<string> validMoveIds)
    {
        var validation = new ContentValidationResult();
        var path = Path.Combine(_trainersDirectory, filename);

        TrainerContent? trainer;
        try
        {
            var json = File.ReadAllText(path);
            trainer = JsonSerializer.Deserialize<TrainerContent>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidContentException($"Invalid JSON in trainer file '{filename}': {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            throw new InvalidContentException($"Could not read trainer file '{filename}': {ex.Message}", ex);
        }

        if (trainer is null)
            throw new InvalidContentException($"Trainer file '{filename}' deserialized to null.");

        Validate(trainer, filename, validCreatureIds, validMoveIds, validation);
        validation.ThrowIfInvalid();

        return trainer;
    }

    public static void Validate(
        TrainerContent trainer,
        string filename,
        IReadOnlySet<string> validCreatureIds,
        IReadOnlySet<string> validMoveIds,
        ContentValidationResult validation)
    {
        if (string.IsNullOrWhiteSpace(trainer.Id))
            validation.AddError($"Trainer '{filename}' missing 'id'");
        if (string.IsNullOrWhiteSpace(trainer.DisplayName))
            validation.AddError($"Trainer '{trainer.Id}' missing 'displayName'");
        if (string.IsNullOrWhiteSpace(trainer.MapId))
            validation.AddError($"Trainer '{trainer.Id}' missing 'mapId'");
        if (trainer.TilePosition is null)
            validation.AddError($"Trainer '{trainer.Id}' missing 'tilePosition'");
        if (trainer.SightRange < 0)
            validation.AddError($"Trainer '{trainer.Id}' has invalid sightRange ({trainer.SightRange}); must be >= 0");

        ValidateDialogue(trainer.DialogueBefore, trainer.Id, "dialogueBefore", validation);
        ValidateDialogue(trainer.DialogueAfter, trainer.Id, "dialogueAfter", validation);

        if (trainer.Party is null || trainer.Party.Count == 0)
        {
            validation.AddError($"Trainer '{trainer.Id}' has no party members");
        }
        else
        {
            foreach (var member in trainer.Party)
            {
                if (string.IsNullOrWhiteSpace(member.CreatureId))
                    validation.AddError($"Trainer '{trainer.Id}' party member missing 'creatureId'");
                else if (!validCreatureIds.Contains(member.CreatureId))
                    validation.AddError($"Trainer '{trainer.Id}' party references unknown creature ID '{member.CreatureId}'");

                if (member.Level <= 0)
                    validation.AddError($"Trainer '{trainer.Id}' party member '{member.CreatureId}' has invalid level ({member.Level}); must be > 0");

                if (member.Moves is not null)
                {
                    foreach (var moveId in member.Moves)
                    {
                        if (string.IsNullOrWhiteSpace(moveId))
                            validation.AddError($"Trainer '{trainer.Id}' party member '{member.CreatureId}' has empty move ID");
                        else if (!validMoveIds.Contains(moveId))
                            validation.AddError($"Trainer '{trainer.Id}' party member '{member.CreatureId}' references unknown move ID '{moveId}'");
                    }
                }
            }
        }
    }

    private static void ValidateDialogue(
        TrainerDialogueContent? dialogue,
        string trainerId,
        string fieldName,
        ContentValidationResult validation)
    {
        if (dialogue is null)
        {
            validation.AddError($"Trainer '{trainerId}' missing '{fieldName}'");
            return;
        }

        if (string.IsNullOrWhiteSpace(dialogue.Speaker))
            validation.AddError($"Trainer '{trainerId}' '{fieldName}' missing 'speaker'");
        if (dialogue.Lines is null || dialogue.Lines.Count == 0)
            validation.AddError($"Trainer '{trainerId}' '{fieldName}' has empty or missing 'lines'");
    }
}
