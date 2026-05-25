using JoyMon.Content;
using JoyMon.Core;

namespace JoyMon.Tests;

public class IceTileMovementTests
{
    private static readonly string ContentRoot = FindContentRoot();

    [Fact]
    public void IceTileMetadata_ValidatesSuccessfully()
    {
        var map = new MapLoader(Path.Combine(ContentRoot, "maps")).Load("frostveil-path.json");

        Assert.NotNull(map.Layers.MovementEffect);
        Assert.Equal(map.Height, map.Layers.MovementEffect.Count);
        Assert.Equal(map.Width, map.Layers.MovementEffect[0].Count);

        foreach (var row in map.Layers.MovementEffect)
        {
            foreach (var effect in row)
                Assert.True(MovementEffect.IsValid(effect));
        }

        Assert.Contains(map.Layers.MovementEffect.SelectMany(r => r), e => e == MovementEffect.Ice);
        Assert.Contains(map.Layers.MovementEffect.SelectMany(r => r), e => e == MovementEffect.Normal);
    }

    [Fact]
    public void Player_SlidesAcrossConsecutiveIceTiles()
    {
        var player = new Player();
        player.Initialize(2, 5);
        player.Facing = Direction.Right;

        bool IsWalkable(int x, int y) => x >= 0 && x <= 5 && y == 5;
        bool IsIce(int x, int y) => x >= 3 && x <= 5 && y == 5;

        // Walk onto first ice tile, then auto-slide
        player.Update(0.2f, Direction.Right, IsWalkable, IsIce);
        Assert.Equal(3, player.X);
        Assert.True(player.IsSliding);

        player.Update(0.2f, Direction.None, IsWalkable, IsIce);
        Assert.Equal(4, player.X);
        Assert.True(player.IsSliding);

        player.Update(0.2f, Direction.None, IsWalkable, IsIce);
        Assert.Equal(5, player.X);
        Assert.False(player.IsSliding);
    }

    [Fact]
    public void Player_StopsBeforeBlockedTile()
    {
        var player = new Player();
        player.Initialize(2, 5);
        player.Facing = Direction.Right;

        bool IsWalkable(int x, int y) => x <= 4 && y == 5;
        bool IsIce(int x, int y) => x >= 3 && x <= 5 && y == 5;

        player.Update(0.2f, Direction.Right, IsWalkable, IsIce);
        player.Update(0.2f, Direction.None, IsWalkable, IsIce);

        Assert.Equal(4, player.X);
        Assert.False(player.IsSliding);
        Assert.Equal(MovementState.Idle, player.State);
    }

    [Fact]
    public void Player_ExitsSlideOnNonIceTile()
    {
        var player = new Player();
        player.Initialize(2, 5);
        player.Facing = Direction.Right;

        bool IsWalkable(int x, int y) => x <= 5 && y == 5;
        bool IsIce(int x, int y) => x >= 3 && x <= 4 && y == 5;

        player.Update(0.2f, Direction.Right, IsWalkable, IsIce);
        player.Update(0.2f, Direction.None, IsWalkable, IsIce);
        player.Update(0.2f, Direction.None, IsWalkable, IsIce);

        Assert.Equal(5, player.X);
        Assert.False(player.IsSliding);
        Assert.False(IsIce(5, 5));
    }

    [Fact]
    public void Player_InputIgnoredWhileSliding()
    {
        var player = new Player();
        player.Initialize(2, 5);
        player.Facing = Direction.Right;

        bool IsWalkable(int x, int y) => x >= 0 && x <= 5 && y == 5;
        bool IsIce(int x, int y) => x >= 3 && x <= 5 && y == 5;

        player.Update(0.2f, Direction.Right, IsWalkable, IsIce);
        Assert.Equal(3, player.X);
        Assert.True(player.IsSliding);

        // Opposite input should not change facing or slide direction
        player.Update(0.2f, Direction.Left, IsWalkable, IsIce);
        Assert.Equal(4, player.X);
        Assert.Equal(Direction.Right, player.Facing);
    }

    [Fact]
    public void InvalidMovementEffect_FailsValidation()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "JoyMonIceTest_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        try
        {
            var mapJson = """
            {
              "id": "ice-test",
              "name": "Ice Test",
              "width": 3,
              "height": 2,
              "tileSize": 16,
              "tilesetId": "overworld",
              "spawnPoint": { "x": 0, "y": 0 },
              "layers": {
                "ground": [[1,1,1],[1,1,1]],
                "decoration": [[0,0,0],[0,0,0]],
                "collision": [[0,0,0],[0,0,0]],
                "movementEffect": [["normal","slippery","normal"],["ice","normal","normal"]]
              }
            }
            """;
            File.WriteAllText(Path.Combine(tempDir, "ice-test.json"), mapJson);

            var ex = Assert.Throws<InvalidContentException>(() =>
                new MapLoader(tempDir).Load("ice-test.json"));

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
