using System.Collections.Generic;

namespace JoyMon.Core;

public class PlayerProfile
{
    public string Name { get; set; } = "Player";
    public List<JoyMonInstance> Party { get; } = new();
    public List<string> Inventory { get; } = new();
    public Dictionary<string, bool> Flags { get; } = new();

    public bool HasFlag(string key)
    {
        return Flags.TryGetValue(key, out var val) && val;
    }

    public void SetFlag(string key, bool value)
    {
        Flags[key] = value;
    }
}
