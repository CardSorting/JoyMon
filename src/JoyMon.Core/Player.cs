namespace JoyMon.Core;

public enum Direction
{
    None,
    Up,
    Down,
    Left,
    Right
}

public enum MovementState
{
    Idle,
    Moving
}

/// <summary>
/// Exposes player coordinates, facing direction, state, and interpolation progress.
/// Movement checks are performed using a delegate/callback to decouple from MonoGame content models.
/// </summary>
public class Player
{
    public const float MoveDuration = 0.2f; // 200 ms per tile step

    public int X { get; set; }
    public int Y { get; set; }
    public int TargetX { get; set; }
    public int TargetY { get; set; }
    public Direction Facing { get; set; } = Direction.Down;
    public MovementState State { get; set; } = MovementState.Idle;
    public float MoveProgress { get; set; } = 1.0f;

    public void Initialize(int x, int y)
    {
        X = x;
        Y = y;
        TargetX = x;
        TargetY = y;
        Facing = Direction.Down;
        State = MovementState.Idle;
        MoveProgress = 1.0f;
    }

    public void Update(float deltaTime, Direction inputDir, Func<int, int, bool> isWalkable)
    {
        if (State == MovementState.Idle)
        {
            TryStartMove(inputDir, isWalkable);
        }

        if (State == MovementState.Moving)
        {
            MoveProgress += deltaTime / MoveDuration;
            if (MoveProgress >= 1.0f)
            {
                X = TargetX;
                Y = TargetY;
                State = MovementState.Idle;
                MoveProgress = 1.0f;

                // Chain next step immediately if input direction is held
                TryStartMove(inputDir, isWalkable);
            }
        }
    }

    private void TryStartMove(Direction dir, Func<int, int, bool> isWalkable)
    {
        if (dir == Direction.None) return;

        Facing = dir; // Facing direction updates even when blocked

        int dx = 0;
        int dy = 0;
        switch (dir)
        {
            case Direction.Up: dy = -1; break;
            case Direction.Down: dy = 1; break;
            case Direction.Left: dx = -1; break;
            case Direction.Right: dx = 1; break;
        }

        int tx = X + dx;
        int ty = Y + dy;

        if (isWalkable(tx, ty))
        {
            TargetX = tx;
            TargetY = ty;
            State = MovementState.Moving;
            MoveProgress = 0.0f;
        }
    }
}
