using System.Text.Json;
using JoyMon.Content;
using JoyMon.Core;

namespace JoyMon.Tests;

public class RegionExpansionTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "JoyMonRegionTest_" + Guid.NewGuid());
    private readonly HashSet<string> _validCreatures;

    public RegionExpansionTests()
    {
        Directory.CreateDirectory(_tempDir);
        _validCreatures = new HashSet<string>
        {
            "glimmoo", "queuebee", "pebblit", "drizzleaf",
            "rootsnail", "cindermite",
            "lanternox", "mossprout", "staticrow", "murmurl"
        };
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
    /// Writes all maps needed for the region chain: starter-town, route-1, trial-grove,
    /// riverside-village, mill-road, old-watermill.
    /// </summary>
    private void WriteAllMaps()
    {
        WriteMap("starter-town.json", MinimalMap("starter-town", "Starter Town"));

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
            transitions = new object[]
            {
                new
                {
                    fromMapId = "trial-grove",
                    fromTile = new { x = 10, y = 13 },
                    toMapId = "route-1",
                    toTile = new { x = 10, y = 1 },
                    requiredFlag = default(string)
                },
                new
                {
                    fromMapId = "trial-grove",
                    fromTile = new { x = 10, y = 0 },
                    toMapId = "riverside-village",
                    toTile = new { x = 10, y = 12 },
                    requiredFlag = "trial_grove_cleared"
                }
            }
        });

        WriteMap("riverside-village.json", new
        {
            id = "riverside-village",
            name = "Riverside Village",
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
                    fromMapId = "riverside-village",
                    fromTile = new { x = 10, y = 0 },
                    toMapId = "mill-road",
                    toTile = new { x = 10, y = 12 }
                },
                new
                {
                    fromMapId = "riverside-village",
                    fromTile = new { x = 10, y = 13 },
                    toMapId = "trial-grove",
                    toTile = new { x = 10, y = 0 }
                }
            }
        });

        WriteMap("mill-road.json", new
        {
            id = "mill-road",
            name = "Mill Road",
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
                    fromMapId = "mill-road",
                    fromTile = new { x = 10, y = 13 },
                    toMapId = "riverside-village",
                    toTile = new { x = 10, y = 0 }
                },
                new
                {
                    fromMapId = "mill-road",
                    fromTile = new { x = 20, y = 7 },
                    toMapId = "old-watermill",
                    toTile = new { x = 0, y = 7 }
                }
            }
        });

        WriteMap("old-watermill.json", new
        {
            id = "old-watermill",
            name = "Old Watermill",
            width = 22,
            height = 14,
            tileSize = 16,
            tilesetId = "overworld",
            spawnPoint = new { x = 1, y = 7 },
            layers = new
            {
                ground = Grid(22, 14, 2),
                decoration = Grid(22, 14, 0),
                collision = Grid(22, 14, 0),
            },
            transitions = new[]
            {
                new
                {
                    fromMapId = "old-watermill",
                    fromTile = new { x = 0, y = 7 },
                    toMapId = "mill-road",
                    toTile = new { x = 19, y = 7 }
                }
            }
        });
    }

    // ── 1. New maps validate ──────────────────────────────────────

    [Fact]
    public void RiversideVillageMap_ValidatesSuccessfully()
    {
        WriteAllMaps();
        var loader = CreateMapLoader();
        var map = loader.Load("riverside-village.json");

        Assert.Equal("riverside-village", map.Id);
        Assert.Equal("Riverside Village", map.Name);
        Assert.Equal(22, map.Width);
        Assert.Equal(14, map.Height);
        Assert.NotNull(map.SpawnPoint);
        Assert.Equal(10, map.SpawnPoint.X);
        Assert.Equal(12, map.SpawnPoint.Y);
        Assert.Equal(2, map.Transitions.Count);
    }

    [Fact]
    public void MillRoadMap_ValidatesSuccessfully()
    {
        WriteAllMaps();
        var loader = CreateMapLoader();
        var map = loader.Load("mill-road.json");

        Assert.Equal("mill-road", map.Id);
        Assert.Equal("Mill Road", map.Name);
        Assert.Equal(2, map.Transitions.Count);
    }

    [Fact]
    public void OldWatermillMap_ValidatesSuccessfully()
    {
        WriteAllMaps();
        var loader = CreateMapLoader();
        var map = loader.Load("old-watermill.json");

        Assert.Equal("old-watermill", map.Id);
        Assert.Equal("Old Watermill", map.Name);
        Assert.Single(map.Transitions);
        Assert.NotNull(map.SpawnPoint);
        Assert.Equal(1, map.SpawnPoint.X);
        Assert.Equal(7, map.SpawnPoint.Y);
    }

    // ── 2. New transitions validate ──────────────────────────────

    [Fact]
    public void TrialGrove_HasNorthTransitionToRiverside()
    {
        WriteAllMaps();
        var loader = CreateMapLoader();
        var map = loader.Load("trial-grove.json");

        var transition = map.Transitions.FirstOrDefault(t => t.ToMapId == "riverside-village");
        Assert.NotNull(transition);
        Assert.Equal("trial-grove", transition.FromMapId);
        Assert.Equal(10, transition.FromTile.X);
        Assert.Equal(0, transition.FromTile.Y);
        Assert.Equal(10, transition.ToTile.X);
        Assert.Equal(12, transition.ToTile.Y);
        Assert.Equal("trial_grove_cleared", transition.RequiredFlag);
    }

    [Fact]
    public void Riverside_HasNorthTransitionToMillRoad()
    {
        WriteAllMaps();
        var loader = CreateMapLoader();
        var map = loader.Load("riverside-village.json");

        var transition = map.Transitions.FirstOrDefault(t => t.ToMapId == "mill-road");
        Assert.NotNull(transition);
        Assert.Equal("riverside-village", transition.FromMapId);
        Assert.Equal(10, transition.FromTile.X);
        Assert.Equal(0, transition.FromTile.Y);
        Assert.Equal(10, transition.ToTile.X);
        Assert.Equal(12, transition.ToTile.Y);
    }

    [Fact]
    public void Riverside_HasSouthTransitionToTrialGrove()
    {
        WriteAllMaps();
        var loader = CreateMapLoader();
        var map = loader.Load("riverside-village.json");

        var transition = map.Transitions.FirstOrDefault(t => t.ToMapId == "trial-grove");
        Assert.NotNull(transition);
        Assert.Equal(10, transition.FromTile.X);
        Assert.Equal(13, transition.FromTile.Y);
    }

    [Fact]
    public void MillRoad_HasEastTransitionToOldWatermill()
    {
        WriteAllMaps();
        var loader = CreateMapLoader();
        var map = loader.Load("mill-road.json");

        var transition = map.Transitions.FirstOrDefault(t => t.ToMapId == "old-watermill");
        Assert.NotNull(transition);
        Assert.Equal(20, transition.FromTile.X);
        Assert.Equal(7, transition.FromTile.Y);
        Assert.Equal(0, transition.ToTile.X);
        Assert.Equal(7, transition.ToTile.Y);
    }

    [Fact]
    public void OldWatermill_HasWestTransitionToMillRoad()
    {
        WriteAllMaps();
        var loader = CreateMapLoader();
        var map = loader.Load("old-watermill.json");

        var transition = map.Transitions.FirstOrDefault(t => t.ToMapId == "mill-road");
        Assert.NotNull(transition);
        Assert.Equal(0, transition.FromTile.X);
        Assert.Equal(7, transition.FromTile.Y);
        Assert.Equal(19, transition.ToTile.X);
        Assert.Equal(7, transition.ToTile.Y);
    }

    // ── 3. Riverside is gated by trial_grove_cleared ─────────────

    [Fact]
    public void Player_CannotEnterRiverside_WithoutTrialGroveCleared()
    {
        WriteAllMaps();
        var mapLoader = CreateMapLoader();
        var trialGrove = mapLoader.Load("trial-grove.json");

        var profile = new PlayerProfile();

        var player = new Player();
        player.Initialize(10, 1);

        bool IsWalkable(int x, int y)
        {
            if (x < 0 || x >= trialGrove.Width || y < 0 || y >= trialGrove.Height) return false;
            if (trialGrove.Layers.Collision[y][x] != 0) return false;

            var t = trialGrove.Transitions.FirstOrDefault(tr => tr.FromTile.X == x && tr.FromTile.Y == y);
            if (t is not null && !string.IsNullOrEmpty(t.RequiredFlag))
            {
                if (!profile.HasFlag(t.RequiredFlag)) return false;
            }
            return true;
        }

        player.Update(0.1f, Direction.Up, IsWalkable);

        Assert.Equal(MovementState.Idle, player.State);
        Assert.Equal(10, player.X);
        Assert.Equal(1, player.Y);
    }

    [Fact]
    public void Player_CanEnterRiverside_WithTrialGroveCleared()
    {
        WriteAllMaps();
        var mapLoader = CreateMapLoader();
        var trialGrove = mapLoader.Load("trial-grove.json");

        var profile = new PlayerProfile();
        profile.SetFlag("trial_grove_cleared", true);

        var player = new Player();
        player.Initialize(10, 1);

        bool IsWalkable(int x, int y)
        {
            if (x < 0 || x >= trialGrove.Width || y < 0 || y >= trialGrove.Height) return false;
            if (trialGrove.Layers.Collision[y][x] != 0) return false;

            var t = trialGrove.Transitions.FirstOrDefault(tr => tr.FromTile.X == x && tr.FromTile.Y == y);
            if (t is not null && !string.IsNullOrEmpty(t.RequiredFlag))
            {
                if (!profile.HasFlag(t.RequiredFlag)) return false;
            }
            return true;
        }

        player.Update(0.1f, Direction.Up, IsWalkable);
        player.Update(0.15f, Direction.None, IsWalkable);

        Assert.Equal(10, player.X);
        Assert.Equal(0, player.Y);

        var triggered = trialGrove.Transitions.FirstOrDefault(t => t.FromTile.X == player.X && t.FromTile.Y == player.Y);
        Assert.NotNull(triggered);
        Assert.Equal("riverside-village", triggered.ToMapId);
    }

    // ── 4. Drizzleaf content validates ──────────────────────────

    [Fact]
    public void Drizzleaf_CreatureFile_CanBeLoaded()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "content", "creatures", "drizzleaf.json");
        var json = File.ReadAllText(path);
        var creature = JsonSerializer.Deserialize<CreatureContent>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(creature);
        Assert.Equal("drizzleaf", creature.Id);
        Assert.Equal("Drizzleaf", creature.Name);
        Assert.Equal("Tide", creature.Type);
        Assert.Equal("Moss", creature.SecondaryType);
        Assert.False(creature.StarterEligible);

        Assert.Equal(55, creature.BaseStats.MaxHp);
        Assert.Equal(6, creature.BaseStats.Attack);
        Assert.Equal(12, creature.BaseStats.Defense);
        Assert.Equal(4, creature.BaseStats.Speed);

        Assert.Equal(3, creature.Learnset.Count);
        Assert.Contains("tide-slap", creature.Learnset);
        Assert.Contains("moss-tap", creature.Learnset);
        Assert.Contains("guard-curl", creature.Learnset);
    }

    [Fact]
    public void Drizzleaf_ConvertsToJoyMonSpecies()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "content", "creatures", "drizzleaf.json");
        var json = File.ReadAllText(path);
        var creature = JsonSerializer.Deserialize<CreatureContent>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(creature);

        var species = new JoyMonSpecies(
            creature.Name,
            creature.Type switch
            {
                "Tide" => JoyMonType.Tide,
                _ => JoyMonType.Neutral
            },
            creature.BaseStats.MaxHp,
            creature.BaseStats.Attack,
            creature.BaseStats.Defense,
            creature.BaseStats.Speed,
            Array.Empty<MoveDefinition>()
        );

        Assert.Equal("Drizzleaf", species.Name);
        Assert.Equal(JoyMonType.Tide, species.Type);
        Assert.Equal(55, species.BaseMaxHp);
        Assert.Equal(6, species.BaseAttack);
        Assert.Equal(12, species.BaseDefense);
        Assert.Equal(4, species.BaseSpeed);
    }

    // ── 5. Mill Road encounter table validates ──────────────────

    [Fact]
    public void MillRoadEncounterTable_LoadsSuccessfully()
    {
        WriteEncounter("mill-road.json", new
        {
            id = "mill-road-encounters",
            mapId = "mill-road",
            zoneId = "grass",
            tileIds = new[] { 1 },
            encounterRate = 0.12,
            entries = new[]
            {
                new { creatureId = "glimmoo", minLevel = 5, maxLevel = 7, weight = 30 },
                new { creatureId = "queuebee", minLevel = 5, maxLevel = 6, weight = 25 },
                new { creatureId = "pebblit", minLevel = 6, maxLevel = 7, weight = 25 },
                new { creatureId = "drizzleaf", minLevel = 6, maxLevel = 8, weight = 20 }
            }
        });

        var loader = CreateEncounterLoader();
        var table = loader.Load("mill-road.json", _validCreatures);

        Assert.Equal("mill-road-encounters", table.Id);
        Assert.Equal("mill-road", table.MapId);
        Assert.Equal("grass", table.ZoneId);
        Assert.Single(table.TileIds);
        Assert.Equal(1, table.TileIds[0]);
        Assert.Equal(0.12, table.EncounterRate);
        Assert.Equal(4, table.Entries.Count);

        Assert.Equal("glimmoo", table.Entries[0].CreatureId);
        Assert.Equal(5, table.Entries[0].MinLevel);
        Assert.Equal(7, table.Entries[0].MaxLevel);
        Assert.Equal(30, table.Entries[0].Weight);

        Assert.Equal("queuebee", table.Entries[1].CreatureId);
        Assert.Equal(5, table.Entries[1].MinLevel);
        Assert.Equal(6, table.Entries[1].MaxLevel);
        Assert.Equal(25, table.Entries[1].Weight);

        Assert.Equal("pebblit", table.Entries[2].CreatureId);
        Assert.Equal(6, table.Entries[2].MinLevel);
        Assert.Equal(7, table.Entries[2].MaxLevel);
        Assert.Equal(25, table.Entries[2].Weight);

        Assert.Equal("drizzleaf", table.Entries[3].CreatureId);
        Assert.Equal(6, table.Entries[3].MinLevel);
        Assert.Equal(8, table.Entries[3].MaxLevel);
        Assert.Equal(20, table.Entries[3].Weight);
    }

    [Fact]
    public void MillRoadEncounter_InvalidCreature_FailsValidation()
    {
        WriteEncounter("bad.json", new
        {
            id = "mill-road-encounters",
            mapId = "mill-road",
            zoneId = "grass",
            tileIds = new[] { 1 },
            encounterRate = 0.12,
            entries = new[]
            {
                new { creatureId = "unknown-creature", minLevel = 5, maxLevel = 5, weight = 100 }
            }
        });

        var loader = CreateEncounterLoader();
        var ex = Assert.Throws<InvalidContentException>(() => loader.Load("bad.json", _validCreatures));
        Assert.Contains("references unknown creature ID", ex.Message);
    }

    [Fact]
    public void MillRoadEncounter_WeightedSelection_IncludesDrizzleaf()
    {
        var entries = new List<EncounterEntryContent>
        {
            new() { CreatureId = "glimmoo", MinLevel = 5, MaxLevel = 7, Weight = 30 },
            new() { CreatureId = "queuebee", MinLevel = 5, MaxLevel = 6, Weight = 25 },
            new() { CreatureId = "pebblit", MinLevel = 6, MaxLevel = 7, Weight = 25 },
            new() { CreatureId = "drizzleaf", MinLevel = 6, MaxLevel = 8, Weight = 20 }
        };

        var rng = new MockRng(0.05, 80);
        var selected = SelectEntry(entries, rng);
        Assert.Equal("drizzleaf", selected.CreatureId);

        int level = selected.MinLevel;
        if (selected.MaxLevel > selected.MinLevel)
        {
            level = rng.Next(selected.MinLevel, selected.MaxLevel + 1);
        }
        Assert.InRange(level, 6, 8);
    }

    // ── 6. Old Watermill locked door trigger works ──────────────

    [Fact]
    public void OldWatermill_LockedDoor_IsCollisionBlocked()
    {
        // Create old-watermill with a custom collision grid that has the locked door at (10,7)
        var collision = Grid(22, 14, 0);
        collision[7][10] = 1; // Locked inner door

        WriteMap("old-watermill.json", new
        {
            id = "old-watermill",
            name = "Old Watermill",
            width = 22,
            height = 14,
            tileSize = 16,
            tilesetId = "overworld",
            spawnPoint = new { x = 1, y = 7 },
            layers = new
            {
                ground = Grid(22, 14, 2),
                decoration = Grid(22, 14, 0),
                collision
            },
            transitions = new[]
            {
                new
                {
                    fromMapId = "old-watermill",
                    fromTile = new { x = 0, y = 7 },
                    toMapId = "mill-road",
                    toTile = new { x = 19, y = 7 }
                }
            }
        });

        WriteMap("starter-town.json", MinimalMap("starter-town", "Starter Town"));
        WriteMap("route-1.json", MinimalMap("route-1", "Route 1"));
        WriteMap("trial-grove.json", MinimalMap("trial-grove", "Trial Grove"));
        WriteMap("riverside-village.json", MinimalMap("riverside-village", "Riverside Village"));
        WriteMap("mill-road.json", MinimalMap("mill-road", "Mill Road"));

        var loader = CreateMapLoader();
        var map = loader.Load("old-watermill.json");

        Assert.Equal(1, map.Layers.Collision[7][10]);
    }

    [Fact]
    public void OldWatermill_LockedDoor_HasNpcWithPlaceholderDialogue()
    {
        var dialoguePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "content", "dialogue", "riverside-village.json");
        var json = File.ReadAllText(dialoguePath);
        var content = JsonSerializer.Deserialize<DialogueFileContent>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(content);

        var doorNpc = content.Npcs.FirstOrDefault(n => n.Id == "old-watermill-door");
        Assert.NotNull(doorNpc);
        Assert.Equal("old-watermill", doorNpc.MapId);
        Assert.Equal(10, doorNpc.TilePosition.X);
        Assert.Equal(7, doorNpc.TilePosition.Y);

        var doorDialogue = content.Dialogues.FirstOrDefault(d => d.Id == doorNpc.DialogueId);
        Assert.NotNull(doorDialogue);
        Assert.Equal("Locked Door", doorDialogue.Speaker);
        Assert.True(doorDialogue.Lines.Count > 0);
        Assert.Contains("bolted shut", doorDialogue.Lines[0]);
    }

    // ── 7. Backtracking to previous maps works ──────────────────

    [Fact]
    public void Player_CanBacktrack_FromRiversideToTrialGrove()
    {
        WriteAllMaps();
        var mapLoader = CreateMapLoader();
        var riverside = mapLoader.Load("riverside-village.json");

        var profile = new PlayerProfile();

        var player = new Player();
        player.Initialize(10, 12);

        bool IsWalkable(int x, int y)
        {
            if (x < 0 || x >= riverside.Width || y < 0 || y >= riverside.Height) return false;
            if (riverside.Layers.Collision[y][x] != 0) return false;

            var t = riverside.Transitions.FirstOrDefault(tr => tr.FromTile.X == x && tr.FromTile.Y == y);
            if (t is not null && !string.IsNullOrEmpty(t.RequiredFlag))
            {
                if (!profile.HasFlag(t.RequiredFlag)) return false;
            }
            return true;
        }

        player.Update(0.1f, Direction.Down, IsWalkable);
        player.Update(0.15f, Direction.None, IsWalkable);

        Assert.Equal(10, player.X);
        Assert.Equal(13, player.Y);

        var triggered = riverside.Transitions.FirstOrDefault(t => t.FromTile.X == player.X && t.FromTile.Y == player.Y);
        Assert.NotNull(triggered);
        Assert.Equal("trial-grove", triggered.ToMapId);
    }

    [Fact]
    public void Player_CanBacktrack_FromMillRoadToRiverside()
    {
        WriteAllMaps();
        var mapLoader = CreateMapLoader();
        var millRoad = mapLoader.Load("mill-road.json");

        var profile = new PlayerProfile();

        var player = new Player();
        player.Initialize(10, 12);

        bool IsWalkable(int x, int y)
        {
            if (x < 0 || x >= millRoad.Width || y < 0 || y >= millRoad.Height) return false;
            if (millRoad.Layers.Collision[y][x] != 0) return false;

            var t = millRoad.Transitions.FirstOrDefault(tr => tr.FromTile.X == x && tr.FromTile.Y == y);
            if (t is not null && !string.IsNullOrEmpty(t.RequiredFlag))
            {
                if (!profile.HasFlag(t.RequiredFlag)) return false;
            }
            return true;
        }

        player.Update(0.1f, Direction.Down, IsWalkable);
        player.Update(0.15f, Direction.None, IsWalkable);

        Assert.Equal(13, player.Y);

        var triggered = millRoad.Transitions.FirstOrDefault(t => t.FromTile.X == player.X && t.FromTile.Y == player.Y);
        Assert.NotNull(triggered);
        Assert.Equal("riverside-village", triggered.ToMapId);
    }

    // ── 8. Riverside Village NPC dialogue loads ─────────────────

    [Fact]
    public void RiversideVillageDialogue_LoadsSuccessfully()
    {
        var dialoguePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "content", "dialogue", "riverside-village.json");
        var json = File.ReadAllText(dialoguePath);
        var content = JsonSerializer.Deserialize<DialogueFileContent>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(content);
        Assert.Equal(7, content.Npcs.Count);
        Assert.Equal(7, content.Dialogues.Count);

        var healer = content.Npcs.FirstOrDefault(n => n.Id == "riverside-healer");
        Assert.NotNull(healer);
        Assert.Equal("riverside-village", healer.MapId);
        Assert.Equal(9, healer.TilePosition.X);
        Assert.Equal(10, healer.TilePosition.Y);

        var elderDlg = content.Dialogues.FirstOrDefault(d => d.Id == "riverside-elder-talk");
        Assert.NotNull(elderDlg);
        Assert.Equal("Village Elder", elderDlg.Speaker);
        Assert.Equal(4, elderDlg.Lines.Count);
        Assert.Contains("watermill", elderDlg.Lines[1]);
        Assert.Contains("Echo-type", elderDlg.Lines[2]);

        var fisherDlg = content.Dialogues.FirstOrDefault(d => d.Id == "riverside-fisher-talk");
        Assert.NotNull(fisherDlg);
        Assert.Contains("humming", fisherDlg.Lines[1]);

        var workerDlg = content.Dialogues.FirstOrDefault(d => d.Id == "riverside-worker-talk");
        Assert.NotNull(workerDlg);
        Assert.Contains("bridge", workerDlg.Lines[0]);
        Assert.Contains("delayed", workerDlg.Lines[2]);

        var scoutDlg = content.Dialogues.FirstOrDefault(d => d.Id == "riverside-scout-talk");
        Assert.NotNull(scoutDlg);
        Assert.Contains("Lanternox", scoutDlg.Lines[0]);
    }

    // ── Helpers ────────────────────────────────────────────────

    private static EncounterEntryContent SelectEntry(List<EncounterEntryContent> entries, IRng rng)
    {
        int totalWeight = entries.Sum(e => e.Weight);
        int roll = rng.Next(totalWeight);
        int currentSum = 0;

        foreach (var entry in entries)
        {
            currentSum += entry.Weight;
            if (roll < currentSum) return entry;
        }

        return entries[0];
    }

    private class MockRng : IRng
    {
        private readonly double _doubleVal;
        private readonly int _intVal;

        public MockRng(double doubleVal, int intVal)
        {
            _doubleVal = doubleVal;
            _intVal = intVal;
        }

        public int Next(int maxValue) => _intVal % maxValue;
        public int Next(int minValue, int maxValue) => minValue + (_intVal % (maxValue - minValue));
        public double NextDouble() => _doubleVal;
    }
}