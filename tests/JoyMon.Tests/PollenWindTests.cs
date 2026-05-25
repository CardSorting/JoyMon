using JoyMon.Content;
using JoyMon.Core;

namespace JoyMon.Tests;

public class PollenWindTests
{
    private static readonly string ContentRoot = FindContentRoot();

    [Fact]
    public void PollenWindMetadata_ValidatesSuccessfully()
    {
        var map = new MapLoader(Path.Combine(ContentRoot, "maps")).Load("flowerline-fields.json");

        Assert.NotNull(map.Layers.MovementEffect);
        Assert.Equal(map.Height, map.Layers.MovementEffect.Count);
        Assert.Equal(map.Width, map.Layers.MovementEffect[0].Count);

        foreach (var row in map.Layers.MovementEffect)
        {
            foreach (var effect in row)
                Assert.True(MovementEffect.IsValid(effect));
        }

        // Verify the wind tiles exist
        Assert.Contains(map.Layers.MovementEffect.SelectMany(r => r), e => e == MovementEffect.PollenWindWest);
        Assert.Contains(map.Layers.MovementEffect.SelectMany(r => r), e => e == MovementEffect.PollenWindEast);
        Assert.Contains(map.Layers.MovementEffect.SelectMany(r => r), e => e == MovementEffect.PollenWindNorth);
        Assert.Contains(map.Layers.MovementEffect.SelectMany(r => r), e => e == MovementEffect.PollenWindSouth);
    }

    [Fact]
    public void Player_PushedInCorrectDirection()
    {
        var player = new Player();

        // 1. pollen_wind_east -> push right
        player.Initialize(5, 5);
        bool IsWalkable(int x, int y) => true;
        string GetMovementEffect(int x, int y) => x == 5 && y == 5 ? MovementEffect.PollenWindEast : MovementEffect.Normal;

        // Perform mid-step update to check active push state
        player.Update(0.1f, Direction.None, IsWalkable, null, GetMovementEffect);
        Assert.True(player.IsWindPushed);
        Assert.Equal(Direction.Right, player.Facing);
        Assert.Equal(MovementState.Moving, player.State);
        Assert.Equal(5, player.X);
        Assert.Equal(6, player.TargetX);

        // Complete the push step
        player.Update(0.1f, Direction.None, IsWalkable, null, GetMovementEffect);
        Assert.Equal(6, player.X);
        Assert.False(player.IsWindPushed);

        // 2. pollen_wind_south -> push down
        player.Initialize(5, 5);
        string GetMovementEffectSouth(int x, int y) => x == 5 && y == 5 ? MovementEffect.PollenWindSouth : MovementEffect.Normal;

        player.Update(0.1f, Direction.None, IsWalkable, null, GetMovementEffectSouth);
        Assert.True(player.IsWindPushed);
        Assert.Equal(Direction.Down, player.Facing);
        Assert.Equal(5, player.Y);
        Assert.Equal(6, player.TargetY);
    }

    [Fact]
    public void BlockedDestination_PreventsPush()
    {
        var player = new Player();
        player.Initialize(5, 5);

        // Destination (6, 5) is blocked
        bool IsWalkable(int x, int y) => !(x == 6 && y == 5);
        string GetMovementEffect(int x, int y) => x == 5 && y == 5 ? MovementEffect.PollenWindEast : MovementEffect.Normal;

        player.Update(0.1f, Direction.None, IsWalkable, null, GetMovementEffect);

        Assert.False(player.IsWindPushed);
        Assert.Equal(MovementState.Idle, player.State);
        Assert.Equal(5, player.X);
    }

    [Fact]
    public void MovementEffect_DoesNotSoftlock()
    {
        var player = new Player();
        player.Initialize(5, 5);

        // East is blocked (prevents push), but West (4, 5) is walkable
        bool IsWalkable(int x, int y) => x == 5 && y == 5 || x == 4 && y == 5;
        string GetMovementEffect(int x, int y) => x == 5 && y == 5 ? MovementEffect.PollenWindEast : MovementEffect.Normal;

        // Player is on wind tile, but push is blocked
        player.Update(0.1f, Direction.None, IsWalkable, null, GetMovementEffect);
        Assert.False(player.IsWindPushed);
        Assert.Equal(MovementState.Idle, player.State);

        // Now player presses Left to walk away
        player.Update(0.1f, Direction.Left, IsWalkable, null, GetMovementEffect);
        Assert.Equal(MovementState.Moving, player.State);
        Assert.Equal(4, player.TargetX);

        // Complete the walk step
        player.Update(0.1f, Direction.None, IsWalkable, null, GetMovementEffect);
        Assert.Equal(4, player.X);
        Assert.False(player.IsWindPushed);
        Assert.Equal(MovementState.Idle, player.State);
    }

    [Fact]
    public void InvalidPollenWindMetadata_FailsValidation()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "JoyMonWindTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        try
        {
            var mapJson = """
            {
              "id": "wind-test",
              "name": "Wind Test",
              "width": 3,
              "height": 2,
              "tileSize": 16,
              "tilesetId": "overworld",
              "spawnPoint": { "x": 0, "y": 0 },
              "layers": {
                "ground": [[1,1,1],[1,1,1]],
                "decoration": [[0,0,0],[0,0,0]],
                "collision": [[0,0,0],[0,0,0]],
                "movementEffect": [["normal","pollen_wind_northwest","normal"],["normal","normal","normal"]]
              }
            }
            """;
            File.WriteAllText(Path.Combine(tempDir, "wind-test.json"), mapJson);

            var ex = Assert.Throws<InvalidContentException>(() =>
                new MapLoader(tempDir).Load("wind-test.json"));

            Assert.Contains("movementEffect", ex.Message);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
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
