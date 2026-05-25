using JoyMon.Content;
using JoyMon.Core;
using JoyMon.Game;
using JoyMon.Game.Services;

namespace JoyMon.Tests;

public class FrostveilFoundationTests
{
    private static readonly string ContentRoot = FindContentRoot();

    [Fact]
    public void MountainLift_BlocksBeforeAshbendMineCleared()
    {
        var map = new MapLoader(Path.Combine(ContentRoot, "maps")).Load("ashbend-camp.json");
        var profile = new PlayerProfile();

        var transition = Assert.Single(map.Transitions.Where(t => t.ToMapId == "mountain-lift"));

        Assert.Equal("ashbend_mine_cleared", transition.RequiredFlag);
        Assert.False(IsWalkable(map, profile, transition.FromTile.X, transition.FromTile.Y));
    }

    [Fact]
    public void MountainLift_OpensAfterAshbendMineCleared()
    {
        var map = new MapLoader(Path.Combine(ContentRoot, "maps")).Load("ashbend-camp.json");
        var profile = new PlayerProfile();
        profile.SetFlag("ashbend_mine_cleared", true);

        var transition = Assert.Single(map.Transitions.Where(t => t.ToMapId == "mountain-lift"));

        Assert.True(IsWalkable(map, profile, transition.FromTile.X, transition.FromTile.Y));
    }

    [Fact]
    public void FrostType_IsAcceptedByContentValidation()
    {
        var database = new ContentLoader(ContentRoot).Load();

        Assert.Equal(JoyMonType.Frost, database.Species["snobble"].Type);
        Assert.Equal(JoyMonType.Frost, database.Species["chilleaf"].Type);
        Assert.Equal("Frost/Moss", database.Species["chilleaf"].TypeDisplay);
    }

    [Fact]
    public void FrostveilPathEncounters_ValidateSuccessfully()
    {
        var database = new ContentLoader(ContentRoot).Load();
        var table = new EncounterLoader(Path.Combine(ContentRoot, "encounters"))
            .Load("frostveil-path.json", database.Species.Keys.ToHashSet());

        Assert.Equal("frostveil-path", table.MapId);
        Assert.Equal("snow-grass", table.ZoneId);
        Assert.Equal(new[] { 9 }, table.TileIds);

        Assert.Contains(table.Entries, e => e.CreatureId == "snobble" && e.MinLevel == 16 && e.MaxLevel == 18);
        Assert.Contains(table.Entries, e => e.CreatureId == "chilleaf" && e.MinLevel == 17 && e.MaxLevel == 19);
        Assert.Contains(table.Entries, e => e.CreatureId == "staticrow" && e.MinLevel == 16 && e.MaxLevel == 18);
        Assert.Contains(table.Entries, e => e.CreatureId == "rootsnail" && e.MinLevel == 17 && e.MaxLevel == 18);
    }

    [Fact]
    public void FrostveilPathTrainers_ValidateSuccessfully()
    {
        var database = new ContentLoader(ContentRoot).Load();
        var trainers = new TrainerLoader(Path.Combine(ContentRoot, "trainers"))
            .LoadAll("frostveil-path.json", database.Species.Keys.ToHashSet(), database.Moves.Keys.ToHashSet());
        var map = new MapLoader(Path.Combine(ContentRoot, "maps")).Load("frostveil-path.json");
        var profile = new PlayerProfile();

        Assert.Equal(3, trainers.Count);
        Assert.All(trainers, trainer =>
        {
            Assert.Equal("frostveil-path", trainer.MapId);
            Assert.True(
                MapInteractionService.IsTileWalkable(map, profile, trainer.TilePosition.X, trainer.TilePosition.Y),
                $"{trainer.Id} should be placed on a walkable Frostveil Path tile.");
        });
        Assert.Contains(trainers, t => t.Id == "lodge-runner-mina"
            && t.DisplayName == "Lodge Runner Mina"
            && t.Party.Any(p => p.CreatureId == "snobble" && p.Level == 18)
            && t.Party.Any(p => p.CreatureId == "chilleaf" && p.Level == 18));
        Assert.Contains(trainers, t => t.Id == "static-skater-orro"
            && t.Party.Any(p => p.CreatureId == "staticrow" && p.Level == 18)
            && t.Party.Any(p => p.CreatureId == "ashkit" && p.Level == 17));
        Assert.Contains(trainers, t => t.Id == "moss-hermit-pela"
            && t.Party.Any(p => p.CreatureId == "chilleaf" && p.Level == 19)
            && t.Party.Any(p => p.CreatureId == "drizzleaf" && p.Level == 18));
    }

