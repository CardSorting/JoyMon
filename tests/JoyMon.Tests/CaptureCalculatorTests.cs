using JoyMon.Core;

namespace JoyMon.Tests;

public class CaptureCalculatorTests
{
    [Fact]
    public void LowHp_IncreasesCaptureChance()
    {
        var wild = MakeSpecies().CreateInstance(5);
        var fullHpChance = CaptureCalculator.CalculateChance(wild, playerLevel: 5);

        wild.CurrentHp = 1;
        var lowHpChance = CaptureCalculator.CalculateChance(wild, playerLevel: 5);

        Assert.True(lowHpChance > fullHpChance);
    }

    [Fact]
    public void HigherWildLevel_DecreasesCaptureChance()
    {
        var equalLevelWild = MakeSpecies().CreateInstance(5);
        var higherLevelWild = MakeSpecies().CreateInstance(10);

        var equalLevelChance = CaptureCalculator.CalculateChance(equalLevelWild, playerLevel: 5);
        var higherLevelChance = CaptureCalculator.CalculateChance(higherLevelWild, playerLevel: 5);

        Assert.True(higherLevelChance < equalLevelChance);
    }

    [Fact]
    public void Chance_ClampsBetweenFiveAndNinetyFivePercent()
    {
        var easyWild = MakeSpecies().CreateInstance(1);
        easyWild.CurrentHp = -100;

        var hardWild = MakeSpecies().CreateInstance(100);

        Assert.Equal(0.95, CaptureCalculator.CalculateChance(easyWild, playerLevel: 100));
        Assert.Equal(0.10, CaptureCalculator.CalculateChance(hardWild, playerLevel: 1));
    }

    private static JoyMonSpecies MakeSpecies()
    {
        var move = new MoveDefinition("test-hit", "Test Hit", JoyMonType.Neutral, 35, 100, 20);
        return new JoyMonSpecies("Queuebee", JoyMonType.Neutral, 24, 7, 6, 8, new[] { move });
    }
}
