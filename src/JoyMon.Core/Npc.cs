namespace JoyMon.Core;

public class Npc
{
    public string Id { get; }
    public string Name { get; }
    public int X { get; }
    public int Y { get; }
    public Direction Facing { get; set; }
    public string DialogueId { get; }
    public string SpriteId { get; }
    public string MapId { get; }

    public Npc(string id, string name, int x, int y, Direction facing, string dialogueId, string spriteId, string mapId = "starter-town")
    {
        Id = id;
        Name = name;
        X = x;
        Y = y;
        Facing = facing;
        DialogueId = dialogueId;
        SpriteId = spriteId;
        MapId = mapId;
    }
}

