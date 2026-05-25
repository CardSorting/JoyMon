namespace JoyMon.Core;

public sealed class InventorySlot
{
    public string ItemId { get; }
    public int Quantity { get; set; }

    public InventorySlot(string itemId, int quantity)
    {
        ItemId = itemId;
        Quantity = quantity;
    }
}
