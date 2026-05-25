namespace JoyMon.Core;

/// <summary>
/// Injected random number source for deterministic battle resolution.
/// No static or global RNG — always injected.
/// </summary>
public interface IBattleRng
{
    /// <summary>
    /// Returns a random double in [0.0, 1.0).
    /// </summary>
    double NextDouble();
}

/// <summary>
/// Deterministic RNG that always returns a fixed value. Used for testing.
/// </summary>
public sealed class DeterministicRng : IBattleRng
{
    private readonly double _value;
    public DeterministicRng(double value) => _value = value;
    public double NextDouble() => _value;
}