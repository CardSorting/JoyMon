namespace JoyMon.Core;

public sealed class ItemDefinition
{
    public string Id { get; }
    public string Name { get; }
    public ItemKind Kind { get; }
    public ItemEffect? Effect { get; }

    public ItemDefinition(string id, string name, ItemKind kind, ItemEffect? effect = null)
    {
        Id = id;
        Name = name;
        Kind = kind;
        Effect = effect;
    }
}
