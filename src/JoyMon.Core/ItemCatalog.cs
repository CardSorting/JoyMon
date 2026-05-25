namespace JoyMon.Core;

public static class ItemCatalog
{
    public const string SyncCapsuleId = "sync-capsule";
    public const string BerryTonicId = "berry-tonic";

    public static readonly ItemDefinition SyncCapsule = new(
        SyncCapsuleId,
        "Sync Capsule",
        ItemKind.Capture);

    public static readonly ItemDefinition BerryTonic = new(
        BerryTonicId,
        "Berry Tonic",
        ItemKind.Heal,
        new ItemEffect(healAmount: 10));

    private static readonly Dictionary<string, ItemDefinition> Definitions = new()
    {
        [SyncCapsuleId] = SyncCapsule,
        [BerryTonicId] = BerryTonic,
    };

    public static bool TryGet(string itemId, out ItemDefinition definition)
    {
        if (Definitions.TryGetValue(itemId, out var found))
        {
            definition = found;
            return true;
        }

        definition = null!;
        return false;
    }
}
