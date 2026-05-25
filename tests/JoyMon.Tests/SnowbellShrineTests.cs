using JoyMon.Content;
using JoyMon.Core;
using JoyMon.Game;
using JoyMon.Game.Services;

namespace JoyMon.Tests;

public class SnowbellShrineTests
{
    private static readonly string ContentRoot = FindContentRoot();

    [Fact]
    public void SnowbellShrine_MapValidates()
    {
        var map = new MapLoader(Path.Combine(ContentRoot, "maps")).Load("snowbell-shrine.json");

        Assert.Equal("snowbell-shrine", map.Id);
        Assert.Equal(22, map.Width);
        Assert.Equal(18, map.Height);
        Assert.NotNull(map.Layers.MovementEffect);
        Assert.Contains(map.Triggers, t => t.Kind == "bellSwitch");
        Assert.Contains(map.Triggers, t => t.Kind == "rockGate" && t.Id == "frozen-inner-door");
        Assert.Contains(map.Triggers, t => t.Kind == "warmingBrazier");
        Assert.Contains(map.Transitions, t => t.ToMapId == "snowbell-lodge");

        var lodge = new MapLoader(Path.Combine(ContentRoot, "maps")).Load("snowbell-lodge.json");
        Assert.Contains(lodge.Transitions, t => t.ToMapId == "snowbell-shrine");
    }

    [Fact]
    public void BellSwitches_PersistAndSolvePattern()
    {
        var map = new MapLoader(Path.Combine(ContentRoot, "maps")).Load("snowbell-shrine.json");
        var profile = new PlayerProfile();

        var north = map.Triggers.First(t => t.Id == "bell-north");
        var west = map.Triggers.First(t => t.Id == "bell-west");
        var east = map.Triggers.First(t => t.Id == "bell-east");

        Assert.True(MapInteractionService.TryInteract(map, profile, north.Tile.X, north.Tile.Y, out _));
        Assert.True(profile.HasFlag(ShrineFlags.BellNorth));
        Assert.False(profile.HasFlag(ShrineFlags.BellPatternSolved));

        Assert.True(MapInteractionService.TryInteract(map, profile, west.Tile.X, west.Tile.Y, out _));
        Assert.True(profile.HasFlag(ShrineFlags.BellWest));
        Assert.False(profile.HasFlag(ShrineFlags.BellPatternSolved));

        Assert.True(MapInteractionService.TryInteract(map, profile, east.Tile.X, east.Tile.Y, out _));
        Assert.True(profile.HasFlag(ShrineFlags.BellEast));
        Assert.True(profile.HasFlag(ShrineFlags.BellPatternSolved));
    }

    [Fact]
    public void FrozenDoor_OpensAfterBellPatternSolved()
    {
        var map = new MapLoader(Path.Combine(ContentRoot, "maps")).Load("snowbell-shrine.json");
        var profile = new PlayerProfile();

        Assert.False(MapInteractionService.IsTileWalkable(map, profile, 10, 5));
        Assert.True(MapInteractionService.TryGetBlockedMessage(map, profile, 10, 5, out var blocked));
        Assert.Contains("bell", blocked, StringComparison.OrdinalIgnoreCase);

        profile.SetFlag(ShrineFlags.BellPatternSolved, true);

        Assert.True(MapInteractionService.IsTileWalkable(map, profile, 10, 5));
    }

