namespace JoyMon.Core;

/// <summary>
/// A battle move that a JoyMon can use. Pure data — no framework dependency.
/// </summary>
public class MoveDefinition
{
    public string Id { get; }
    public string Name { get; }
    public JoyMonType Type { get; }
    public int Power { get; }
    public int Accuracy { get; } // 0-100, percentage chance to hit
    public int MaxUses { get; }

    public MoveDefinition(string id, string name, JoyMonType type, int power, int accuracy, int maxUses)
    {
        Id = id;
        Name = name;
        Type = type;
        Power = power;
        Accuracy = accuracy;
        MaxUses = maxUses;
    }
}