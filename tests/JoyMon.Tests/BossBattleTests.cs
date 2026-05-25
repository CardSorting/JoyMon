using System.Text.Json;
using JoyMon.Content;
using JoyMon.Core;
using JoyMon.Game;
using JoyMon.Game.Services;

namespace JoyMon.Tests;

public class BossBattleTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "JoyMonBossTest_" + Guid.NewGuid());
    private readonly HashSet<string> _validCreatures = new() { "lanternox", "queuebee" };

    public BossBattleTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private void WriteBoss(string filename, object data) =>
        File.WriteAllText(Path.Combine(_tempDir, filename), JsonSerializer.Serialize(data));

    private BossLoader CreateLoader() => new(_tempDir);

    private static object ValidBoss() => new
    {
        id = "lanternox",
        displayName = "Lanternox",
        mapId = "trial-grove",
        gateTile = new { x = 10, y = 1 },
        level = 6,
        creatureId = "lanternox",
        clearedFlag = "trial_grove_cleared",
        introDialogue = new
        {
            speaker = "Lanternox",
            lines = new[] { "The gate stirs...", "Prepare yourself!" }
        }
    };

    [Fact]
    public void ValidBoss_LoadsSuccessfully()
    {
        WriteBoss("valid.json", ValidBoss());
        var boss = CreateLoader().Load("valid.json", _validCreatures);

        Assert.Equal("lanternox", boss.Id);
        Assert.Equal("Lanternox", boss.DisplayName);
        Assert.Equal("trial-grove", boss.MapId);
        Assert.Equal(10, boss.GateTile.X);
        Assert.Equal(1, boss.GateTile.Y);
        Assert.Equal(6, boss.Level);
        Assert.Equal("lanternox", boss.CreatureId);
        Assert.Equal("trial_grove_cleared", boss.ClearedFlag);
    }

    [Fact]
    public void InvalidCreatureReference_FailsValidation()
    {
        WriteBoss("invalid.json", new
        {
            id = "bad-boss",
            displayName = "Bad",
            mapId = "trial-grove",
            gateTile = new { x = 1, y = 1 },
            level = 6,
            creatureId = "unknown",
            clearedFlag = "trial_grove_cleared",
            introDialogue = new { speaker = "X", lines = new[] { "Hi" } }
        });

        var ex = Assert.Throws<InvalidContentException>(() => CreateLoader().Load("invalid.json", _validCreatures));
        Assert.Contains("references unknown creature ID", ex.Message);
    }

    [Fact]
    public void BossGate_TriggersIntroDialogue()
    {
        var boss = CreateBossFromContent();
        var profile = new PlayerProfile();

        var result = BossInteraction.TryTriggerGate(boss, profile, "trial-grove", 10, 1);

        Assert.Equal(BossGateTriggerResult.StartIntroDialogue, result);
    }

    [Fact]
    public void BossGate_DoesNotTriggerWhenCleared()
    {
        var boss = CreateBossFromContent();
        var profile = new PlayerProfile();
        profile.SetFlag("trial_grove_cleared", true);

        var result = BossInteraction.TryTriggerGate(boss, profile, "trial-grove", 10, 1);

        Assert.Equal(BossGateTriggerResult.None, result);
    }

    [Fact]
    public void BossBattle_DisablesCapture()
    {
        var player = MakeSpecies("Mossprout", 30, 20, 8, 20).CreateInstance(6);
        var boss = MakeSpecies("Lanternox", 58, 15, 12, 9).CreateInstance(6);
        var scene = ReadyForCommand(new BattleScene(player, boss, new DeterministicRng(0.0), isBossBattle: true, bossDisplayName: "Lanternox"));

        Assert.True(scene.IsBossBattle);
        Assert.False(scene.CanCapture);
        Assert.DoesNotContain("Capture", scene.Commands);
        Assert.Null(scene.TryCapture());
    }

    [Fact]
    public void Victory_SetsClearedFlag()
    {
        var boss = CreateBossFromContent();
        var profile = new PlayerProfile();

        BossInteraction.RecordVictory(profile, boss);

        Assert.True(profile.HasFlag("trial_grove_cleared"));
    }

    [Fact]
    public void Victory_ShowsEndingScreen()
    {
        Assert.True(BossInteraction.ShouldShowEnding(BattleSceneOutcome.Won, wasBossBattle: true));
        Assert.False(BossInteraction.ShouldShowEnding(BattleSceneOutcome.Lost, wasBossBattle: true));
        Assert.False(BossInteraction.ShouldShowEnding(BattleSceneOutcome.Won, wasBossBattle: false));
    }

    [Fact]
    public void Save_IncludesClearedFlagAndCaptures()
    {
        var profile = new PlayerProfile();
        profile.SetFlag("trial_grove_cleared", true);
        profile.RecordCapture("queuebee");
        profile.PlayTimeSeconds = 125;
        profile.Party.Add(MakeSpecies("Mossprout", 30, 9, 8, 12).CreateInstance(5));

        var content = CreateContentDatabase();
        var service = new SaveService(content, Path.Combine(_tempDir, "save.json"));
        var player = new Player();
        player.Initialize(4, 7);

        service.Save(profile, player, "trial-grove");
        var json = service.Serialize(service.LoadSave());

        Assert.Contains("\"trial_grove_cleared\": true", json);
        Assert.Contains("\"queuebee\"", json);
        Assert.Contains("\"playTimeSeconds\": 125", json);
    }

    private static BossContent CreateBossFromContent()
    {
        return new BossContent
        {
            Id = "lanternox",
            DisplayName = "Lanternox",
            MapId = "trial-grove",
            GateTile = new BossGateTileContent { X = 10, Y = 1 },
            Level = 6,
            CreatureId = "lanternox",
            ClearedFlag = "trial_grove_cleared",
            IntroDialogue = new TrainerDialogueContent
            {
                Speaker = "Lanternox",
                Lines = new List<string> { "Prepare yourself!" },
            },
        };
    }

    private static ContentDatabase CreateContentDatabase()
    {
        var tackle = new MoveDefinition("tackle", "Tackle", JoyMonType.Neutral, 20, 95, 25);
        var mossprout = new JoyMonSpecies("Mossprout", JoyMonType.Moss, 48, 8, 9, 6, new[] { tackle });
        return new ContentDatabase(
            new Dictionary<string, CreatureContent>(),
            new Dictionary<string, MoveContent>(),
            new Dictionary<string, JoyMonSpecies> { ["mossprout"] = mossprout },
            new Dictionary<string, MoveDefinition> { ["tackle"] = tackle });
    }

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
}