    [Fact]
    public void FrostveilTrainerDefeats_PersistInSave()
    {
        var service = new SaveService(new ContentLoader(ContentRoot).Load());
        var profile = new PlayerProfile();
        var player = new Player();
        player.Initialize(8, 11);

        var save = service.CreateSave(
            profile,
            player,
            "frostveil-path",
            new[] { "lodge-runner-mina", "static-skater-orro" });
        var roundTrip = service.Deserialize(service.Serialize(save));

        Assert.Contains("lodge-runner-mina", roundTrip.DefeatedTrainers);
        Assert.Contains("static-skater-orro", roundTrip.DefeatedTrainers);
    }

    [Fact]
    public void ShrineGate_BlocksBeforeTwoFrostveilTrainerDefeats()
    {
        var map = new MapLoader(Path.Combine(ContentRoot, "maps")).Load("snowbell-lodge.json");
        var profile = new PlayerProfile();
        var defeated = new HashSet<string> { "lodge-runner-mina" };

        FrostveilShrineGate.RefreshProgress(profile, defeated);

        Assert.False(profile.HasFlag(FrostveilShrineGate.UnlockedFlag));
        Assert.False(MapInteractionService.IsTileWalkable(map, profile, 15, 1));
        Assert.True(MapInteractionService.TryGetBlockedMessage(map, profile, 15, 1, out var blocked));
        Assert.Contains("two Frostveil trainers", blocked);
        Assert.Equal(
            "snowbell-keeper-talk",
            FrostveilShrineGate.ResolveKeeperDialogueId(profile, defeated, "snowbell-keeper-talk"));
    }

    [Fact]
    public void ShrineGate_OpensAfterTwoFrostveilTrainerDefeats()
    {
        var map = new MapLoader(Path.Combine(ContentRoot, "maps")).Load("snowbell-lodge.json");
        var profile = new PlayerProfile();
        var defeated = new HashSet<string> { "lodge-runner-mina", "static-skater-orro" };

        Assert.True(FrostveilShrineGate.RefreshProgress(profile, defeated));

        Assert.True(profile.HasFlag(FrostveilShrineGate.UnlockedFlag));
        Assert.True(MapInteractionService.IsTileWalkable(map, profile, 15, 1));
        Assert.Contains(map.Transitions, transition =>
            transition.ToMapId == "snowbell-shrine"
            && transition.RequiredFlag == FrostveilShrineGate.UnlockedFlag);
        Assert.Contains(
            new DialogueLoader(Path.Combine(ContentRoot, "dialogue")).Load("snowbell-lodge.json").Dialogues,
            dialogue => dialogue.Id == "snowbell-keeper-talk-unlocked");
        Assert.Equal(
            "snowbell-keeper-talk-unlocked",
            FrostveilShrineGate.ResolveKeeperDialogueId(profile, defeated, "snowbell-keeper-talk"));
    }

    [Fact]
    public void SnobbleAndChilleafContent_ValidateSuccessfully()
    {
        var database = new ContentLoader(ContentRoot).Load();

        var snobble = database.Creatures["snobble"];
        Assert.Equal("Snobble", snobble.Name);
        Assert.Equal("Frost", snobble.Type);
        Assert.Null(snobble.SecondaryType);
        Assert.True(snobble.BaseStats.Defense > snobble.BaseStats.Attack);
        Assert.Contains("guard-curl", snobble.Learnset);

        var chilleaf = database.Creatures["chilleaf"];
        Assert.Equal("Chilleaf", chilleaf.Name);
        Assert.Equal("Frost", chilleaf.Type);
        Assert.Equal("Moss", chilleaf.SecondaryType);
        Assert.Contains("moss-tap", chilleaf.Learnset);
        Assert.Contains("guard-curl", chilleaf.Learnset);
    }

    [Fact]
    public void FrostveilRegionFlow_ValidatesToSnowbellLodge()
    {
        var maps = new MapLoader(Path.Combine(ContentRoot, "maps"));
        var lift = maps.Load("mountain-lift.json");
        var path = maps.Load("frostveil-path.json");
        var lodge = maps.Load("snowbell-lodge.json");

        Assert.Contains(lift.Transitions, t => t.ToMapId == "frostveil-path");
        Assert.Contains(path.Transitions, t => t.ToMapId == "snowbell-lodge");
        Assert.Contains(lodge.Transitions, t => t.ToMapId == "frostveil-path");
    }

    private static bool IsWalkable(MapContent map, PlayerProfile profile, int x, int y)
    {
        if (x < 0 || x >= map.Width || y < 0 || y >= map.Height)
            return false;
        if (map.Layers.Collision[y][x] != 0)
            return false;

        var transition = map.Transitions.FirstOrDefault(t => t.FromTile.X == x && t.FromTile.Y == y);
        return transition is null
            || string.IsNullOrEmpty(transition.RequiredFlag)
            || profile.HasFlag(transition.RequiredFlag);
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
