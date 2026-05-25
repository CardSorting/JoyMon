using System.Collections.Generic;

namespace JoyMon.Core;

public class PlayerProfile
{
    public const int PartyLimit = 3;
    public const int DefaultSyncCapsules = 5;
    public const int DefaultBerryTonics = 3;

    public string Name { get; set; } = "Player";
    public List<JoyMonInstance> Party { get; } = new();
    public Inventory Items { get; } = new();
    public List<string> Captures { get; } = new();
    public Dictionary<string, bool> Flags { get; } = new();
    public double PlayTimeSeconds { get; set; }

    public void RecordCapture(string speciesId)
    {
        if (!Captures.Contains(speciesId))
            Captures.Add(speciesId);
    }

    public PlayerProfile()
    {
        ResetDefaultItems();
    }

    public void ResetDefaultItems()
    {
        Items.Clear();
        Items.SetQuantity(ItemCatalog.SyncCapsuleId, DefaultSyncCapsules);
        Items.SetQuantity(ItemCatalog.BerryTonicId, DefaultBerryTonics);
    }

    public bool HasFlag(string key)
    {
        return Flags.TryGetValue(key, out var val) && val;
    }

    public void SetFlag(string key, bool value)
    {
        Flags[key] = value;
    }
}
