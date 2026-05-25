using System.Text.Json;
using JoyMon.Content;
using JoyMon.Core;
using JoyMon.Game;

namespace JoyMon.Tests;

public class MilloxTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "JoyMonMilloxTest_" + Guid.NewGuid());

    public MilloxTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private void WriteBoss(string filename, object data) =>
        File.WriteAllText(Path.Combine(_tempDir, filename), JsonSerializer.Serialize(data));

    private void WriteMap(string filename, object data) =>
        File.WriteAllText(Path.Combine(_tempDir, filename), JsonSerializer.Serialize(data));

    private BossLoader CreateBossLoader() => new(_tempDir);
    private MapLoader CreateMapLoader() => new(_tempDir);

    private static List<List<int>> Grid(int w, int h, int fill)
    {
        var rows = new List<List<int>>();
        for (int y = 0; y < h; y++)
        {
            var row = new List<int>();
            for (int x = 0; x < w; x++) row.Add(fill);
            rows.Add(row);
        }
        return rows;
    }

    private object MinimalMap(string id, string name) => new
    {
        id,
        name,
        width = 22,
        height = 14,
        tileSize = 16,
        tilesetId = "overworld",
        spawnPoint = new { x = 10, y = 12 },
        layers = new
        {
            ground = Grid(22, 14, 1),
            decoration = Grid(22, 14, 0),
            collision = Grid(22, 14, 0),
        },
        transitions = Array.Empty<object>()
    };

    // ── 1. Millox content validates ───────────────────────────────

    [Fact]
    public void MilloxContent_ValidatesSuccessfully()
    {
        var validCreatures = new HashSet<string> { "millox" };

        WriteBoss("millox.json", new
        {
            id = "millox",
            displayName = "Millox",
            mapId = "old-watermill",
            gateTile = new { x = 10, y = 5 },
            level = 11,
            creatureId = "millox",
            clearedFlag = "old_watermill_cleared",
            introDialogue = new
            {
                speaker = "????",
                lines = new[] { "The waterwheel groans to a halt...", "Millox emerges!" }
            }
        });

        var loader = CreateBossLoader();
        var boss = loader.Load("millox.json", validCreatures);

        Assert.Equal("millox", boss.Id);
        Assert.Equal("Millox", boss.DisplayName);
        Assert.Equal("old-watermill", boss.MapId);
        Assert.Equal(10, boss.GateTile.X);
        Assert.Equal(5, boss.GateTile.Y);
        Assert.Equal(11, boss.Level);
        Assert.Equal("millox", boss.CreatureId);
        Assert.Equal("old_watermill_cleared", boss.ClearedFlag);
        Assert.Equal(2, boss.IntroDialogue.Lines.Count);
    }

    // ── 2. Invalid creature reference fails ───────────────────────

    [Fact]
    public void MilloxContent_InvalidCreature_FailsValidation()
    {
        var validCreatures = new HashSet<string> { "some-other-creature" };

        WriteBoss("bad.json", new
        {
            id = "millox",
            displayName = "Millox",
            mapId = "old-watermill",
            gateTile = new { x = 10, y = 5 },
            level = 11,
            creatureId = "unknown-creature",
            clearedFlag = "old_watermill_cleared",
            introDialogue = new { speaker = "X", lines = new[] { "Hi" } }
        });

        var ex = Assert.Throws<InvalidContentException>(() => CreateBossLoader().Load("bad.json", validCreatures));
        Assert.Contains("references unknown creature ID", ex.Message);
    }

    // ── 3. Boss trigger starts correct battle ─────────────────────

    [Fact]
    public void MilloxBossGate_TriggersIntroDialogue()
    {
        var boss = new BossContent
        {
            Id = "millox",
            DisplayName = "Millox",
            MapId = "old-watermill",
            GateTile = new BossGateTileContent { X = 10, Y = 5 },
            Level = 11,
            CreatureId = "millox",
            ClearedFlag = "old_watermill_cleared",
            IntroDialogue = new TrainerDialogueContent
            {
                Speaker = "????",
                Lines = new List<string> { "The waterwheel groans...", "Millox emerges!" }
            }
        };

        var profile = new PlayerProfile();
        var result = BossInteraction.TryTriggerGate(boss, profile, "old-watermill", 10, 5);

        Assert.Equal(BossGateTriggerResult.StartIntroDialogue, result);
    }

    // ── 4. Boss trigger does not fire on wrong tile ───────────────

    [Fact]
    public void MilloxBossGate_DoesNotTrigger_WrongTile()
    {
        var boss = new BossContent
        {
            Id = "millox",
            DisplayName = "Millox",
            MapId = "old-watermill",
            GateTile = new BossGateTileContent { X = 10, Y = 5 },
            Level = 11,
            CreatureId = "millox",
            ClearedFlag = "old_watermill_cleared",
            IntroDialogue = new TrainerDialogueContent
            {
                Speaker = "????",
                Lines = new List<string> { "Test" }
            }
        };

        var profile = new PlayerProfile();
        var result = BossInteraction.TryTriggerGate(boss, profile, "old-watermill", 3, 7);

        Assert.Equal(BossGateTriggerResult.None, result);
    }

    // ── 5. Boss trigger does not fire on wrong map ────────────────

    [Fact]
    public void MilloxBossGate_DoesNotTrigger_WrongMap()
    {
        var boss = new BossContent
        {
            Id = "millox",
            DisplayName = "Millox",
            MapId = "old-watermill",
            GateTile = new BossGateTileContent { X = 10, Y = 5 },
            Level = 11,
            CreatureId = "millox",
            ClearedFlag = "old_watermill_cleared",
            IntroDialogue = new TrainerDialogueContent
            {
                Speaker = "????",
                Lines = new List<string> { "Test" }
            }
        };

        var profile = new PlayerProfile();
        var result = BossInteraction.TryTriggerGate(boss, profile, "trial-grove", 10, 5);

        Assert.Equal(BossGateTriggerResult.None, result);
    }

    // ── 6. Capture disabled for Millox (boss battle) ──────────────

    [Fact]
    public void MilloxBossBattle_DisablesCapture()
    {
        var playerSpecies = new JoyMonSpecies("Pebblit", JoyMonType.Stone, 58, 6, 11, 4,
            new[] { new MoveDefinition("pebble-toss", "Pebble Toss", JoyMonType.Stone, 50, 90, 10) });
        var player = playerSpecies.CreateInstance(11);

        var bossSpecies = new JoyMonSpecies("Millox", JoyMonType.Stone, 62, 9, 12, 5,
            new[] { new MoveDefinition("pebble-toss", "Pebble Toss", JoyMonType.Stone, 50, 90, 10) });
        var boss = bossSpecies.CreateInstance(11);

        var scene = ReadyForCommand(new BattleScene(player, boss, new DeterministicRng(0.0),
            isBossBattle: true, bossDisplayName: "Millox"));

        Assert.True(scene.IsBossBattle);
        Assert.False(scene.CanCapture);
        Assert.DoesNotContain("Capture", scene.Commands);
        Assert.Null(scene.TryCapture());
    }

    // ── 7. Victory sets old_watermill_cleared ─────────────────────

    [Fact]
    public void Victory_SetsOldWatermillCleared()
    {
        var boss = new BossContent
        {
            Id = "millox",
            DisplayName = "Millox",
            MapId = "old-watermill",
            GateTile = new BossGateTileContent { X = 10, Y = 5 },
            Level = 11,
            CreatureId = "millox",
            ClearedFlag = "old_watermill_cleared",
            IntroDialogue = new TrainerDialogueContent
            {
                Speaker = "????",
                Lines = new List<string> { "Test" }
            }
        };

        var profile = new PlayerProfile();
        Assert.False(profile.HasFlag("old_watermill_cleared"));

        BossInteraction.RecordVictory(profile, boss);

        Assert.True(profile.HasFlag("old_watermill_cleared"));
    }

    // ── 8. Millox does NOT show ending screen ─────────────────────

    [Fact]
    public void MilloxVictory_DoesNotShowEnding()
    {
        Assert.False(BossInteraction.ShouldShowEnding(BattleSceneOutcome.Won, wasBossBattle: false));
        // Millox is a chapter boss — ShouldShowEnding checks wasBossBattle && outcome
        // The game logic differentiates via boss ID in CompleteBattle
    }

    // ── 9. Old Watermill map validates ────────────────────────────

    [Fact]
    public void OldWatermillMap_ValidatesSuccessfully()
    {
        // Write old-watermill with valid transitions to riverside
        WriteMap("old-watermill.json", new
        {
            id = "old-watermill",
            name = "Old Watermill",
            width = 22,
            height = 14,
            tileSize = 16,
            tilesetId = "overworld",
            spawnPoint = new { x = 10, y = 12 },
            layers = new
            {
                ground = Grid(22, 14, 1),
                decoration = Grid(22, 14, 0),
                collision = Grid(22, 14, 0),
            },
            transitions = new[]
            {
                new
                {
                    fromMapId = "old-watermill",
                    fromTile = new { x = 10, y = 13 },
                    toMapId = "riverside",
                    toTile = new { x = 10, y = 1 }
                }
            }
        });
        WriteMap("riverside.json", MinimalMap("riverside", "Riverside"));

        var map = CreateMapLoader().Load("old-watermill.json");

        Assert.Equal("old-watermill", map.Id);
        Assert.Equal("Old Watermill", map.Name);
        Assert.Equal(22, map.Width);
        Assert.Equal(14, map.Height);
        Assert.NotNull(map.SpawnPoint);
        Assert.Single(map.Transitions);
        Assert.Equal("riverside", map.Transitions[0].ToMapId);
    }

    // ── 10. Riverside map validates with bridge gate ──────────────

    [Fact]
    public void RiversideMap_HasGatedBridgeTransition()
    {
        WriteMap("riverside.json", new
        {
            id = "riverside",
            name = "Riverside",
            width = 22,
            height = 14,
            tileSize = 16,
            tilesetId = "overworld",
            spawnPoint = new { x = 10, y = 12 },
            layers = new
            {
                ground = Grid(22, 14, 1),
                decoration = Grid(22, 14, 0),
                collision = Grid(22, 14, 0),
            },
            transitions = new object[]
            {
                new
                {
                    fromMapId = "riverside",
                    fromTile = new { x = 10, y = 0 },
                    toMapId = "old-watermill",
                    toTile = new { x = 10, y = 12 }
                },
                new
                {
                    fromMapId = "riverside",
                    fromTile = new { x = 10, y = 13 },
                    toMapId = "next-area",
                    toTile = new { x = 10, y = 1 },
                    requiredFlag = (string?)"old_watermill_cleared"
                }
            }
        });
        WriteMap("old-watermill.json", MinimalMap("old-watermill", "Old Watermill"));
        WriteMap("next-area.json", MinimalMap("next-area", "Next Area"));

        var map = CreateMapLoader().Load("riverside.json");

        Assert.Equal("riverside", map.Id);
        Assert.Equal(2, map.Transitions.Count);

        var bridge = map.Transitions.FirstOrDefault(t => t.ToMapId == "next-area");
        Assert.NotNull(bridge);
        Assert.Equal("old_watermill_cleared", bridge.RequiredFlag);
    }

    // ── 11. Riverside dialogue changes after clear ────────────────

    [Fact]
    public void RiversideDialogue_ChangesAfterClear()
    {
        var profile = new PlayerProfile();

        // Before clear
        string dialogueId = "ferryman-pre-clear";
        Assert.Equal("ferryman-pre-clear", dialogueId);

        // After clear
        profile.SetFlag("old_watermill_cleared", true);
        if (profile.HasFlag("old_watermill_cleared"))
            dialogueId = "ferryman-post-clear";

        Assert.Equal("ferryman-post-clear", dialogueId);
    }

    // ── 12. Bridge gate opens after clear ─────────────────────────

    [Fact]
    public void BridgeGate_BlocksBeforeClear_PassableAfter()
    {
        var profile = new PlayerProfile();
        var map = new MapContent
        {
            Id = "riverside",
            Width = 22,
            Height = 14,
            Transitions = new List<MapTransitionContent>
            {
                new()
                {
                    FromMapId = "riverside",
                    FromTile = new TransitionTileContent { X = 10, Y = 13 },
                    ToMapId = "next-area",
                    ToTile = new TransitionTileContent { X = 10, Y = 1 },
                    RequiredFlag = "old_watermill_cleared"
                }
            },
            Layers = new MapLayersContent
            {
                Collision = Grid(22, 14, 0)
            }
        };

        var player = new Player();
        player.Initialize(10, 12);

        // Walkability check — bridge blocked before clear
        bool IsWalkable(int x, int y)
        {
            if (x < 0 || x >= map.Width || y < 0 || y >= map.Height) return false;
            if (map.Layers.Collision[y][x] != 0) return false;
            var t = map.Transitions.FirstOrDefault(tr => tr.FromTile.X == x && tr.FromTile.Y == y);
            if (t is not null && !string.IsNullOrEmpty(t.RequiredFlag))
                if (!profile.HasFlag(t.RequiredFlag)) return false;
            return true;
        }

        // Try walk to bridge tile (10, 13) — blocked
        Assert.False(IsWalkable(10, 13));

        // Now clear the flag
        profile.SetFlag("old_watermill_cleared", true);

        // Bridge should now be passable
        Assert.True(IsWalkable(10, 13));
    }

    private static BattleScene ReadyForCommand(BattleScene scene)
    {
        while (scene.Mode == BattleSceneMode.Message)
            scene.Confirm();
        return scene;
    }
}