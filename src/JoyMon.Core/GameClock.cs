namespace JoyMon.Core;

/// <summary>
/// Pure domain clock — tracks total elapsed game time with no I/O or framework dependency.
/// </summary>
public class GameClock
{
    private TimeSpan _totalTime;

    public TimeSpan TotalTime => _totalTime;
    public bool IsRunning { get; private set; }

    public void Start() => IsRunning = true;

    public void Stop() => IsRunning = false;

    public void Reset()
    {
        _totalTime = TimeSpan.Zero;
        IsRunning = false;
    }

    /// <summary>
    /// Advances the clock by the given delta. Returns the new total time.
    /// </summary>
    public TimeSpan Tick(TimeSpan delta)
    {
        if (IsRunning)
            _totalTime += delta;
        return _totalTime;
    }
}