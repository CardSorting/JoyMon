using JoyMon.Content;
using JoyMon.Core;
using JoyMon.Game;
using JoyMon.Game.Services;

namespace JoyMon.Tests;

public class BloomrailTests
{
    private static readonly string ContentRoot = FindContentRoot();

    [Fact]
    public void BloomrailMaps_Validate()
    {
        var mapLoader = new MapLoader(Path.Combine(ContentRoot, "maps"));
        
        var descent = mapLoader.Load("mountain-descent.json");
        Assert.Equal("mountain-descent", descent.Id);
        Assert.Equal(22, descent.Width);
        Assert.Equal(14, descent.Height);
        Assert.Contains(descent.Transitions, t => t.ToMapId == "snowbell-lodge");
        Assert.Contains(descent.Transitions, t => t.ToMapId == "bloomrail-station");

        var station = mapLoader.Load("bloomrail-station.json");
        Assert.Equal("bloomrail-station", station.Id);
        Assert.Contains(station.Transitions, t => t.ToMapId == "mountain-descent");
        Assert.Contains(station.Transitions, t => t.ToMapId == "flowerline-fields");
        Assert.Contains(station.Triggers, t => t.Kind == "lockedDoor" && t.RequiredFlag == "bloomrail_fields_solved");

        var fields = mapLoader.Load("flowerline-fields.json");
        Assert.Equal("flowerline-fields", fields.Id);
        Assert.Contains(fields.Transitions, t => t.ToMapId == "bloomrail-station");
    }

    [Fact]
    public void MountainDescent_RequiresSnowbellShrineCleared()
    {
        var lodge = new MapLoader(Path.Combine(ContentRoot, "maps")).Load("snowbell-lodge.json");
        var profile = new PlayerProfile();

        var transition = Assert.Single(lodge.Transitions.Where(t => t.ToMapId == "mountain-descent"));
        Assert.Equal("snowbell_shrine_cleared", transition.RequiredFlag);
        Assert.False(IsWalkable(lodge, profile, transition.FromTile.X, transition.FromTile.Y));

        profile.SetFlag("snowbell_shrine_cleared", true);
        Assert.True(IsWalkable(lodge, profile, transition.FromTile.X, transition.FromTile.Y));
    }

    [Fact]
    public void BloomrailEncounters_Validate()
    {
        var database = new ContentLoader(ContentRoot).Load();
        var table = new EncounterLoader(Path.Combine(ContentRoot, "encounters"))
            .Load("flowerline-fields.json", database.Species.Keys.ToHashSet());

        Assert.Equal("flowerline-fields", table.MapId);
        Assert.Equal("flower-grass", table.ZoneId);
        Assert.Contains(table.Entries, e => e.CreatureId == "pollibun" && e.MinLevel == 21 && e.MaxLevel == 23);
        Assert.Contains(table.Entries, e => e.CreatureId == "railmouse" && e.MinLevel == 22 && e.MaxLevel == 24);
        Assert.Contains(table.Entries, e => e.CreatureId == "queuebee" && e.MinLevel == 21 && e.MaxLevel == 23);
        Assert.Contains(table.Entries, e => e.CreatureId == "chilleaf" && e.MinLevel == 22 && e.MaxLevel == 24);
    }

    [Fact]
    public void BloomrailCreatures_Validate()
    {
        var database = new ContentLoader(ContentRoot).Load();

        Assert.True(database.Creatures.ContainsKey("pollibun"));
        var pollibun = database.Creatures["pollibun"];
        Assert.Equal("Pollibun", pollibun.Name);
        Assert.Equal("Moss", pollibun.Type);
        Assert.Equal("Echo", pollibun.SecondaryType);
        Assert.Contains("moss-tap", pollibun.Learnset);
        Assert.Contains("echo-chirp", pollibun.Learnset);

        Assert.True(database.Creatures.ContainsKey("railmouse"));
        var railmouse = database.Creatures["railmouse"];
        Assert.Equal("Railmouse", railmouse.Name);
        Assert.Equal("Spark", railmouse.Type);
        Assert.Null(railmouse.SecondaryType);
        Assert.Contains("spark-peck", railmouse.Learnset);
    }

