using System.Text.Json;
using JoyMon.Content;
using JoyMon.Core;

namespace JoyMon.Tests;

public class MapTransitionTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "JoyMonMapTransitionTest_" + Guid.NewGuid());

    public MapTransitionTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private void WriteMap(string filename, object data) =>
        File.WriteAllText(Path.Combine(_tempDir, filename), JsonSerializer.Serialize(data));

    private MapLoader CreateLoader() => new(_tempDir);

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

    private object MapWithTransitions(string id, string name, object[] transitions) => new
    {
        id,
        name,
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
        },
        transitions
    };

    // ── 1. Transition content validates ───────────────────────────
    [Fact]
    public void MapWithTransitions_ValidatesSuccessfully()
    {
        var trans = new[]
        {
            new
            {
                fromMapId = "town",
                fromTile = new { x = 5, y = 0 },
                toMapId = "route",
                toTile = new { x = 5, y = 7 },
                requiredFlag = "received_starter"
            }
        };

        // Write both maps to ensure reference existence check passes
        WriteMap("town.json", MapWithTransitions("town", "Town", trans));
        WriteMap("route.json", MapWithTransitions("route", "Route", Array.Empty<object>()));

        var loader = CreateLoader();
        var map = loader.Load("town.json");

        Assert.Equal("town", map.Id);
        Assert.Single(map.Transitions);
        Assert.Equal("town", map.Transitions[0].FromMapId);
        Assert.Equal(5, map.Transitions[0].FromTile.X);
        Assert.Equal(0, map.Transitions[0].FromTile.Y);
        Assert.Equal("route", map.Transitions[0].ToMapId);
        Assert.Equal(5, map.Transitions[0].ToTile.X);
        Assert.Equal(7, map.Transitions[0].ToTile.Y);
        Assert.Equal("received_starter", map.Transitions[0].RequiredFlag);
    }

    // ── 2. Missing target map fails validation ────────────────────
    [Fact]
    public void MissingTargetMap_FailsValidation()
    {
        var trans = new[]
        {
            new
            {
                fromMapId = "town",
                fromTile = new { x = 5, y = 0 },
                toMapId = "non-existent-map",
                toTile = new { x = 5, y = 7 },
                requiredFlag = "received_starter"
            }
        };

        WriteMap("town.json", MapWithTransitions("town", "Town", trans));

        var loader = CreateLoader();
        var ex = Assert.Throws<InvalidContentException>(() => loader.Load("town.json"));
        Assert.Contains("transitions to missing map 'non-existent-map'", ex.Message);
    }

    // ── 3. Player can transition when requirements met ────────────
    [Fact]
    public void Player_CanTransition_WhenRequirementsMet()
    {
        // Mock current map
        var trans = new List<MapTransitionContent>
        {
            new()
            {
                FromMapId = "town",
                FromTile = new TransitionTileContent { X = 5, Y = 0 },
                ToMapId = "route",
                ToTile = new TransitionTileContent { X = 5, Y = 7 },
                RequiredFlag = "received_starter"
            }
        };
        var currentMap = new MapContent
        {
            Id = "town",
            Width = 10,
            Height = 8,
            Transitions = trans,
            Layers = new MapLayersContent
            {
                Collision = Grid(10, 8, 0)
            }
        };

        var profile = new PlayerProfile();
        profile.SetFlag("received_starter", true);

        var player = new Player();
        player.Initialize(5, 1);

        // Simulation of movement step:
        // Walkability check for moving to (5, 0)
        bool IsWalkable(int x, int y)
        {
            if (x < 0 || x >= currentMap.Width || y < 0 || y >= currentMap.Height) return false;
            if (currentMap.Layers.Collision[y][x] != 0) return false;

            var t = currentMap.Transitions.FirstOrDefault(tr => tr.FromTile.X == x && tr.FromTile.Y == y);
            if (t is not null && !string.IsNullOrEmpty(t.RequiredFlag))
            {
                if (!profile.HasFlag(t.RequiredFlag)) return false;
            }
            return true;
        }

        // Try start move up to (5,0)
        player.Update(0.1f, Direction.Up, IsWalkable);
        Assert.Equal(MovementState.Moving, player.State);
        Assert.Equal(5, player.TargetX);
        Assert.Equal(0, player.TargetY);

        // Finish step
        player.Update(0.15f, Direction.None, IsWalkable);
        Assert.Equal(MovementState.Idle, player.State);
        Assert.Equal(5, player.X);
        Assert.Equal(0, player.Y);

        // Verify transition condition triggers
        var triggeredTransition = currentMap.Transitions.FirstOrDefault(t => t.FromTile.X == player.X && t.FromTile.Y == player.Y);
        Assert.NotNull(triggeredTransition);
        Assert.Equal("route", triggeredTransition.ToMapId);
        Assert.Equal(5, triggeredTransition.ToTile.X);
        Assert.Equal(7, triggeredTransition.ToTile.Y);
    }

    // ── 4. Player cannot transition when required flag missing ────
    [Fact]
    public void Player_CannotTransition_WhenRequiredFlagMissing()
    {
        // Mock current map
        var trans = new List<MapTransitionContent>
        {
            new()
            {
                FromMapId = "town",
                FromTile = new TransitionTileContent { X = 5, Y = 0 },
                ToMapId = "route",
                ToTile = new TransitionTileContent { X = 5, Y = 7 },
                RequiredFlag = "received_starter"
            }
        };
        var currentMap = new MapContent
        {
            Id = "town",
            Width = 10,
            Height = 8,
            Transitions = trans,
            Layers = new MapLayersContent
            {
                Collision = Grid(10, 8, 0)
            }
        };

        var profile = new PlayerProfile(); // No flag set

        var player = new Player();
        player.Initialize(5, 1);

        // Walkability check
        bool IsWalkable(int x, int y)
        {
            if (x < 0 || x >= currentMap.Width || y < 0 || y >= currentMap.Height) return false;
            if (currentMap.Layers.Collision[y][x] != 0) return false;

            var t = currentMap.Transitions.FirstOrDefault(tr => tr.FromTile.X == x && tr.FromTile.Y == y);
            if (t is not null && !string.IsNullOrEmpty(t.RequiredFlag))
            {
                if (!profile.HasFlag(t.RequiredFlag)) return false;
            }
            return true;
        }

        // Try start move up to (5,0)
        player.Update(0.1f, Direction.Up, IsWalkable);
        
        // Since walkability check returns false, the player should not start moving and remain Idle at (5,1)
        Assert.Equal(MovementState.Idle, player.State);
        Assert.Equal(5, player.X);
        Assert.Equal(1, player.Y);
    }

    // ── 5. Player appears at correct destination tile ─────────────
    [Fact]
    public void Transition_PlacesPlayerAtCorrectDestinationTile()
    {
        var trans = new MapTransitionContent
        {
            FromMapId = "town",
            FromTile = new TransitionTileContent { X = 5, Y = 0 },
            ToMapId = "route",
            ToTile = new TransitionTileContent { X = 7, Y = 6 }
        };

        var player = new Player();
        player.Initialize(5, 0); // Player is on transition tile

        // Simulate transition execution
        player.Initialize(trans.ToTile.X, trans.ToTile.Y);

        Assert.Equal(7, player.X);
        Assert.Equal(6, player.Y);
        Assert.Equal(7, player.TargetX);
        Assert.Equal(6, player.TargetY);
        Assert.Equal(MovementState.Idle, player.State);
    }
}
