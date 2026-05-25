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
    public bool IsSliding { get; private set; }
    public bool IsWindPushed { get; private set; }

    public void Initialize(int x, int y)
    {
        X = x;
        Y = y;
        TargetX = x;
        TargetY = y;
        Facing = Direction.Down;
        State = MovementState.Idle;
        MoveProgress = 1.0f;
        IsSliding = false;
        IsWindPushed = false;
    }

    public void Update(
        float deltaTime,
        Direction inputDir,
        Func<int, int, bool> isWalkable,
        Func<int, int, bool>? isIce = null,
        Func<int, int, string>? getMovementEffect = null)
    {
        if (State == MovementState.Idle)
        {
            TryApplyWindPush(isWalkable, getMovementEffect);

            if (State == MovementState.Idle && !IsSliding && !IsWindPushed)
            {
                TryStartMove(inputDir, isWalkable);
            }
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

                TryContinueSlide(isWalkable, isIce);

                if (!IsSliding)
                {
                    TryApplyWindPush(isWalkable, getMovementEffect);
                }

                if (State == MovementState.Idle && !IsSliding && !IsWindPushed)
                    TryStartMove(inputDir, isWalkable);
            }
        }
    }

    private void TryApplyWindPush(Func<int, int, bool> isWalkable, Func<int, int, string>? getMovementEffect)
    {
        if (getMovementEffect is null)
        {
            IsWindPushed = false;
            return;
        }

        var effect = getMovementEffect(X, Y);
        if (string.IsNullOrEmpty(effect))
        {
            IsWindPushed = false;
            return;
        }

        Direction pushDir = Direction.None;
        if (string.Equals(effect, "pollen_wind_north", StringComparison.OrdinalIgnoreCase)) pushDir = Direction.Up;
        else if (string.Equals(effect, "pollen_wind_south", StringComparison.OrdinalIgnoreCase)) pushDir = Direction.Down;
        else if (string.Equals(effect, "pollen_wind_east", StringComparison.OrdinalIgnoreCase)) pushDir = Direction.Right;
        else if (string.Equals(effect, "pollen_wind_west", StringComparison.OrdinalIgnoreCase)) pushDir = Direction.Left;

        if (pushDir == Direction.None)
        {
            IsWindPushed = false;
            return;
        }

        if (!TryGetOffset(X, Y, pushDir, out int tx, out int ty))
        {
            IsWindPushed = false;
            return;
        }

        if (!isWalkable(tx, ty))
        {
            IsWindPushed = false;
            return;
        }

        Facing = pushDir;
        TargetX = tx;
        TargetY = ty;
        State = MovementState.Moving;
        MoveProgress = 0.0f;
        IsWindPushed = true;
    }

    private void TryContinueSlide(Func<int, int, bool> isWalkable, Func<int, int, bool>? isIce)
    {
        if (isIce is null)
        {
            IsSliding = false;
            return;
        }

        if (IsSliding && !isIce(X, Y))
        {
            IsSliding = false;
            return;
        }

        if (!isIce(X, Y))
            return;

        IsSliding = true;

        if (!TryGetOffset(X, Y, Facing, out int tx, out int ty))
            return;

        if (!isWalkable(tx, ty))
        {
            IsSliding = false;
            return;
        }

        TargetX = tx;
        TargetY = ty;
        State = MovementState.Moving;
        MoveProgress = 0.0f;
    }

    private void TryStartMove(Direction dir, Func<int, int, bool> isWalkable)
    {
        if (dir == Direction.None) return;

        Facing = dir; // Facing direction updates even when blocked

        if (!TryGetOffset(X, Y, dir, out int tx, out int ty))
            return;

        if (isWalkable(tx, ty))
        {
            TargetX = tx;
            TargetY = ty;
            State = MovementState.Moving;
            MoveProgress = 0.0f;
        }
    }

    private static bool TryGetOffset(int x, int y, Direction dir, out int tx, out int ty)
    {
        tx = x;
        ty = y;
        switch (dir)
        {
            case Direction.Up: ty = y - 1; return true;
            case Direction.Down: ty = y + 1; return true;
            case Direction.Left: tx = x - 1; return true;
            case Direction.Right: tx = x + 1; return true;
            default: return false;
        }
    }
}
