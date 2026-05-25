using System.Text.Json;
using JoyMon.Content;

namespace JoyMon.Tests;

public class MapLoaderTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "JoyMonMapTest_" + Guid.NewGuid());

    public MapLoaderTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private void WriteMap(string filename, object data) =>
        File.WriteAllText(Path.Combine(_tempDir, filename),
            JsonSerializer.Serialize(data));

    private MapLoader CreateLoader() => new(_tempDir);

    private object ValidMap() => new
    {
        id = "test-town",
        name = "Test Town",
        width = 10,
        height = 8,
        tileSize = 16,
        tilesetId = "overworld",
        spawnPoint = new { x = 2, y = 3 },
        layers = new
        {
            ground = Grid(10, 8, 1),
            decoration = Grid(10, 8, 0),
            collision = Grid(10, 8, 0),
        }
    };

    /// <summary>Generates an H×W grid filled with a constant value.</summary>
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

    // ── 1. Valid map loads ──────────────────────────────────────

    [Fact]
    public void ValidMap_LoadsSuccessfully()
    {
        WriteMap("town.json", ValidMap());
        var loader = CreateLoader();

        var map = loader.Load("town.json");

        Assert.Equal("test-town", map.Id);
        Assert.Equal("Test Town", map.Name);
        Assert.Equal(10, map.Width);
        Assert.Equal(8, map.Height);
        Assert.Equal(16, map.TileSize);
        Assert.NotNull(map.SpawnPoint);
        Assert.Equal(3, map.SpawnPoint.Y);
        Assert.Equal(8, map.Layers.Ground.Count);
    }

    // ── 2. Invalid dimensions fail ──────────────────────────────

    [Fact]
    public void InvalidDimensions_FailValidation()
    {
        WriteMap("bad.json", new
        {
            id = "bad",
            name = "Bad",
            width = 0, // invalid
            height = -1, // invalid
            tileSize = 16,
            tilesetId = "x",
            spawnPoint = new { x = 0, y = 0 },
            layers = new
            {
                ground = new[] { new[] { 1 } },
                decoration = new[] { new[] { 0 } },
                collision = new[] { new[] { 0 } },
            }
        });

        var loader = CreateLoader();
        var ex = Assert.Throws<InvalidContentException>(() => loader.Load("bad.json"));
        Assert.Contains("width", ex.Message);
        Assert.Contains("height", ex.Message);
    }

    // ── 3. Layer size mismatch fails ───────────────────────────

    [Fact]
    public void LayerSizeMismatch_FailsValidation()
    {
        WriteMap("mismatch.json", new
        {
            id = "mismatch",
            name = "Mismatch",
            width = 10,
            height = 8,
            tileSize = 16,
            tilesetId = "x",
            spawnPoint = new { x = 0, y = 0 },
            layers = new
            {
                ground = Grid(10, 8, 1),
                decoration = Grid(10, 8, 0),
                collision = Grid(5, 3, 0), // wrong size
            }
        });

        var loader = CreateLoader();
        var ex = Assert.Throws<InvalidContentException>(() => loader.Load("mismatch.json"));
        Assert.Contains("collision", ex.Message);
    }

    // ── 4. Missing spawn point fails ───────────────────────────

    [Fact]
    public void MissingSpawnPoint_FailsValidation()
    {
        WriteMap("nospawn.json", new
        {
            id = "nospawn",
            name = "No Spawn",
            width = 10,
            height = 8,
            tileSize = 16,
            tilesetId = "x",
            layers = new
            {
                ground = Grid(10, 8, 1),
                decoration = Grid(10, 8, 0),
                collision = Grid(10, 8, 0),
            }
        });

        var loader = CreateLoader();
        var ex = Assert.Throws<InvalidContentException>(() => loader.Load("nospawn.json"));
        Assert.Contains("spawnPoint", ex.Message);
    }

    // ── 5. Collision layer parses correctly ────────────────────

    [Fact]
    public void CollisionLayer_ParsesCorrectly()
    {
        // Custom collision: a wall at (2,3), the rest open
        var collision = Grid(10, 8, 0);
        collision[3][2] = 1; // row 3, col 2 = blocked

        WriteMap("collision_test.json", new
        {
            id = "collision-test",
            name = "Collision Test",
            width = 10,
            height = 8,
            tileSize = 16,
            tilesetId = "x",
            spawnPoint = new { x = 1, y = 1 },
            layers = new
            {
                ground = Grid(10, 8, 1),
                decoration = Grid(10, 8, 0),
                collision,
            }
        });

        var loader = CreateLoader();
        var map = loader.Load("collision_test.json");

        Assert.Equal(0, map.Layers.Collision[0][0]); // open
        Assert.Equal(1, map.Layers.Collision[3][2]); // blocked
        Assert.Equal(8, map.Layers.Collision.Count); // correct row count
    }
}