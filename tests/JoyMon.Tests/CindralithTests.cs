using JoyMon.Content;
using JoyMon.Core;
using JoyMon.Game;
using JoyMon.Game.Services;

namespace JoyMon.Tests;

public class CindralithTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "JoyMonCindralithTest_" + Guid.NewGuid());
    private readonly string _contentRoot = FindContentRoot();

    public CindralithTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public void CindralithContent_ValidatesSuccessfully()
    {
        var database = new ContentLoader(_contentRoot).Load();
        var boss = new BossLoader(Path.Combine(_contentRoot, "bosses"))
            .Load("cindralith.json", database.Species.Keys.ToHashSet());

        Assert.True(database.Creatures.ContainsKey("cindralith"));
        Assert.True(database.Species.ContainsKey("cindralith"));

        var creature = database.Creatures["cindralith"];
        Assert.Equal("Cindralith", creature.Name);
        Assert.Equal("Ember", creature.Type);
        Assert.Equal("Stone", creature.SecondaryType);
        Assert.Equal(new[] { "ember-nudge", "pebble-toss", "guard-curl", "tackle" }, creature.Learnset);

        Assert.Equal("cindralith", boss.Id);
        Assert.Equal("ashbend-mine", boss.MapId);
        Assert.Equal(8, boss.GateTile.X);
        Assert.Equal(2, boss.GateTile.Y);
        Assert.Equal(17, boss.Level);
        Assert.Equal("ashbend_mine_cleared", boss.ClearedFlag);
        Assert.True(boss.IntroDialogue.Lines.Count >= 2);
    }

    [Fact]
    public void CindralithBossBattle_StartsFromAshbendMineGate()
    {
        var database = new ContentLoader(_contentRoot).Load();
        var boss = new BossLoader(Path.Combine(_contentRoot, "bosses"))
            .Load("cindralith.json", database.Species.Keys.ToHashSet());
        var profile = new PlayerProfile();

        var result = BossInteraction.TryTriggerGate(boss, profile, "ashbend-mine", 8, 2);

        Assert.Equal(BossGateTriggerResult.StartIntroDialogue, result);
    }

    [Fact]
    public void CindralithBossBattle_DisablesCapture()
    {
        var player = MakeSpecies("Pebblit", 58, 12, 12, 8).CreateInstance(17);
        var boss = new JoyMonSpecies(
            "Cindralith",
            JoyMonType.Ember,
            86,
            15,
            17,
            7,
            new[]
            {
                new MoveDefinition("ember-nudge", "Ember Nudge", JoyMonType.Ember, 35, 100, 25),
                new MoveDefinition("pebble-toss", "Pebble Toss", JoyMonType.Stone, 50, 90, 10),
            },
            "Ember/Stone").CreateInstance(17);

        var scene = ReadyForCommand(new BattleScene(player, boss, new DeterministicRng(0.0),
            isBossBattle: true, bossDisplayName: "Cindralith"));

        Assert.True(scene.IsBossBattle);
        Assert.False(scene.CanCapture);
        Assert.DoesNotContain("Capture", scene.Commands);
        Assert.Null(scene.TryCapture());
    }

    [Fact]
    public void CindralithClearFlag_PersistsInSave()
    {
        var database = new ContentLoader(_contentRoot).Load();
        var boss = new BossLoader(Path.Combine(_contentRoot, "bosses"))
            .Load("cindralith.json", database.Species.Keys.ToHashSet());
        var profile = new PlayerProfile();
        var player = new Player();
        player.Initialize(8, 2);

        BossInteraction.RecordVictory(profile, boss);

        var service = new SaveService(database, Path.Combine(_tempDir, "save.json"));
        service.Save(profile, player, "ashbend-mine");
        var json = service.Serialize(service.LoadSave());

        Assert.True(profile.HasFlag("ashbend_mine_cleared"));
        Assert.Contains("\"ashbend_mine_cleared\": true", json);
    }

    [Fact]
    public void AshbendMineShortcut_OpensAfterVictory()
    {
        var database = new ContentLoader(_contentRoot).Load();
        var boss = new BossLoader(Path.Combine(_contentRoot, "bosses"))
            .Load("cindralith.json", database.Species.Keys.ToHashSet());
        var map = new MapLoader(Path.Combine(_contentRoot, "maps")).Load("ashbend-mine.json");
        var profile = new PlayerProfile();

        Assert.False(IsWalkable(map, profile, 15, 1));

        BossInteraction.RecordVictory(profile, boss);

        Assert.True(IsWalkable(map, profile, 15, 1));
    }

    [Fact]
    public void AshbendCampDialogue_HasPostClearVariants()
    {
        var dialogue = new DialogueLoader(Path.Combine(_contentRoot, "dialogue")).Load("ashbend-camp.json");

        Assert.Contains(dialogue.Dialogues, d => d.Id == "ashbend-elder-talk");
        Assert.Contains(dialogue.Dialogues, d => d.Id == "ashbend-elder-talk-cleared");
        Assert.Contains(dialogue.Dialogues, d => d.Id == "ashbend-miner-1-talk-cleared");
    }

    private static bool IsWalkable(MapContent map, PlayerProfile profile, int x, int y) =>
        MapInteractionService.IsTileWalkable(map, profile, x, y);

    private static BattleScene ReadyForCommand(BattleScene scene)
    {
        while (scene.Mode == BattleSceneMode.Message)
            scene.Confirm();
        return scene;
    }

    private static JoyMonSpecies MakeSpecies(string name, int hp, int atk, int def, int spd)
    {
        var move = new MoveDefinition("test-hit", "Test Hit", JoyMonType.Neutral, 35, 100, 20);
        return new JoyMonSpecies(name, JoyMonType.Neutral, hp, atk, def, spd, new[] { move });
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
