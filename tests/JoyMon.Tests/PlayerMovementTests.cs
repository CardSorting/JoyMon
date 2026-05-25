using JoyMon.Core;

namespace JoyMon.Tests;

public class PlayerMovementTests
{
    [Fact]
    public void Player_CanMoveIntoWalkableTile()
    {
        var player = new Player();
        player.Initialize(5, 5);

        // walkable tile is true
        player.Update(0.1f, Direction.Right, (x, y) => true);

        Assert.Equal(5, player.X); // still interpolating
        Assert.Equal(6, player.TargetX);
        Assert.Equal(MovementState.Moving, player.State);
        Assert.Equal(Direction.Right, player.Facing);
    }

    [Fact]
    public void Player_CannotMoveIntoBlockedTile()
    {
        var player = new Player();
        player.Initialize(5, 5);

        // blocked tile is false
        player.Update(0.1f, Direction.Right, (x, y) => false);

        Assert.Equal(5, player.X);
        Assert.Equal(5, player.TargetX);
        Assert.Equal(MovementState.Idle, player.State);
        Assert.Equal(Direction.Right, player.Facing); // Facing still updates!
    }

    [Fact]
    public void Player_CannotMoveOutsideMapBounds()
    {
        var player = new Player();
        player.Initialize(0, 0);

        // Map size 10x10. Delegate checks bounds.
        bool IsWalkable(int x, int y) => x >= 0 && x < 10 && y >= 0 && y < 10;

        player.Update(0.1f, Direction.Left, IsWalkable);

        Assert.Equal(0, player.X);
        Assert.Equal(0, player.TargetX);
        Assert.Equal(MovementState.Idle, player.State);
        Assert.Equal(Direction.Left, player.Facing);
    }

    [Fact]
    public void Player_FacingDirectionUpdatesEvenWhenBlocked()
    {
        var player = new Player();
        player.Initialize(5, 5);

        player.Update(0.1f, Direction.Up, (x, y) => false);

        Assert.Equal(Direction.Up, player.Facing);
        Assert.Equal(MovementState.Idle, player.State);
    }

    [Fact]
    public void Player_MovementCompletesAfterExpectedDuration()
    {
        var player = new Player();
        player.Initialize(5, 5);

        // Step 1: Start moving (duration: 0.2s)
        player.Update(0.05f, Direction.Down, (x, y) => true);
        Assert.Equal(MovementState.Moving, player.State);
        Assert.Equal(5, player.Y);
        Assert.Equal(6, player.TargetY);

        // Step 2: Update with remaining time (total 0.2f needed)
        player.Update(0.15f, Direction.None, (x, y) => true);
        Assert.Equal(MovementState.Idle, player.State);
        Assert.Equal(6, player.Y);
        Assert.Equal(6, player.TargetY);
        Assert.Equal(1.0f, player.MoveProgress);
    }
}