    [Fact]
    public void Backtracking_Works()
    {
        var mapLoader = new MapLoader(Path.Combine(ContentRoot, "maps"));
        var profile = new PlayerProfile();

        var fields = mapLoader.Load("flowerline-fields.json");
        var fieldToStation = Assert.Single(fields.Transitions.Where(t => t.ToMapId == "bloomrail-station"));
        Assert.True(IsWalkable(fields, profile, fieldToStation.FromTile.X, fieldToStation.FromTile.Y));

        var station = mapLoader.Load("bloomrail-station.json");
        var stationToDescent = Assert.Single(station.Transitions.Where(t => t.ToMapId == "mountain-descent"));
        Assert.True(IsWalkable(station, profile, stationToDescent.FromTile.X, stationToDescent.FromTile.Y));

        var descent = mapLoader.Load("mountain-descent.json");
        var descentToLodge = Assert.Single(descent.Transitions.Where(t => t.ToMapId == "snowbell-lodge"));
        Assert.True(IsWalkable(descent, profile, descentToLodge.FromTile.X, descentToLodge.FromTile.Y));
    }

    [Fact]
    public void FlowerlineTrainers_Validate()
    {
        var validCreatures = new HashSet<string> { "pollibun", "queuebee", "railmouse", "staticrow", "chilleaf", "drizzleaf" };
        var validMoves = new HashSet<string> { "moss-tap", "echo-chirp", "tackle", "spark-peck" };
        
        var trainers = new TrainerLoader(Path.Combine(ContentRoot, "trainers"))
            .LoadAll("flowerline-fields.json", validCreatures, validMoves);

        Assert.Equal(3, trainers.Count);

        var sera = Assert.Single(trainers.Where(t => t.Id == "fields-keeper-sera"));
        Assert.Equal("Pollen Keeper Sera", sera.DisplayName);
        Assert.Equal("operator", sera.SpriteId);
        Assert.Equal(5, sera.TilePosition.X);
        Assert.Equal(7, sera.TilePosition.Y);
        Assert.Contains(sera.Party, p => p.CreatureId == "pollibun" && p.Level == 23);
        Assert.Contains(sera.Party, p => p.CreatureId == "queuebee" && p.Level == 23);

        var tiko = Assert.Single(trainers.Where(t => t.Id == "fields-railhand-tiko"));
        Assert.Equal("Railhand Tiko", tiko.DisplayName);
        Assert.Equal("kid", tiko.SpriteId);
        Assert.Equal(11, tiko.TilePosition.X);
        Assert.Equal(10, tiko.TilePosition.Y);
        Assert.Contains(tiko.Party, p => p.CreatureId == "railmouse" && p.Level == 24);
        Assert.Contains(tiko.Party, p => p.CreatureId == "staticrow" && p.Level == 23);

        var loma = Assert.Single(trainers.Where(t => t.Id == "fields-chef-loma"));
        Assert.Equal("Meadow Chef Loma", loma.DisplayName);
        Assert.Equal("dr-cedar", loma.SpriteId);
        Assert.Equal(15, loma.TilePosition.X);
        Assert.Equal(7, loma.TilePosition.Y);
        Assert.Contains(loma.Party, p => p.CreatureId == "chilleaf" && p.Level == 23);
        Assert.Contains(loma.Party, p => p.CreatureId == "drizzleaf" && p.Level == 24);
    }

    [Fact]
    public void ConductorDialogue_Changes()
    {
        var profile = new PlayerProfile();
        var defeated = new HashSet<string>();

        // Before solving
        var dialogueId = BloomrailPlatformGate.ResolveConductorDialogueId(profile, defeated, "bloomrail-conductor-talk");
        Assert.Equal("bloomrail-conductor-talk", dialogueId);

        // Defeat 1 trainer
        defeated.Add("fields-keeper-sera");
        BloomrailPlatformGate.RefreshProgress(profile, defeated);
        dialogueId = BloomrailPlatformGate.ResolveConductorDialogueId(profile, defeated, "bloomrail-conductor-talk");
        Assert.Equal("bloomrail-conductor-talk", dialogueId);

        // Defeat 2 trainers
        defeated.Add("fields-railhand-tiko");
        BloomrailPlatformGate.RefreshProgress(profile, defeated);
        dialogueId = BloomrailPlatformGate.ResolveConductorDialogueId(profile, defeated, "bloomrail-conductor-talk");
        Assert.Equal("bloomrail-conductor-talk-solved", dialogueId);
    }

