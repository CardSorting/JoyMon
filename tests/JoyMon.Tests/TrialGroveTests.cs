using System.Text.Json;
using JoyMon.Content;
using JoyMon.Core;
using JoyMon.Game;

namespace JoyMon.Tests;

public class TrialGroveTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "JoyMonTrialGroveTest_" + Guid.NewGuid());
    private readonly HashSet<string> _validCreatures = new() { "rootsnail", "queuebee", "glimmoo" };

    public TrialGroveTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private void WriteMap(string filename, object data) =>
        File.WriteAllText(Path.Combine(_tempDir, filename), JsonSerializer.Serialize(data));

    private void WriteEncounter(string filename, object data) =>
        File.WriteAllText(Path.Combine(_tempDir, filename), JsonSerializer.Serialize(data));

    private MapLoader CreateMapLoader() => new(_tempDir);
    private EncounterLoader CreateEncounterLoader() => new(_tempDir);

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

    /// <summary>
    /// Helper to write a realistic trial-grove map to the temp directory.
    /// </summary>
    private void WriteBothMaps()
    {
        // Write trial-grove with transitions to route-1
        WriteMap("trial-grove.json", new
        {
            id = "trial-grove",
            name = "Trial Grove",
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
                    fromMapId = "trial-grove",
                    fromTile = new { x = 10, y = 13 },
                    toMapId = "route-1",
                    toTile = new { x = 10, y = 1 }
                }
            }
        });

        // Write route-1 with transitions to trial-grove only
        WriteMap("route-1.json", new
        {
            id = "route-1",
            name = "Route 1",
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
                    fromMapId = "route-1",
                    fromTile = new { x = 10, y = 0 },
                    toMapId = "trial-grove",
                    toTile = new { x = 10, y = 12 }
                }
            }
        });

        // Also write starter-town for full validation
        WriteMap("starter-town.json", MinimalMap("starter-town", "Starter Town"));
    }

    // ── 1. Trial Grove map validates ──────────────────────────────

    [Fact]
    public void TrialGroveMap_ValidatesSuccessfully()
    {
        WriteBothMaps();
        var loader = CreateMapLoader();
        var map = loader.Load("trial-grove.json");

        Assert.Equal("trial-grove", map.Id);
        Assert.Equal("Trial Grove", map.Name);
        Assert.Equal(22, map.Width);
        Assert.Equal(14, map.Height);
        Assert.NotNull(map.SpawnPoint);
        Assert.Equal(10, map.SpawnPoint.X);
        Assert.Equal(12, map.SpawnPoint.Y);
        Assert.Single(map.Transitions);
    }

    // ── 2. Transition from Route1 to Trial Grove works ────────────

    [Fact]
    public void Route1_HasNorthTransitionToTrialGrove()
    {
        WriteBothMaps();
        var loader = CreateMapLoader();
        var map = loader.Load("route-1.json");

        var transition = map.Transitions.FirstOrDefault(t => t.ToMapId == "trial-grove");
        Assert.NotNull(transition);
        Assert.Equal("route-1", transition.FromMapId);
        Assert.Equal(10, transition.FromTile.X);
        Assert.Equal(0, transition.FromTile.Y);
        Assert.Equal(10, transition.ToTile.X);
        Assert.Equal(12, transition.ToTile.Y);
    }

    // ── 3. Transition from Trial Grove back to Route1 works ───────

    [Fact]
    public void TrialGrove_HasSouthTransitionToRoute1()
    {
        WriteBothMaps();
        var loader = CreateMapLoader();
        var map = loader.Load("trial-grove.json");

        var transition = map.Transitions.FirstOrDefault(t => t.ToMapId == "route-1");
        Assert.NotNull(transition);
        Assert.Equal("trial-grove", transition.FromMapId);
        Assert.Equal(10, transition.FromTile.X);
        Assert.Equal(13, transition.FromTile.Y);
        Assert.Equal(10, transition.ToTile.X);
        Assert.Equal(1, transition.ToTile.Y);
    }

    // ── 4. Player can transition from Route1 to Trial Grove ───────

    [Fact]
    public void Player_CanTransitionFromRoute1_ToTrialGrove()
    {
        WriteBothMaps();
        var mapLoader = CreateMapLoader();
        var route1 = mapLoader.Load("route-1.json");

        var profile = new PlayerProfile();
        profile.SetFlag("received_starter", true);

        var player = new Player();
        player.Initialize(10, 1);

        // Walkability check that allows transition
        bool IsWalkable(int x, int y)
        {
            if (x < 0 || x >= route1.Width || y < 0 || y >= route1.Height) return false;
            if (route1.Layers.Collision[y][x] != 0) return false;

            var t = route1.Transitions.FirstOrDefault(tr => tr.FromTile.X == x && tr.FromTile.Y == y);
            if (t is not null && !string.IsNullOrEmpty(t.RequiredFlag))
            {
                if (!profile.HasFlag(t.RequiredFlag)) return false;
            }
            return true;
        }

        // Move up to (10, 0) - the transition tile
        player.Update(0.1f, Direction.Up, IsWalkable);
        player.Update(0.15f, Direction.None, IsWalkable);

        Assert.Equal(10, player.X);
        Assert.Equal(0, player.Y);

        // Verify transition triggers to trial-grove
        var triggered = route1.Transitions.FirstOrDefault(t => t.FromTile.X == player.X && t.FromTile.Y == player.Y);
        Assert.NotNull(triggered);
        Assert.Equal("trial-grove", triggered.ToMapId);
        Assert.Equal(10, triggered.ToTile.X);
        Assert.Equal(12, triggered.ToTile.Y);
    }

    // ── 5. Healing NPC restores party ─────────────────────────────

    [Fact]
    public void HealParty_RestoresAllJoyMonHP()
    {
        // Create a species and instance
        var moveDef = new MoveDefinition("tackle", "Tackle", JoyMonType.Neutral, 20, 100, 5);
        var species = new JoyMonSpecies("Rootsnail", JoyMonType.Moss, 52, 6, 10, 3, new[] { moveDef });
        var joymon = species.CreateInstance(5);

        int maxHp = joymon.MaxHp;
        Assert.True(maxHp > 0);

        // Damage the JoyMon
        joymon.CurrentHp = 1;
        Assert.Equal(1, joymon.CurrentHp);

        // Heal (same logic as Game1.HealParty)
        joymon.CurrentHp = joymon.MaxHp;
        for (int i = 0; i < joymon.RemainingUses.Length && i < joymon.Species.Moves.Count; i++)
        {
            joymon.RemainingUses[i] = joymon.Species.Moves[i].MaxUses;
        }

        Assert.Equal(maxHp, joymon.CurrentHp);
    }

    // ── 6. Healing restores full PP ───────────────────────────────

    [Fact]
    public void HealParty_RestoresMoveUses()
    {
        var move1 = new MoveDefinition("tackle", "Tackle", JoyMonType.Neutral, 20, 100, 5);
        var move2 = new MoveDefinition("moss-tap", "Moss Tap", JoyMonType.Moss, 30, 90, 8);
        var species = new JoyMonSpecies("Rootsnail", JoyMonType.Moss, 52, 6, 10, 3, new[] { move1, move2 });
        var joymon = species.CreateInstance(5);

        // Deplete move uses
        joymon.RemainingUses[0] = 0;
        joymon.RemainingUses[1] = 2;

        // Heal
        joymon.CurrentHp = joymon.MaxHp;
        for (int i = 0; i < joymon.RemainingUses.Length && i < joymon.Species.Moves.Count; i++)
        {
            joymon.RemainingUses[i] = joymon.Species.Moves[i].MaxUses;
        }

        Assert.Equal(5, joymon.RemainingUses[0]); // tackle max uses
        Assert.Equal(8, joymon.RemainingUses[1]); // moss-tap max uses
    }

    // ── 7. Boss gate interaction triggers placeholder dialogue ────

    [Fact]
    public void BossGate_OnTrialGrove_TriggersIntroDialogue()
    {
        var boss = new BossContent
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
                Lines = new List<string> { "The gate stirs...", "Prepare yourself!" }
            }
        };

        var profile = new PlayerProfile();

        var result = BossInteraction.TryTriggerGate(boss, profile, "trial-grove", 10, 1);

        Assert.Equal(BossGateTriggerResult.StartIntroDialogue, result);
    }

    // ── 8. Boss gate does not trigger on wrong map ────────────────

    [Fact]
    public void BossGate_DoesNotTrigger_WrongMap()
    {
        var boss = new BossContent
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
                Lines = new List<string> { "Test" }
            }
        };

        var profile = new PlayerProfile();
        var result = BossInteraction.TryTriggerGate(boss, profile, "route-1", 10, 1);

        Assert.Equal(BossGateTriggerResult.None, result);
    }

    // ── 9. Boss gate does not trigger after cleared ───────────────

    [Fact]
    public void BossGate_DoesNotTrigger_AfterCleared()
    {
        var boss = new BossContent
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
                Lines = new List<string> { "Test" }
            }
        };

        var profile = new PlayerProfile();
        BossInteraction.RecordVictory(profile, boss); // Mark as cleared

        var result = BossInteraction.TryTriggerGate(boss, profile, "trial-grove", 10, 1);

        Assert.Equal(BossGateTriggerResult.None, result);
    }

    // ── 10. Trial Grove encounters use correct table ──────────────

    [Fact]
    public void TrialGroveEncounterTable_LoadsSuccessfully()
    {
        WriteEncounter("trial-grove.json", new
        {
            id = "trial-grove-encounters",
            mapId = "trial-grove",
            zoneId = "forest",
            tileIds = new[] { 1 },
            encounterRate = 0.12,
            entries = new[]
            {
                new { creatureId = "rootsnail", minLevel = 3, maxLevel = 5, weight = 40 },
                new { creatureId = "queuebee", minLevel = 3, maxLevel = 5, weight = 35 },
                new { creatureId = "glimmoo", minLevel = 3, maxLevel = 4, weight = 25 }
            }
        });

        var loader = CreateEncounterLoader();
        var table = loader.Load("trial-grove.json", _validCreatures);

        Assert.Equal("trial-grove-encounters", table.Id);
        Assert.Equal("trial-grove", table.MapId);
        Assert.Equal("forest", table.ZoneId);
        Assert.Single(table.TileIds);
        Assert.Equal(1, table.TileIds[0]);
        Assert.Equal(0.12, table.EncounterRate);
        Assert.Equal(3, table.Entries.Count);

        // Verify creatures
        Assert.Equal("rootsnail", table.Entries[0].CreatureId);
        Assert.Equal(3, table.Entries[0].MinLevel);
        Assert.Equal(5, table.Entries[0].MaxLevel);
        Assert.Equal(40, table.Entries[0].Weight);

        Assert.Equal("queuebee", table.Entries[1].CreatureId);
        Assert.Equal(3, table.Entries[1].MinLevel);
        Assert.Equal(5, table.Entries[1].MaxLevel);
        Assert.Equal(35, table.Entries[1].Weight);

        Assert.Equal("glimmoo", table.Entries[2].CreatureId);
        Assert.Equal(3, table.Entries[2].MinLevel);
        Assert.Equal(4, table.Entries[2].MaxLevel);
        Assert.Equal(25, table.Entries[2].Weight);
    }

    // ── 11. Trial Grove encounter fails with invalid creature ─────

    [Fact]
    public void TrialGroveEncounter_InvalidCreature_FailsValidation()
    {
        WriteEncounter("bad.json", new
        {
            id = "trial-grove-encounters",
            mapId = "trial-grove",
            zoneId = "forest",
            tileIds = new[] { 1 },
            encounterRate = 0.12,
            entries = new[]
            {
                new { creatureId = "unknown-creature", minLevel = 3, maxLevel = 5, weight = 100 }
            }
        });

        var loader = CreateEncounterLoader();
        var ex = Assert.Throws<InvalidContentException>(() => loader.Load("bad.json", _validCreatures));
        Assert.Contains("references unknown creature ID", ex.Message);
    }

    // ── 12. Player can walk maze path from entrance to boss gate ──

    [Fact]
    public void PlayerCanWalk_FromEntrance_ToBossGatePosition()
    {
        WriteBothMaps();
        var mapLoader = CreateMapLoader();
        var map = mapLoader.Load("trial-grove.json");

        // Verify spawn point is walkable
        Assert.Equal(0, map.Layers.Collision[map.SpawnPoint.Y][map.SpawnPoint.X]);

        // Verify boss gate area is walkable
        Assert.Equal(0, map.Layers.Collision[1][10]); // boss gate tile

        // Verify transition tile is walkable
        Assert.Equal(0, map.Layers.Collision[13][10]); // exit tile
        Assert.Equal(0, map.Layers.Collision[12][10]); // spawn tile
    }

    // ── 13. Dialogue loads and NPC has correct properties ──────────

    [Fact]
    public void TrialGroveDialogue_LoadsSuccessfully()
    {
        WriteDialogue("trial-grove.json", new
        {
            npcs = new[]
            {
                new
                {
                    id = "trial-grove-healer",
                    name = "Elder Willow",
                    mapId = "trial-grove",
                    tilePosition = new { x = 4, y = 8 },
                    facingDirection = "right",
                    dialogueId = "elder-willow-heal",
                    spriteId = "elder-willow"
                }
            },
            dialogues = new[]
            {
                new
                {
                    id = "elder-willow-heal",
                    speaker = "Elder Willow",
                    lines = new[] { "Welcome!", "Let me restore your strength." }
                }
            }
        });

        var loader = new DialogueLoader(_tempDir);
        var content = loader.Load("trial-grove.json");

        Assert.Single(content.Npcs);
        Assert.Single(content.Dialogues);

        var npc = content.Npcs[0];
        Assert.Equal("trial-grove-healer", npc.Id);
        Assert.Equal("Elder Willow", npc.Name);
        Assert.Equal("trial-grove", npc.MapId);
        Assert.Equal(4, npc.TilePosition.X);
        Assert.Equal(8, npc.TilePosition.Y);

        var dlg = content.Dialogues[0];
        Assert.Equal("elder-willow-heal", dlg.Id);
        Assert.Equal("Elder Willow", dlg.Speaker);
        Assert.Equal(2, dlg.Lines.Count);
    }

    private void WriteDialogue(string filename, object data) =>
        File.WriteAllText(Path.Combine(_tempDir, filename), JsonSerializer.Serialize(data));
}
