namespace JoyMon.Core;

public interface IRng
{
    int Next(int maxValue);
    int Next(int minValue, int maxValue);
    double NextDouble();
}

public class DefaultRng : IRng
{
    private readonly Random _random;

    public DefaultRng() => _random = new Random();
    public DefaultRng(int seed) => _random = new Random(seed);

    public int Next(int maxValue) => _random.Next(maxValue);
    public int Next(int minValue, int maxValue) => _random.Next(minValue, maxValue);
    public double NextDouble() => _random.NextDouble();
}
