using System.Collections.Generic;
using System.Linq;

namespace JoyMon.Core;

public sealed class Inventory
{
    private readonly List<InventorySlot> _slots = new();

    public IReadOnlyList<InventorySlot> Slots => _slots;

    public void Clear()
    {
        _slots.Clear();
    }

    public int GetQuantity(string itemId)
    {
        return _slots.FirstOrDefault(slot => slot.ItemId == itemId)?.Quantity ?? 0;
    }

    public void SetQuantity(string itemId, int quantity)
    {
        if (quantity <= 0)
        {
            _slots.RemoveAll(slot => slot.ItemId == itemId);
            return;
        }

        var slot = _slots.FirstOrDefault(s => s.ItemId == itemId);
        if (slot is null)
            _slots.Add(new InventorySlot(itemId, quantity));
        else
            slot.Quantity = quantity;
    }

    public void Add(string itemId, int quantity)
    {
        if (quantity <= 0)
            return;

        SetQuantity(itemId, GetQuantity(itemId) + quantity);
    }

    public bool TryConsume(string itemId)
    {
        var slot = _slots.FirstOrDefault(s => s.ItemId == itemId);
        if (slot is null || slot.Quantity <= 0)
            return false;

        slot.Quantity--;
        if (slot.Quantity <= 0)
            _slots.Remove(slot);

        return true;
    }
}
