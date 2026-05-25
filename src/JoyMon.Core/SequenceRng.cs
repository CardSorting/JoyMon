namespace JoyMon.Core;

/// <summary>
/// Deterministic RNG that returns values from a sequence. Used when a single turn
/// needs multiple independent rolls (accuracy, status chance, etc.).
/// </summary>
public sealed class SequenceRng : IBattleRng
{
    private readonly double[] _values;
    private int _index;

    public SequenceRng(params double[] values)
    {
        _values = values.Length > 0 ? values : new[] { 0.0 };
    }

    public double NextDouble()
    {
        var value = _values[Math.Min(_index, _values.Length - 1)];
        if (_index < _values.Length - 1)
            _index++;
        return value;
    }
}
