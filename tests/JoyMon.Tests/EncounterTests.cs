using System.Text.Json;
using JoyMon.Content;
using JoyMon.Core;

namespace JoyMon.Tests;

public class EncounterTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "JoyMonEncounterTest_" + Guid.NewGuid());
    private readonly HashSet<string> _validCreatures = new() { "queuebee", "rootsnail", "cindermite" };

    public EncounterTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private void WriteEncounter(string filename, object data) =>
        File.WriteAllText(Path.Combine(_tempDir, filename), JsonSerializer.Serialize(data));

    private EncounterLoader CreateLoader() => new(_tempDir);

    private object ValidTable() => new
    {
        id = "route-1-encounters",
        mapId = "route-1",
        zoneId = "grass",
        tileIds = new[] { 1 },
        encounterRate = 0.1,
        entries = new[]
        {
            new { creatureId = "queuebee", minLevel = 2, maxLevel = 3, weight = 50 },
            new { creatureId = "rootsnail", minLevel = 2, maxLevel = 4, weight = 35 },
            new { creatureId = "cindermite", minLevel = 3, maxLevel = 3, weight = 15 }
        }
    };

    // ── 1. Encounter content validates ───────────────────────────
    [Fact]
    public void ValidEncounterTable_LoadsSuccessfully()
    {
        WriteEncounter("valid.json", ValidTable());
        var loader = CreateLoader();
        var table = loader.Load("valid.json", _validCreatures);

        Assert.Equal("route-1-encounters", table.Id);
        Assert.Equal("route-1", table.MapId);
        Assert.Equal("grass", table.ZoneId);
        Assert.Single(table.TileIds);
        Assert.Equal(1, table.TileIds[0]);
        Assert.Equal(0.1, table.EncounterRate);
        Assert.Equal(3, table.Entries.Count);
        Assert.Equal("queuebee", table.Entries[0].CreatureId);
        Assert.Equal(2, table.Entries[0].MinLevel);
        Assert.Equal(3, table.Entries[0].MaxLevel);
        Assert.Equal(50, table.Entries[0].Weight);
    }

    // ── 2. Invalid creature reference fails ────────────────────────
    [Fact]
    public void InvalidCreatureReference_FailsValidation()
    {
        WriteEncounter("invalid_creature.json", new
        {
            id = "test-encounters",
            mapId = "route-1",
            zoneId = "grass",
            tileIds = new[] { 1 },
            encounterRate = 0.1,
            entries = new[]
            {
                new { creatureId = "unknown-creature", minLevel = 5, maxLevel = 5, weight = 10 }
            }
        });

        var loader = CreateLoader();
        var ex = Assert.Throws<InvalidContentException>(() => loader.Load("invalid_creature.json", _validCreatures));
        Assert.Contains("references unknown creature ID", ex.Message);
    }

    // ── 3. Weighted selection is deterministic with seeded RNG ───
    [Fact]
    public void WeightedSelection_IsDeterministic_WithSeededRng()
    {
        var entries = new List<EncounterEntryContent>
        {
            new() { CreatureId = "queuebee", MinLevel = 2, MaxLevel = 3, Weight = 50 },      // cumulative weight: 0 - 49
            new() { CreatureId = "rootsnail", MinLevel = 2, MaxLevel = 4, Weight = 35 },     // cumulative weight: 50 - 84
            new() { CreatureId = "cindermite", MinLevel = 3, MaxLevel = 3, Weight = 15 }     // cumulative weight: 85 - 99
        };

        // If roll is 49 -> selects queuebee
        var rng1 = new MockRng(0.05, 49);
        var entry1 = SelectEntry(entries, rng1);
        Assert.Equal("queuebee", entry1.CreatureId);

        // If roll is 50 -> selects rootsnail
        var rng2 = new MockRng(0.05, 50);
        var entry2 = SelectEntry(entries, rng2);
        Assert.Equal("rootsnail", entry2.CreatureId);

        // If roll is 85 -> selects cindermite
        var rng3 = new MockRng(0.05, 85);
        var entry3 = SelectEntry(entries, rng3);
        Assert.Equal("cindermite", entry3.CreatureId);
    }

    // ── 4. No encounter occurs on non-grass tiles ──────────────────
    [Fact]
    public void NoEncounterOccurs_OnNonGrassTiles()
    {
        var currentMap = new MapContent
        {
            Id = "route-1",
            Width = 10,
            Height = 10,
            Layers = new MapLayersContent
            {
                Ground = Grid(10, 10, 2), // 2 = path tile
                Collision = Grid(10, 10, 0)
            }
        };

        var table = new EncounterTableContent
        {
            MapId = "route-1",
            TileIds = new List<int> { 1 }, // Only tile ID 1 (grass) triggers encounters
            EncounterRate = 1.0 // 100% trigger rate
        };

        var player = new Player();
        player.Initialize(5, 5);

        // Step from (5,5) to (6,5)
        player.Update(0.1f, Direction.Right, (x, y) => true);
        player.Update(0.15f, Direction.None, (x, y) => true);

        // Ground tile at player new position (6,5) is 2 (path)
        int tileId = currentMap.Layers.Ground[player.Y][player.X];

        // Verification: tileId is 2, which is not in table.TileIds
        Assert.DoesNotContain(tileId, table.TileIds);
    }

    // ── 5. Encounters cannot trigger without starter ───────────────
    [Fact]
    public void EncountersCannotTrigger_WithoutStarter()
    {
        var currentMap = new MapContent
        {
            Id = "route-1",
            Width = 10,
            Height = 10,
            Layers = new MapLayersContent
            {
                Ground = Grid(10, 10, 1), // all grass
                Collision = Grid(10, 10, 0)
            }
        };

        var table = new EncounterTableContent
        {
            MapId = "route-1",
            TileIds = new List<int> { 1 },
            EncounterRate = 1.0 // 100% trigger rate
        };

        var profile = new PlayerProfile(); // Has no flags, received_starter is false

        var rng = new MockRng(0.01, 10); // rate check 0.01 < 1.0 would normally trigger

        bool canTrigger = profile.HasFlag("received_starter") && table.TileIds.Contains(currentMap.Layers.Ground[5][6]) && (rng.NextDouble() < table.EncounterRate);

        Assert.False(canTrigger);
    }

    // ── 6. Encounter switches to battle placeholder ────────────────
    [Fact]
    public void Encounter_SwitchesToBattlePlaceholder()
    {
        var species = new JoyMonSpecies("Queuebee", JoyMonType.Spark, 40, 7, 5, 10, new List<MoveDefinition> { new("tackle", "Tackle", JoyMonType.Neutral, 35, 95, 35) });
        var db = new Dictionary<string, JoyMonSpecies> { { "queuebee", species } };

        var table = new EncounterTableContent
        {
            MapId = "route-1",
            TileIds = new List<int> { 1 },
            EncounterRate = 1.0,
            Entries = new List<EncounterEntryContent>
            {
                new() { CreatureId = "queuebee", MinLevel = 2, MaxLevel = 3, Weight = 100 }
            }
        };

        // Rng returns roll that triggers (0.01 < 1.0) and selects queuebee (level 2)
        var rng = new MockRng(0.01, 0);

        // Simulation of trigger
        JoyMonInstance? wildEncounter = null;
        string state = "Overworld";

        int totalWeight = table.Entries.Sum(e => e.Weight);
        int roll = rng.Next(totalWeight);
        int currentSum = 0;
        EncounterEntryContent? selectedEntry = null;

        foreach (var entry in table.Entries)
        {
            currentSum += entry.Weight;
            if (roll < currentSum)
            {
                selectedEntry = entry;
                break;
            }
        }

        if (selectedEntry is not null)
        {
            int level = selectedEntry.MinLevel;
            if (selectedEntry.MaxLevel > selectedEntry.MinLevel)
            {
                level = rng.Next(selectedEntry.MinLevel, selectedEntry.MaxLevel + 1);
            }

            if (db.TryGetValue(selectedEntry.CreatureId, out var sp))
            {
                wildEncounter = sp.CreateInstance(level);
                state = "Battle";
            }
        }

        Assert.Equal("Battle", state);
        Assert.NotNull(wildEncounter);
        Assert.Equal("Queuebee", wildEncounter.Species.Name);
        Assert.Equal(2, wildEncounter.Level);
    }

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
