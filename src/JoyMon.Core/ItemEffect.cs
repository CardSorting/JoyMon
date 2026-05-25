namespace JoyMon.Core;

public sealed class ItemEffect
{
    public int HealAmount { get; }

    public ItemEffect(int healAmount)
    {
        HealAmount = healAmount;
    }
}
