namespace JoyMon.Core;

public static class ItemService
{
    public const int BerryTonicHealAmount = 10;

    public static UseItemResult TryUse(
        Inventory inventory,
        string itemId,
        JoyMonInstance? target,
        bool allowReviveFainted = false)
    {
        if (!ItemCatalog.TryGet(itemId, out var definition))
            return UseItemResult.UnknownItem;

        if (inventory.GetQuantity(itemId) <= 0)
            return UseItemResult.MissingItem;

        return definition.Kind switch
        {
            ItemKind.Heal => TryUseHeal(inventory, definition, target, allowReviveFainted),
            _ => UseItemResult.InvalidTarget,
        };
    }

    private static UseItemResult TryUseHeal(
        Inventory inventory,
        ItemDefinition definition,
        JoyMonInstance? target,
        bool allowReviveFainted)
    {
        if (target is null)
            return UseItemResult.InvalidTarget;

        if (target.IsFainted && !allowReviveFainted)
            return UseItemResult.TargetFainted;

        if (target.CurrentHp >= target.MaxHp)
            return UseItemResult.AlreadyFullHp;

        int healAmount = definition.Effect?.HealAmount ?? BerryTonicHealAmount;
        target.CurrentHp = Math.Min(target.MaxHp, target.CurrentHp + healAmount);
        inventory.TryConsume(definition.Id);
        return UseItemResult.Success;
    }
}
