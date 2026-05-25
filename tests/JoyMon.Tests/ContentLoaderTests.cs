using System.Text.Json;
using JoyMon.Content;
using JoyMon.Core;

namespace JoyMon.Tests;

public class ContentLoaderTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "JoyMonTest_" + Guid.NewGuid());

    public ContentLoaderTests()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, "moves"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "creatures"));
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best-effort cleanup */ }
    }

    // ── Helpers ─────────────────────────────────────────────────

    private void WriteMove(string filename, object data) =>
        File.WriteAllText(Path.Combine(_tempDir, "moves", filename), JsonSerializer.Serialize(data));

    private void WriteCreature(string filename, object data) =>
        File.WriteAllText(Path.Combine(_tempDir, "creatures", filename), JsonSerializer.Serialize(data));

    private ContentLoader CreateLoader() => new(_tempDir);

    private void SeedValidContent()
    {
        WriteMove("tackle.json", new
        {
            id = "tackle", name = "Tackle", type = "Neutral",
            power = 20, accuracy = 95, maxUses = 25
        });
        WriteMove("vine-whip.json", new
        {
            id = "vine-whip", name = "Vine Whip", type = "Moss",
            power = 40, accuracy = 95, maxUses = 15
        });
        WriteCreature("mossprout.json", new
        {
            id = "mossprout", name = "Mossprout", type = "Moss",
            baseStats = new { maxHp = 48, attack = 8, defense = 9, speed = 6 },
            starterEligible = true,
            learnset = new[] { "vine-whip", "tackle" }
        });
    }

    // ── 1. Valid content loads successfully ─────────────────────

    [Fact]
    public void ValidContent_LoadsSuccessfully()
    {
        SeedValidContent();
        var loader = CreateLoader();

        var db = loader.Load();

        Assert.NotNull(db);
        Assert.Equal(2, db.Moves.Count);
        Assert.Single(db.Creatures);
        Assert.Equal(2, db.MoveDefinitions.Count);
        Assert.Single(db.Species);
    }

    // ── 2. Duplicate creature IDs fail validation ───────────────

    [Fact]
    public void DuplicateCreatureId_FailsValidation()
    {
        WriteMove("tackle.json", new
        {
            id = "tackle", name = "Tackle", type = "Neutral",
            power = 20, accuracy = 95, maxUses = 25
        });

        // Two creatures with the same ID
        WriteCreature("a.json", new
        {
            id = "dupe", name = "Alpha", type = "Moss",
            baseStats = new { maxHp = 40, attack = 8, defense = 8, speed = 7 },
            learnset = new[] { "tackle" }
        });
        WriteCreature("b.json", new
        {
            id = "dupe", name = "Beta", type = "Spark",
            baseStats = new { maxHp = 35, attack = 6, defense = 5, speed = 12 },
            learnset = new[] { "tackle" }
        });

        var loader = CreateLoader();
        var ex = Assert.Throws<InvalidContentException>(() => loader.Load());
        Assert.Contains("dupe", ex.Message);
    }

    // ── 3. Missing move reference fails validation ──────────────

    [Fact]
    public void MissingMoveReference_FailsValidation()
    {
        WriteMove("tackle.json", new
        {
            id = "tackle", name = "Tackle", type = "Neutral",
            power = 20, accuracy = 95, maxUses = 25
        });
        WriteCreature("mossprout.json", new
        {
            id = "mossprout", name = "Mossprout", type = "Moss",
            baseStats = new { maxHp = 48, attack = 8, defense = 9, speed = 6 },
            starterEligible = true,
            learnset = new[] { "tackle", "does-not-exist" }
        });

        var loader = CreateLoader();
        var ex = Assert.Throws<InvalidContentException>(() => loader.Load());
        Assert.Contains("does-not-exist", ex.Message);
    }

    // ── 4. Missing required field fails validation ──────────────

    [Fact]
    public void MissingRequiredField_FailsValidation()
    {
        // Move with missing 'power' field
        WriteMove("bad.json", new
        {
            id = "bad-move", name = "Bad Move", type = "Neutral",
            power = 0, // 0 is invalid (> 0 required)
            accuracy = 95, maxUses = 25
        });
        WriteCreature("c.json", new
        {
            id = "test-creature", name = "Test", type = "Moss",
            baseStats = new { maxHp = 40, attack = 8, defense = 8, speed = 7 },
            learnset = new[] { "bad-move" }
        });

        var loader = CreateLoader();
        var ex = Assert.Throws<InvalidContentException>(() => loader.Load());
        Assert.Contains("power", ex.Message);
    }

    // ── 5. Creature converts to core JoyMonSpecies ──────────────

    [Fact]
    public void Creature_ConvertsToJoyMonSpecies()
    {
        WriteMove("vine-whip.json", new
        {
            id = "vine-whip", name = "Vine Whip", type = "Moss",
            power = 40, accuracy = 95, maxUses = 15
        });
        WriteMove("tackle.json", new
        {
            id = "tackle", name = "Tackle", type = "Neutral",
            power = 20, accuracy = 95, maxUses = 25
        });
        WriteCreature("mossprout.json", new
        {
            id = "mossprout", name = "Mossprout", type = "Moss",
            baseStats = new { maxHp = 48, attack = 8, defense = 9, speed = 6 },
            starterEligible = true,
            learnset = new[] { "vine-whip", "tackle" }
        });

        var loader = CreateLoader();
        var db = loader.Load();
        var species = db.Species["mossprout"];

        Assert.Equal("Mossprout", species.Name);
        Assert.Equal(JoyMonType.Moss, species.Type);
        Assert.Equal(48, species.BaseMaxHp);
        Assert.Equal(8, species.BaseAttack);
        Assert.Equal(9, species.BaseDefense);
        Assert.Equal(6, species.BaseSpeed);
        Assert.Equal(2, species.Moves.Count);
    }

    // ── 6. Move converts to core MoveDefinition ─────────────────

    [Fact]
    public void Move_ConvertsToMoveDefinition()
    {
        WriteMove("spark-peck.json", new
        {
            id = "spark-peck", name = "Spark Peck", type = "Spark",
            power = 45, accuracy = 90, maxUses = 12
        });
        WriteCreature("c.json", new
        {
            id = "test-c", name = "Test", type = "Spark",
            baseStats = new { maxHp = 35, attack = 7, defense = 5, speed = 13 },
            learnset = new[] { "spark-peck" }
        });

        var loader = CreateLoader();
        var db = loader.Load();
        var move = db.MoveDefinitions["spark-peck"];

        Assert.Equal("spark-peck", move.Id);
        Assert.Equal("Spark Peck", move.Name);
        Assert.Equal(JoyMonType.Spark, move.Type);
        Assert.Equal(45, move.Power);
        Assert.Equal(90, move.Accuracy);
        Assert.Equal(12, move.MaxUses);
    }
}