using JoyMon.Core;

namespace JoyMon.Game;

public sealed class EndingScreenData
{
    public IReadOnlyList<JoyMonInstance> Party { get; init; } = Array.Empty<JoyMonInstance>();
    public IReadOnlyList<string> Captures { get; init; } = Array.Empty<string>();
    public double PlayTimeSeconds { get; init; }
}
