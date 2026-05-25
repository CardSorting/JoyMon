namespace JoyMon.Game.Services;

public sealed class SaveGame
{
    public int SchemaVersion { get; init; }
    public SaveProfile Profile { get; init; } = new();
    public string CurrentMap { get; init; } = "";
    public SaveTilePosition PlayerTilePosition { get; init; } = new();
    public List<SaveJoyMon> Party { get; init; } = new();
    public List<SaveInventorySlot> Inventory { get; init; } = new();
    public Dictionary<string, bool> Flags { get; init; } = new();
    public List<string> DefeatedTrainers { get; init; } = new();
    public List<string> Captures { get; init; } = new();
    public double PlayTimeSeconds { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}

public sealed class SaveProfile
{
    public string Name { get; init; } = "Player";
}

public sealed class SaveInventorySlot
{
    public string ItemId { get; init; } = "";
    public int Quantity { get; init; }
}

public sealed class SaveTilePosition
{
    public int X { get; init; }
    public int Y { get; init; }
}

public sealed class SaveJoyMon
{
    public string SpeciesId { get; init; } = "";
    public int Level { get; init; }
    public int CurrentHp { get; init; }
    public int MaxHp { get; init; }
    public int Attack { get; init; }
    public int Defense { get; init; }
    public int Speed { get; init; }
    public int Xp { get; init; }
    public List<int> RemainingUses { get; init; } = new();
}