    [Fact]
    public void PlatformGate_BlocksAndOpens()
    {
        var mapLoader = new MapLoader(Path.Combine(ContentRoot, "maps"));
        var station = mapLoader.Load("bloomrail-station.json");
        var profile = new PlayerProfile();
        var defeated = new HashSet<string>();

        // Initially blocked
        Assert.False(IsWalkable(station, profile, 10, 5));

        // Defeat 1 trainer -> still blocked
        defeated.Add("fields-keeper-sera");
        BloomrailPlatformGate.RefreshProgress(profile, defeated);
        Assert.False(IsWalkable(station, profile, 10, 5));

        // Defeat 2 trainers -> opens
        defeated.Add("fields-chef-loma");
        BloomrailPlatformGate.RefreshProgress(profile, defeated);
        Assert.True(IsWalkable(station, profile, 10, 5));
    }

    [Fact]
    public void AbandonedTrainMap_Validates()
    {
        var mapLoader = new MapLoader(Path.Combine(ContentRoot, "maps"));
        var train = mapLoader.Load("abandoned-train.json");

        Assert.Equal("abandoned-train", train.Id);
        Assert.Equal(22, train.Width);
        Assert.Equal(70, train.Height);
        Assert.Equal(10, train.SpawnPoint.X);
        Assert.Equal(10, train.SpawnPoint.Y);

        // Check triggers
        Assert.Contains(train.Triggers, t => t.Id == "train-lever-1" && t.Kind == "switch");
        Assert.Contains(train.Triggers, t => t.Id == "dungeon-door-1" && t.Kind == "lockedDoor");
        Assert.Contains(train.Triggers, t => t.Id == "train-rest-bench" && t.Kind == "healingSpring");
    }

    [Fact]
    public void AbandonedTrainTransitions_Work()
    {
        var mapLoader = new MapLoader(Path.Combine(ContentRoot, "maps"));
        var train = mapLoader.Load("abandoned-train.json");

        // Verify C1 to C2
        var c1ToC2 = Assert.Single(train.Transitions.Where(t => t.FromTile.X == 10 && t.FromTile.Y == 4));
        Assert.Equal("abandoned-train", c1ToC2.ToMapId);
        Assert.Equal(10, c1ToC2.ToTile.X);
        Assert.Equal(15, c1ToC2.ToTile.Y);

        // Verify C2 to C1 (backtracking)
        var c2ToC1 = Assert.Single(train.Transitions.Where(t => t.FromTile.X == 10 && t.FromTile.Y == 14));
        Assert.Equal("abandoned-train", c2ToC1.ToMapId);
        Assert.Equal(10, c2ToC1.ToTile.X);
        Assert.Equal(5, c2ToC1.ToTile.Y);
    }

    [Fact]
    public void LeversAndDoors_ProgressiveUnlock()
    {
        var mapLoader = new MapLoader(Path.Combine(ContentRoot, "maps"));
        var train = mapLoader.Load("abandoned-train.json");
        var profile = new PlayerProfile();

        // 1. Carriage 2 to 3 transition at (10, 25) is blocked initially
        Assert.False(IsWalkable(train, profile, 10, 25));

        // Interaction with switch-1 sets train_lever_1
        var switch1 = Assert.Single(train.Triggers.Where(t => t.Id == "train-lever-1"));
        string message;
        Assert.True(MapInteractionService.TryInteract(train, profile, switch1.Tile.X, switch1.Tile.Y, out message));
        Assert.True(profile.HasFlag("train_lever_1"));

        // Now Carriage 2 to 3 transition at (10, 25) is walkable
        Assert.True(IsWalkable(train, profile, 10, 25));
    }

