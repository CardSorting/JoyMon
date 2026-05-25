using System.Text.Json;
using JoyMon.Core;

namespace JoyMon.Content;

/// <summary>
/// Loads, validates, and converts JSON content files into Core domain types.
/// </summary>
public class ContentLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly string _contentRoot;

    /// <param name="contentRoot">Absolute path to the solution-level content/ directory.</param>
    public ContentLoader(string contentRoot)
    {
        _contentRoot = contentRoot;
    }

    /// <summary>
    /// Loads all content files from the content directory, validates, and converts to domain models.
    /// Throws <see cref="InvalidContentException"/> on any validation failure.
    /// </summary>
    public ContentDatabase Load()
    {
        var validation = new ContentValidationResult();

        // ── 1. Load moves ──
        var movesDir = Path.Combine(_contentRoot, "moves");
        var rawMoves = LoadJsonFiles<MoveContent>(movesDir, "move", validation);

        // ── 2. Load creatures ──
        var creaturesDir = Path.Combine(_contentRoot, "creatures");
        var rawCreatures = LoadJsonFiles<CreatureContent>(creaturesDir, "creature", validation);

        // ── 3. Validate ──
        ValidateRequiredFields(rawMoves, rawCreatures, validation);
        ValidateUniqueIds(rawMoves, rawCreatures, validation);
        ValidateLearnsetReferences(rawCreatures, rawMoves, validation);

        // Stop early if invalid
        validation.ThrowIfInvalid();

        // ── 4. Convert to Core domain types ──
        var moveDefs = ConvertMoves(rawMoves, validation);
        var species = ConvertCreatures(rawCreatures, moveDefs, validation);

        // Final check after conversion
        validation.ThrowIfInvalid();

        return new ContentDatabase(
            rawCreatures.ToDictionary(c => c.Id),
            rawMoves.ToDictionary(m => m.Id),
            species.ToDictionary(s => s.Key, s => s.Value),
            moveDefs.ToDictionary(m => m.Key, m => m.Value));
    }

    // ── File loading ────────────────────────────────────────────

    private static List<T> LoadJsonFiles<T>(string directory, string label, ContentValidationResult validation)
    {
        var results = new List<T>();

        if (!Directory.Exists(directory))
        {
            validation.AddError($"Directory not found: {directory}");
            return results;
        }

        foreach (var file in Directory.EnumerateFiles(directory, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var item = JsonSerializer.Deserialize<T>(json, JsonOptions);
                if (item is null)
                {
                    validation.AddError($"Failed to deserialize {label} file: {file} (result was null)");
                    continue;
                }
                results.Add(item);
            }
            catch (JsonException ex)
            {
                validation.AddError($"Invalid JSON in {label} file '{file}': {ex.Message}");
            }
            catch (Exception ex) when (ex is not InvalidContentException)
            {
                validation.AddError($"Error reading {label} file '{file}': {ex.Message}");
            }
        }

        return results;
    }

    // ── Required fields ─────────────────────────────────────────

    private static void ValidateRequiredFields(
        List<MoveContent> moves,
        List<CreatureContent> creatures,
        ContentValidationResult validation)
    {
        foreach (var move in moves)
        {
            if (string.IsNullOrWhiteSpace(move.Id))
                validation.AddError($"Move missing 'id'");
            if (string.IsNullOrWhiteSpace(move.Name))
                validation.AddError($"Move '{move.Id}' missing 'name'");
            if (string.IsNullOrWhiteSpace(move.Type))
                validation.AddError($"Move '{move.Id}' missing 'type'");
            if (move.Power <= 0)
                validation.AddError($"Move '{move.Id}' has invalid 'power' ({move.Power}); must be > 0");
            if (move.Accuracy < 0 || move.Accuracy > 100)
                validation.AddError($"Move '{move.Id}' has invalid 'accuracy' ({move.Accuracy}); must be 0–100");
            if (move.MaxUses <= 0)
                validation.AddError($"Move '{move.Id}' has invalid 'maxUses' ({move.MaxUses}); must be > 0");
        }

        foreach (var creature in creatures)
        {
            if (string.IsNullOrWhiteSpace(creature.Id))
                validation.AddError($"Creature missing 'id'");
            if (string.IsNullOrWhiteSpace(creature.Name))
                validation.AddError($"Creature '{creature.Id}' missing 'name'");
            if (string.IsNullOrWhiteSpace(creature.Type))
                validation.AddError($"Creature '{creature.Id}' missing 'type'");
            if (creature.BaseStats.MaxHp <= 0)
                validation.AddError($"Creature '{creature.Id}' has invalid baseStats.maxHp ({creature.BaseStats.MaxHp})");
            if (creature.BaseStats.Attack <= 0)
                validation.AddError($"Creature '{creature.Id}' has invalid baseStats.attack ({creature.BaseStats.Attack})");
            if (creature.BaseStats.Defense <= 0)
                validation.AddError($"Creature '{creature.Id}' has invalid baseStats.defense ({creature.BaseStats.Defense})");
            if (creature.BaseStats.Speed <= 0)
                validation.AddError($"Creature '{creature.Id}' has invalid baseStats.speed ({creature.BaseStats.Speed})");
            if (creature.Learnset is null || creature.Learnset.Count == 0)
                validation.AddError($"Creature '{creature.Id}' has empty or missing 'learnset'");
        }
    }

    // ── Unique IDs ──────────────────────────────────────────────

    private static void ValidateUniqueIds(
        List<MoveContent> moves,
        List<CreatureContent> creatures,
        ContentValidationResult validation)
    {
        var moveIds = new HashSet<string>();
        foreach (var move in moves)
        {
            if (!string.IsNullOrWhiteSpace(move.Id) && !moveIds.Add(move.Id))
                validation.AddError($"Duplicate move ID: '{move.Id}'");
        }

        var creatureIds = new HashSet<string>();
        foreach (var creature in creatures)
        {
            if (!string.IsNullOrWhiteSpace(creature.Id) && !creatureIds.Add(creature.Id))
                validation.AddError($"Duplicate creature ID: '{creature.Id}'");
        }
    }

    // ── Learnset references ─────────────────────────────────────

    private static void ValidateLearnsetReferences(
        List<CreatureContent> creatures,
        List<MoveContent> moves,
        ContentValidationResult validation)
    {
        var validMoveIds = moves
            .Where(m => !string.IsNullOrWhiteSpace(m.Id))
            .Select(m => m.Id)
            .ToHashSet();

        foreach (var creature in creatures.Where(c => !string.IsNullOrWhiteSpace(c.Id)))
        {
            if (creature.Learnset is null) continue;

            foreach (var moveId in creature.Learnset)
            {
                if (!validMoveIds.Contains(moveId))
                    validation.AddError($"Creature '{creature.Id}' references unknown move ID '{moveId}'");
            }
        }
    }

    // ── Type conversion helpers ─────────────────────────────────

    private static JoyMonType ParseType(string typeStr, string context, ContentValidationResult validation)
    {
        if (Enum.TryParse<JoyMonType>(typeStr, ignoreCase: true, out var result))
            return result;

        validation.AddError($"'{context}' has invalid type '{typeStr}'. Valid values: {string.Join(", ", Enum.GetNames<JoyMonType>())}");
        return JoyMonType.Neutral;
    }

    // ── Domain conversion ───────────────────────────────────────

    private static Dictionary<string, MoveDefinition> ConvertMoves(
        List<MoveContent> rawMoves,
        ContentValidationResult validation)
    {
        var dict = new Dictionary<string, MoveDefinition>();

        foreach (var raw in rawMoves)
        {
            if (string.IsNullOrWhiteSpace(raw.Id)) continue;

            var type = ParseType(raw.Type, $"move '{raw.Id}'", validation);

            var def = new MoveDefinition(raw.Id, raw.Name, type, raw.Power, raw.Accuracy, raw.MaxUses);
            dict[raw.Id] = def;
        }

        return dict;
    }

    private static Dictionary<string, JoyMonSpecies> ConvertCreatures(
        List<CreatureContent> rawCreatures,
        Dictionary<string, MoveDefinition> moveDefs,
        ContentValidationResult validation)
    {
        var dict = new Dictionary<string, JoyMonSpecies>();

        foreach (var raw in rawCreatures)
        {
            if (string.IsNullOrWhiteSpace(raw.Id)) continue;

            var type = ParseType(raw.Type, $"creature '{raw.Id}'", validation);
            string? typeDisplay = null;
            if (!string.IsNullOrWhiteSpace(raw.SecondaryType))
            {
                _ = ParseType(raw.SecondaryType, $"creature '{raw.Id}' secondaryType", validation);
                typeDisplay = $"{raw.Type}/{raw.SecondaryType}";
            }

            var moves = new List<MoveDefinition>();
            if (raw.Learnset is not null)
            {
                foreach (var moveId in raw.Learnset)
                {
                    if (moveDefs.TryGetValue(moveId, out var moveDef))
                        moves.Add(moveDef);
                }
            }

            if (moves.Count == 0)
            {
                validation.AddError($"Creature '{raw.Id}' has no valid moves in learnset after conversion");
                continue;
            }

            var species = new JoyMonSpecies(
                raw.Name,
                type,
                raw.BaseStats.MaxHp,
                raw.BaseStats.Attack,
                raw.BaseStats.Defense,
                raw.BaseStats.Speed,
                moves,
                typeDisplay);

            dict[raw.Id] = species;
        }

        return dict;
    }
}