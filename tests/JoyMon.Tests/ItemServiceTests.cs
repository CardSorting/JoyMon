using JoyMon.Core;

namespace JoyMon.Tests;

public class ItemServiceTests
{
    [Fact]
    public void UseItem_DecrementsQuantity()
    {
        var inventory = new Inventory();
        inventory.SetQuantity(ItemCatalog.BerryTonicId, 3);
        var joymon = MakeSpecies().CreateInstance(5);
        joymon.CurrentHp = 10;

        var result = ItemService.TryUse(inventory, ItemCatalog.BerryTonicId, joymon);

        Assert.Equal(UseItemResult.Success, result);
        Assert.Equal(2, inventory.GetQuantity(ItemCatalog.BerryTonicId));
    }

    [Fact]
    public void UseItem_MissingItem_Fails()
    {
        var inventory = new Inventory();
        var joymon = MakeSpecies().CreateInstance(5);
        joymon.CurrentHp = 10;

        var result = ItemService.TryUse(inventory, ItemCatalog.BerryTonicId, joymon);

        Assert.Equal(UseItemResult.MissingItem, result);
        Assert.Equal(10, joymon.CurrentHp);
    }

    [Fact]
    public void BerryTonic_HealsTenHp()
    {
        var inventory = new Inventory();
        inventory.SetQuantity(ItemCatalog.BerryTonicId, 1);
        var joymon = MakeSpecies(baseHp: 30).CreateInstance(5);
        joymon.CurrentHp = 12;

        var result = ItemService.TryUse(inventory, ItemCatalog.BerryTonicId, joymon);

        Assert.Equal(UseItemResult.Success, result);
        Assert.Equal(22, joymon.CurrentHp);
    }

    [Fact]
    public void BerryTonic_CannotExceedMaxHp()
    {
        var inventory = new Inventory();
        inventory.SetQuantity(ItemCatalog.BerryTonicId, 1);
        var joymon = MakeSpecies(baseHp: 30).CreateInstance(5);
        joymon.CurrentHp = joymon.MaxHp - 4;

        var result = ItemService.TryUse(inventory, ItemCatalog.BerryTonicId, joymon);

        Assert.Equal(UseItemResult.Success, result);
        Assert.Equal(joymon.MaxHp, joymon.CurrentHp);
        Assert.Equal(0, inventory.GetQuantity(ItemCatalog.BerryTonicId));
    }

    [Fact]
    public void BerryTonic_CannotReviveFaintedJoyMon()
    {
        var inventory = new Inventory();
        inventory.SetQuantity(ItemCatalog.BerryTonicId, 1);
        var joymon = MakeSpecies().CreateInstance(5);
        joymon.CurrentHp = 0;

        var result = ItemService.TryUse(inventory, ItemCatalog.BerryTonicId, joymon);

        Assert.Equal(UseItemResult.TargetFainted, result);
        Assert.Equal(0, joymon.CurrentHp);
        Assert.Equal(1, inventory.GetQuantity(ItemCatalog.BerryTonicId));
    }

    [Fact]
    public void BerryTonic_InvalidTarget_FailsSafely()
    {
        var inventory = new Inventory();
        inventory.SetQuantity(ItemCatalog.BerryTonicId, 1);

        var result = ItemService.TryUse(inventory, ItemCatalog.BerryTonicId, target: null);

        Assert.Equal(UseItemResult.InvalidTarget, result);
        Assert.Equal(1, inventory.GetQuantity(ItemCatalog.BerryTonicId));
    }

    [Fact]
    public void BerryTonic_AlreadyFullHp_DoesNotConsume()
    {
        var inventory = new Inventory();
        inventory.SetQuantity(ItemCatalog.BerryTonicId, 1);
        var joymon = MakeSpecies().CreateInstance(5);

        var result = ItemService.TryUse(inventory, ItemCatalog.BerryTonicId, joymon);

        Assert.Equal(UseItemResult.AlreadyFullHp, result);
        Assert.Equal(1, inventory.GetQuantity(ItemCatalog.BerryTonicId));
    }

    private static JoyMonSpecies MakeSpecies(int baseHp = 30)
    {
        var move = new MoveDefinition("test-hit", "Test Hit", JoyMonType.Neutral, 35, 100, 20);
        return new JoyMonSpecies("Testmon", JoyMonType.Neutral, baseHp, 10, 10, 10, new[] { move });
    }
}
