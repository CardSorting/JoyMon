namespace JoyMon.Core;

public enum CaptureResult
{
    Success,
    Failed,
    PartyFull,
}

public sealed class CaptureCalculator
{
    public const double MinimumChance = 0.10;
    public const double MaximumChance = 0.95;

    private readonly IBattleRng _rng;

    public CaptureCalculator(IBattleRng rng)
    {
        _rng = rng ?? throw new ArgumentNullException(nameof(rng));
    }

    public static double CalculateChance(JoyMonInstance wildJoyMon, int playerLevel)
    {
        ArgumentNullException.ThrowIfNull(wildJoyMon);

        const double baseChance = 0.45;
        double hpRatio = wildJoyMon.MaxHp <= 0
            ? 1.0
            : (double)wildJoyMon.CurrentHp / wildJoyMon.MaxHp;
        double hpBonus = 0.4 * (1.0 - hpRatio);
        double levelPenalty = Math.Max(0.0, (wildJoyMon.Level - playerLevel) * 0.02);
        double finalChance = baseChance + hpBonus - levelPenalty;

        return Math.Clamp(finalChance, MinimumChance, MaximumChance);
    }

    public CaptureResult TryCapture(
        JoyMonInstance wildJoyMon,
        int playerLevel,
        int partyCount,
        int partyLimit)
    {
        if (partyCount >= partyLimit)
            return CaptureResult.PartyFull;

        double chance = CalculateChance(wildJoyMon, playerLevel);
        return _rng.NextDouble() < chance
            ? CaptureResult.Success
            : CaptureResult.Failed;
    }
}