    [Fact]
    public void NewCreaturesAndEncounters_Validate()
    {
        var database = new ContentLoader(ContentRoot).Load();

        // Validate creatures
        Assert.True(database.Creatures.ContainsKey("cargolem"));
        var cargolem = database.Creatures["cargolem"];
        Assert.Equal("Cargolem", cargolem.Name);
        Assert.Equal("Stone", cargolem.Type);
        Assert.Equal("Spark", cargolem.SecondaryType);

        Assert.True(database.Creatures.ContainsKey("whistowl"));
        var whistowl = database.Creatures["whistowl"];
        Assert.Equal("Whistowl", whistowl.Name);
        Assert.Equal("Echo", whistowl.Type);
        Assert.Equal("Spark", whistowl.SecondaryType);

        // Validate encounter table
        var validCreatureIds = database.Species.Keys.ToHashSet();
        var table = new EncounterLoader(Path.Combine(ContentRoot, "encounters"))
            .Load("abandoned-train.json", validCreatureIds);

        Assert.Equal("abandoned-train", table.MapId);
        Assert.Contains(table.Entries, e => e.CreatureId == "cargolem" && e.MinLevel == 25 && e.MaxLevel == 27);
        Assert.Contains(table.Entries, e => e.CreatureId == "whistowl" && e.MinLevel == 26 && e.MaxLevel == 26);
    }

    [Fact]
    public void ConducthornBoss_Validates()
    {
        var database = new ContentLoader(ContentRoot).Load();
        
        // 1. Creature validates
        Assert.True(database.Creatures.ContainsKey("conducthorn"));
        var creature = database.Creatures["conducthorn"];
        Assert.Equal("Conducthorn", creature.Name);
        Assert.Equal("Spark", creature.Type);
        Assert.Equal("Stone", creature.SecondaryType);
        Assert.Contains("spark-peck", creature.Learnset);
        Assert.Contains("pebble-toss", creature.Learnset);
        Assert.Contains("guard-curl", creature.Learnset);
        Assert.Contains("tackle", creature.Learnset);

        // 2. Boss config validates
        var bossesDir = Path.Combine(ContentRoot, "bosses");
        var boss = new BossLoader(bossesDir).Load("conducthorn.json", database.Species.Keys.ToHashSet());
        
        Assert.Equal("conducthorn", boss.Id);
        Assert.Equal("Conducthorn", boss.DisplayName);
        Assert.Equal("abandoned-train", boss.MapId);
        Assert.Equal(10, boss.GateTile.X);
        Assert.Equal(63, boss.GateTile.Y);
        Assert.Equal(28, boss.Level);
        Assert.Equal("conducthorn", boss.CreatureId);
        Assert.Equal("abandoned_train_cleared", boss.ClearedFlag);
    }

    [Fact]
    public void ConducthornProgression_Works()
    {
        var mapLoader = new MapLoader(Path.Combine(ContentRoot, "maps"));
        var station = mapLoader.Load("bloomrail-station.json");
        var profile = new PlayerProfile();
        var defeated = new HashSet<string>();

        // Initially departure route is blocked (player does not have cleared flag)
        Assert.False(IsWalkable(station, profile, 11, 4));

        // Conductor dialogue before clearing
        var dialogueId = BloomrailPlatformGate.ResolveConductorDialogueId(profile, defeated, "bloomrail-conductor-talk");
        Assert.Equal("bloomrail-conductor-talk", dialogueId);

        // Clear the train (simulated boss victory)
        profile.SetFlag("abandoned_train_cleared", true);

        // Now departure route is walkable
        Assert.True(IsWalkable(station, profile, 11, 4));

        // Conductor dialogue updates to cleared
        dialogueId = BloomrailPlatformGate.ResolveConductorDialogueId(profile, defeated, "bloomrail-conductor-talk");
        Assert.Equal("bloomrail-conductor-talk-cleared", dialogueId);
    }

    private static bool IsWalkable(MapContent map, PlayerProfile profile, int x, int y) =>
        MapInteractionService.IsTileWalkable(map, profile, x, y);

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
