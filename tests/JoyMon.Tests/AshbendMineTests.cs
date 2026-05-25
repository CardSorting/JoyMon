using JoyMon.Content;
using JoyMon.Core;
using JoyMon.Game;

namespace JoyMon.Tests;

public class AshbendMineTests
{
    private static readonly string ContentRoot = FindContentRoot();

    [Fact]
    public void AshbendMine_MapValidates()
    {
        var map = new MapLoader(Path.Combine(ContentRoot, "maps")).Load("ashbend-mine.json");

        Assert.Equal("ashbend-mine", map.Id);
        Assert.Equal(16, map.Width);
        Assert.Equal(12, map.Height);
        Assert.NotEmpty(map.Triggers);
        Assert.Contains(map.Triggers, t => t.Kind == "minecartSwitch");
        Assert.Contains(map.Triggers, t => t.Kind == "rockGate");
        Assert.Contains(map.Transitions, t => t.ToMapId == "ashbend-camp");
    }

    [Fact]
    public void MinecartSwitch_ChangesTraversablePath()
    {
        var map = new MapLoader(Path.Combine(ContentRoot, "maps")).Load("ashbend-mine.json");
        var profile = new PlayerProfile();

        Assert.False(MapInteractionService.IsTileWalkable(map, profile, 8, 7));

        profile.SetFlag(MineFlags.MinecartSwitchA, true);

        Assert.True(MapInteractionService.IsTileWalkable(map, profile, 8, 7));
    }

    [Fact]
    public void MinePass_GateWorks()
    {
        var map = new MapLoader(Path.Combine(ContentRoot, "maps")).Load("ashbend-mine.json");
        var profile = new PlayerProfile();

        Assert.False(MapInteractionService.IsTileWalkable(map, profile, 6, 4));
        Assert.True(MapInteractionService.TryGetBlockedMessage(map, profile, 6, 4, out var blocked));
        Assert.Contains("foreman", blocked, StringComparison.OrdinalIgnoreCase);

        profile.SetFlag(MineFlags.MinePass, true);

        Assert.True(MapInteractionService.IsTileWalkable(map, profile, 6, 4));
    }

    [Fact]
    public void Cragmite_AndSmokowl_Validate()
    {
        var database = new ContentLoader(ContentRoot).Load();

        Assert.True(database.Creatures.ContainsKey("cragmite"));
        Assert.True(database.Creatures.ContainsKey("smokowl"));

        var cragmite = database.Creatures["cragmite"];
        Assert.Equal("Stone", cragmite.Type);
        Assert.Equal(new[] { "pebble-toss", "guard-curl", "tackle" }, cragmite.Learnset);

        var smokowl = database.Creatures["smokowl"];
        Assert.Equal("Ember", smokowl.Type);
        Assert.Equal("Echo", smokowl.SecondaryType);
        Assert.Contains("ember-nudge", smokowl.Learnset);
        Assert.Contains("echo-chirp", smokowl.Learnset);
    }

    [Fact]
    public void AshbendMine_EncounterTableValidates()
    {
        var database = new ContentLoader(ContentRoot).Load();
        var table = new EncounterLoader(Path.Combine(ContentRoot, "encounters"))
            .Load("ashbend-mine.json", database.Species.Keys.ToHashSet());

        Assert.Equal("ashbend-mine-encounters", table.Id);
        Assert.Equal("ashbend-mine", table.MapId);
        Assert.Contains(table.Entries, e => e.CreatureId == "pebblit" && e.MinLevel == 13 && e.MaxLevel == 15);
        Assert.Contains(table.Entries, e => e.CreatureId == "coalboar" && e.MaxLevel == 16);
        Assert.Contains(table.Entries, e => e.CreatureId == "cragmite" && e.MinLevel == 15);
        Assert.Contains(table.Entries, e => e.CreatureId == "smokowl" && e.MaxLevel == 16);
    }

    [Fact]
    public void MinecartSwitch_SetsFlagOnInteract()
    {
        var map = new MapLoader(Path.Combine(ContentRoot, "maps")).Load("ashbend-mine.json");
        var profile = new PlayerProfile();
        var trigger = map.Triggers.First(t => t.Id == "minecart-switch-a");

        Assert.True(MapInteractionService.TryInteract(map, profile, trigger.Tile.X, trigger.Tile.Y, out var message));
        Assert.True(profile.HasFlag(MineFlags.MinecartSwitchA));
        Assert.Contains("track", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AshbendMine_DialogueLoads()
    {
        var dialogue = new DialogueLoader(Path.Combine(ContentRoot, "dialogue")).Load("ashbend-mine.json");

        Assert.Contains(dialogue.Npcs, n => n.Id == "ashbend-foreman");
        Assert.Contains(dialogue.Npcs, n => n.Id == "ashbend-mine-healer");
        Assert.Contains(dialogue.Npcs, n => n.Id == "ashbend-boss-chamber-sign");
        Assert.Contains(dialogue.Dialogues, d => d.Id == "ashbend-boss-chamber-talk");
    }

    private static string FindContentRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var contentPath = Path.Combine(directory.FullName, "content");
            if (Directory.Exists(contentPath) && File.Exists(Path.Combine(directory.FullName, "JoyMon.sln")))
                return contentPath;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository content directory.");
    }
}
