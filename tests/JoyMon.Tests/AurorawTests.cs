using JoyMon.Content;
using JoyMon.Core;
using JoyMon.Game;
using JoyMon.Game.Services;

namespace JoyMon.Tests;

public class AurorawTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "JoyMonAurorawTest_" + Guid.NewGuid());
    private readonly string _contentRoot = FindContentRoot();

    public AurorawTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public void AurorawContent_ValidatesSuccessfully()
    {
        var database = new ContentLoader(_contentRoot).Load();
        var boss = new BossLoader(Path.Combine(_contentRoot, "bosses"))
            .Load("auroraw.json", database.Species.Keys.ToHashSet());

        Assert.True(database.Creatures.ContainsKey("auroraw"));
        Assert.True(database.Species.ContainsKey("auroraw"));

        var creature = database.Creatures["auroraw"];
        Assert.Equal("Auroraw", creature.Name);
        Assert.Equal("Frost", creature.Type);
        Assert.Equal("Echo", creature.SecondaryType);
        Assert.Equal(new[] { "echo-chirp", "guard-curl", "frost-nip", "tackle" }, creature.Learnset);

        Assert.Equal("auroraw", boss.Id);
        Assert.Equal("snowbell-shrine", boss.MapId);
        Assert.Equal(11, boss.GateTile.X);
        Assert.Equal(3, boss.GateTile.Y);
        Assert.Equal(22, boss.Level);
        Assert.Equal("snowbell_shrine_cleared", boss.ClearedFlag);
        Assert.True(boss.IntroDialogue.Lines.Count >= 2);
    }

    [Fact]
    public void FrostNipMove_ValidatesSuccessfully()
    {
        var database = new ContentLoader(_contentRoot).Load();
        Assert.True(database.MoveDefinitions.ContainsKey("frost-nip"));

        var move = database.MoveDefinitions["frost-nip"];
        Assert.Equal("Frost Nip", move.Name);
        Assert.Equal(JoyMonType.Frost, move.Type);
        Assert.Equal(9, move.Power);
        Assert.Equal(95, move.Accuracy);
        Assert.Equal(MoveEffects.Chill, move.Effect);
        Assert.Equal(20, move.EffectChance);
    }

    [Fact]
    public void Chill_ReducesSpeedAndExpires()
    {
        var frostNip = new MoveDefinition("frost-nip", "Frost Nip", JoyMonType.Frost, 9, 95, 20, MoveEffects.Chill, 100);
        var wait = new MoveDefinition("wait", "Wait", JoyMonType.Neutral, 0, 100, 20);

        var player = MakeSpecies("Player", 80, 10, 10, 10, frostNip, wait).CreateInstance(10);
        var opponent = MakeSpecies("Opponent", 80, 10, 10, 20, wait).CreateInstance(10);

        Assert.Equal(30, opponent.Speed);
        Assert.Equal(0, opponent.ChillTurnsRemaining);

        var state = new BattleState(player, opponent);
        var sys = new BattleSystem(new DeterministicRng(0.0));

        // Use Frost Nip to apply Chill
        sys.ExecuteTurn(state, new BattleCommand.Fight(0));

        Assert.True(opponent.ChillTurnsRemaining > 0);
        Assert.Equal(BattleStatus.ChillDurationTurns, opponent.ChillTurnsRemaining);
        Assert.Equal("CHL", opponent.BattleStatusLabel);

        // Verify speed reduced by 25% (30 * 0.75 = 22.5 -> 22)
        Assert.Equal(22, opponent.Speed);

        // Turn 2: use wait instead of Frost Nip to let status decay
        sys.ExecuteTurn(state, new BattleCommand.Fight(1));
        Assert.Equal(2, opponent.ChillTurnsRemaining);
        Assert.Equal(22, opponent.Speed);

        // Turn 3: use wait again
        sys.ExecuteTurn(state, new BattleCommand.Fight(1));
        Assert.Equal(1, opponent.ChillTurnsRemaining);
        Assert.Equal(22, opponent.Speed);

        // Turn 4: use wait again (expires after opponent acts)
        sys.ExecuteTurn(state, new BattleCommand.Fight(1));
        Assert.Equal(0, opponent.ChillTurnsRemaining);
        Assert.Equal(30, opponent.Speed);
        Assert.Null(opponent.BattleStatusLabel);
    }

    [Fact]
    public void AurorawBossBattle_StartsFromShrineGate()
    {
        var database = new ContentLoader(_contentRoot).Load();
        var boss = new BossLoader(Path.Combine(_contentRoot, "bosses"))
            .Load("auroraw.json", database.Species.Keys.ToHashSet());
        var profile = new PlayerProfile();

        var result = BossInteraction.TryTriggerGate(boss, profile, "snowbell-shrine", 11, 3);

        Assert.Equal(BossGateTriggerResult.StartIntroDialogue, result);
    }

    [Fact]
    public void AurorawBossBattle_DisablesCapture()
    {
        var player = MakeSpecies("Snobble", 60, 10, 10, 10).CreateInstance(22);
        var boss = MakeSpecies("Auroraw", 80, 15, 15, 15).CreateInstance(22);

        var scene = ReadyForCommand(new BattleScene(player, boss, new DeterministicRng(0.0),
            isBossBattle: true, bossDisplayName: "Auroraw"));

        Assert.True(scene.IsBossBattle);
        Assert.False(scene.CanCapture);
        Assert.DoesNotContain("Capture", scene.Commands);
        Assert.Null(scene.TryCapture());
    }

    [Fact]
    public void AurorawClearFlag_PersistsInSave()
    {
        var database = new ContentLoader(_contentRoot).Load();
        var boss = new BossLoader(Path.Combine(_contentRoot, "bosses"))
            .Load("auroraw.json", database.Species.Keys.ToHashSet());
        var profile = new PlayerProfile();
        var player = new Player();
        player.Initialize(11, 3);

        BossInteraction.RecordVictory(profile, boss);

        var service = new SaveService(database, Path.Combine(_tempDir, "save.json"));
        service.Save(profile, player, "snowbell-shrine");
        var json = service.Serialize(service.LoadSave());

        Assert.True(profile.HasFlag("snowbell_shrine_cleared"));
        Assert.Contains("\"snowbell_shrine_cleared\": true", json);
    }

    [Fact]
    public void MountainShortcut_OpensAfterVictory()
    {
        var database = new ContentLoader(_contentRoot).Load();
        var boss = new BossLoader(Path.Combine(_contentRoot, "bosses"))
            .Load("auroraw.json", database.Species.Keys.ToHashSet());
        var shrine = new MapLoader(Path.Combine(_contentRoot, "maps")).Load("snowbell-shrine.json");
        var lodge = new MapLoader(Path.Combine(_contentRoot, "maps")).Load("snowbell-lodge.json");
        var profile = new PlayerProfile();

        // Before victory, shortcuts are not walkable
        Assert.False(IsWalkable(shrine, profile, 11, 3));
        Assert.False(IsWalkable(lodge, profile, 14, 1));

        BossInteraction.RecordVictory(profile, boss);

        // After victory, shortcuts are walkable
        Assert.True(IsWalkable(shrine, profile, 11, 3));
        Assert.True(IsWalkable(lodge, profile, 14, 1));
    }

    private static bool IsWalkable(MapContent map, PlayerProfile profile, int x, int y) =>
        MapInteractionService.IsTileWalkable(map, profile, x, y);

    private static BattleScene ReadyForCommand(BattleScene scene)
    {
        while (scene.Mode == BattleSceneMode.Message)
            scene.Confirm();
        return scene;
    }

    private static JoyMonSpecies MakeSpecies(string name, int hp, int atk, int def, int spd, params MoveDefinition[] moves)
    {
        var defaultMoves = moves.Length > 0 ? moves : new[] { new MoveDefinition("test-hit", "Test Hit", JoyMonType.Neutral, 35, 100, 20) };
        return new JoyMonSpecies(name, JoyMonType.Neutral, hp, atk, def, spd, defaultMoves);
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