    [Fact]
    public void WarmingBrazier_HealsParty()
    {
        var map = new MapLoader(Path.Combine(ContentRoot, "maps")).Load("snowbell-shrine.json");
        var database = new ContentLoader(ContentRoot).Load();
        var profile = new PlayerProfile();
        var joymon = database.Species["snobble"].CreateInstance(20);
        joymon.CurrentHp = 5;
        joymon.RemainingUses[0] = 0;
        profile.Party.Add(joymon);

        var brazier = map.Triggers.First(t => t.Id == "warming-brazier");
        var healed = false;

        Assert.True(MapInteractionService.TryInteract(
            map,
            profile,
            brazier.Tile.X,
            brazier.Tile.Y,
            out var message,
            () => healed = HealPartyLikeGame(profile)));

        Assert.True(healed);
        Assert.Equal(joymon.MaxHp, joymon.CurrentHp);
        Assert.True(joymon.RemainingUses[0] > 0);
        Assert.Contains("brazier", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BellcubAndFrostmoth_Validate()
    {
        var database = new ContentLoader(ContentRoot).Load();

        Assert.True(database.Creatures.ContainsKey("bellcub"));
        Assert.True(database.Creatures.ContainsKey("frostmoth"));

        var bellcub = database.Creatures["bellcub"];
        Assert.Equal("Bellcub", bellcub.Name);
        Assert.Equal("Echo", bellcub.Type);
        Assert.Equal("Frost", bellcub.SecondaryType);
        Assert.Contains("echo-chirp", bellcub.Learnset);

        var frostmoth = database.Creatures["frostmoth"];
        Assert.Equal("Frostmoth", frostmoth.Name);
        Assert.Equal("Frost", frostmoth.Type);
        Assert.Null(frostmoth.SecondaryType);
        Assert.True(frostmoth.BaseStats.Speed > frostmoth.BaseStats.Attack);
        Assert.Contains("echo-chirp", frostmoth.Learnset);
    }

    [Fact]
    public void SnowbellShrine_EncounterTableValidates()
    {
        var database = new ContentLoader(ContentRoot).Load();
        var table = new EncounterLoader(Path.Combine(ContentRoot, "encounters"))
            .Load("snowbell-shrine.json", database.Species.Keys.ToHashSet());

        Assert.Equal("snowbell-shrine", table.MapId);
        Assert.Equal("shrine-halls", table.ZoneId);

        Assert.Contains(table.Entries, e => e.CreatureId == "snobble" && e.MinLevel == 18 && e.MaxLevel == 20);
        Assert.Contains(table.Entries, e => e.CreatureId == "bellcub" && e.MinLevel == 19 && e.MaxLevel == 21);
        Assert.Contains(table.Entries, e => e.CreatureId == "frostmoth" && e.MinLevel == 20 && e.MaxLevel == 21);
        Assert.Contains(table.Entries, e => e.CreatureId == "chilleaf" && e.MinLevel == 19 && e.MaxLevel == 20);
    }

    [Fact]
    public void ShrineDialogue_LoadsBossChamberPlaceholder()
    {
        var dialogue = new DialogueLoader(Path.Combine(ContentRoot, "dialogue")).Load("snowbell-shrine.json");

        Assert.Contains(dialogue.Npcs, n => n.Id == "shrine-boss-chamber-sign");
        Assert.Contains(dialogue.Dialogues, d => d.Id == "shrine-boss-chamber-talk");
        Assert.Contains(dialogue.Dialogues.First(d => d.Id == "shrine-boss-chamber-talk").Lines,
            l => l.Contains("Placeholder", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ShrineFlags_PersistThroughSaveRoundTrip()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "JoyMonShrineSave_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        try
        {
            var database = new ContentLoader(ContentRoot).Load();
            var profile = new PlayerProfile();
            profile.SetFlag(ShrineFlags.BellNorth, true);
            profile.SetFlag(ShrineFlags.BellPatternSolved, true);

            var player = new Player();
            player.Initialize(11, 16);

            var service = new SaveService(database, Path.Combine(tempDir, "save.json"));
            service.Save(profile, player, "snowbell-shrine");

            var restoredProfile = new PlayerProfile();
            var restoredPlayer = new Player();
            service.Restore(service.LoadSave(), restoredProfile, restoredPlayer);

            Assert.True(restoredProfile.HasFlag(ShrineFlags.BellNorth));
            Assert.True(restoredProfile.HasFlag(ShrineFlags.BellPatternSolved));
            Assert.Equal("snowbell-shrine", service.LoadSave().CurrentMap);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    private static bool HealPartyLikeGame(PlayerProfile profile)
    {
        foreach (var joymon in profile.Party)
        {
            joymon.CurrentHp = joymon.MaxHp;
            for (int i = 0; i < joymon.RemainingUses.Length && i < joymon.Species.Moves.Count; i++)
                joymon.RemainingUses[i] = joymon.Species.Moves[i].MaxUses;
        }

        return profile.Party.Count > 0;
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
