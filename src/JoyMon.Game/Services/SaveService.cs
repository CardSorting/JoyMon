using System.Text.Json;
using JoyMon.Content;
using JoyMon.Core;

namespace JoyMon.Game.Services;

public sealed class SaveService
{
    public const int CurrentSchemaVersion = 3;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private readonly ContentDatabase _content;
    private readonly string _savePath;

    public SaveService(ContentDatabase content, string? savePath = null)
    {
        _content = content;
        _savePath = savePath ?? GetDefaultSavePath();
    }

    public string SavePath => _savePath;
    public bool SaveExists => File.Exists(_savePath);

    public static string GetDefaultSavePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(appData))
            appData = AppDomain.CurrentDomain.BaseDirectory;

        return Path.Combine(appData, "JoyMon", "save.json");
    }

    public SaveGame CreateSave(
        PlayerProfile profile,
        Player player,
        string currentMap,
        IEnumerable<string>? defeatedTrainers = null,
        DateTimeOffset? timestamp = null)
    {
        return new SaveGame
        {
            SchemaVersion = CurrentSchemaVersion,
            Profile = new SaveProfile
            {
                Name = profile.Name,
            },
            CurrentMap = currentMap,
            PlayerTilePosition = new SaveTilePosition
            {
                X = player.X,
                Y = player.Y,
            },
            Party = profile.Party.Select(ToSaveJoyMon).ToList(),
            Inventory = profile.Items.Slots
                .Select(slot => new SaveInventorySlot
                {
                    ItemId = slot.ItemId,
                    Quantity = slot.Quantity,
                })
                .ToList(),
            Flags = new Dictionary<string, bool>(profile.Flags),
            DefeatedTrainers = defeatedTrainers?.Distinct().OrderBy(id => id).ToList() ?? new List<string>(),
            Captures = profile.Captures.Distinct().OrderBy(id => id).ToList(),
            PlayTimeSeconds = profile.PlayTimeSeconds,
            Timestamp = timestamp ?? DateTimeOffset.UtcNow,
        };
    }

    public void Save(
        PlayerProfile profile,
        Player player,
        string currentMap,
        IEnumerable<string>? defeatedTrainers = null)
    {
        var save = CreateSave(profile, player, currentMap, defeatedTrainers);
        Save(save);
    }

    public void Save(SaveGame save)
    {
        var directory = Path.GetDirectoryName(_savePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(_savePath, Serialize(save));
    }

    public string Serialize(SaveGame save)
    {
        return JsonSerializer.Serialize(save, JsonOptions);
    }

    public SaveGame LoadSave()
    {
        if (!File.Exists(_savePath))
            throw new FileNotFoundException($"No JoyMon save exists at '{_savePath}'.", _savePath);

        var json = File.ReadAllText(_savePath);
        return Deserialize(json);
    }

    public SaveGame Deserialize(string json)
    {
        var save = JsonSerializer.Deserialize<SaveGame>(json, JsonOptions);
        if (save is null)
            throw new InvalidOperationException("Save file could not be read.");

        if (save.SchemaVersion != CurrentSchemaVersion)
        {
            throw new InvalidOperationException(
                $"Unsupported save schemaVersion {save.SchemaVersion}. Expected {CurrentSchemaVersion}.");
        }

        return save;
    }

    public void Restore(SaveGame save, PlayerProfile profile, Player player)
    {
        if (save.SchemaVersion != CurrentSchemaVersion)
        {
            throw new InvalidOperationException(
                $"Unsupported save schemaVersion {save.SchemaVersion}. Expected {CurrentSchemaVersion}.");
        }

        profile.Name = save.Profile.Name;

        profile.Party.Clear();
        foreach (var savedJoyMon in save.Party)
        {
            profile.Party.Add(FromSaveJoyMon(savedJoyMon));
        }

        profile.Items.Clear();
        foreach (var slot in save.Inventory)
        {
            profile.Items.SetQuantity(slot.ItemId, slot.Quantity);
        }

        profile.Flags.Clear();
        foreach (var flag in save.Flags)
        {
            profile.Flags[flag.Key] = flag.Value;
        }

        profile.Captures.Clear();
        profile.Captures.AddRange(save.Captures);
        profile.PlayTimeSeconds = save.PlayTimeSeconds;

        player.Initialize(save.PlayerTilePosition.X, save.PlayerTilePosition.Y);
    }

    private SaveJoyMon ToSaveJoyMon(JoyMonInstance joyMon)
    {
        return new SaveJoyMon
        {
            SpeciesId = GetSpeciesId(joyMon.Species),
            Level = joyMon.Level,
            CurrentHp = joyMon.CurrentHp,
            MaxHp = joyMon.MaxHp,
            Attack = joyMon.Attack,
            Defense = joyMon.Defense,
            Speed = joyMon.Speed,
            Xp = joyMon.Xp,
            RemainingUses = joyMon.RemainingUses.ToList(),
        };
    }

    private JoyMonInstance FromSaveJoyMon(SaveJoyMon savedJoyMon)
    {
        if (!_content.Species.TryGetValue(savedJoyMon.SpeciesId, out var species))
            throw new InvalidOperationException($"Save references unknown JoyMon species '{savedJoyMon.SpeciesId}'.");

        return new JoyMonInstance(
            species,
            savedJoyMon.Level,
            savedJoyMon.CurrentHp,
            savedJoyMon.MaxHp,
            savedJoyMon.Attack,
            savedJoyMon.Defense,
            savedJoyMon.Speed,
            savedJoyMon.Xp,
            savedJoyMon.RemainingUses.ToArray());
    }

    private string GetSpeciesId(JoyMonSpecies species)
    {
        foreach (var entry in _content.Species)
        {
            if (ReferenceEquals(entry.Value, species))
                return entry.Key;
        }

        foreach (var entry in _content.Species)
        {
            if (entry.Value.Name == species.Name)
                return entry.Key;
        }

        throw new InvalidOperationException($"Cannot save unknown JoyMon species '{species.Name}'.");
    }
}
