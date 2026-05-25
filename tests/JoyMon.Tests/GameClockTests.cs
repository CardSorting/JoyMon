using JoyMon.Core;

namespace JoyMon.Tests;

public class GameClockTests
{
    [Fact]
    public void Tick_WhenRunning_AdvancesTotalTime()
    {
        var clock = new GameClock();
        clock.Start();

        clock.Tick(TimeSpan.FromSeconds(1.5));

        Assert.Equal(TimeSpan.FromSeconds(1.5), clock.TotalTime);
    }

    [Fact]
    public void Tick_WhenStopped_DoesNotAdvance()
    {
        var clock = new GameClock();

        clock.Tick(TimeSpan.FromSeconds(5));

        Assert.Equal(TimeSpan.Zero, clock.TotalTime);
    }

    [Fact]
    public void Reset_SetsTimeToZeroAndStops()
    {
        var clock = new GameClock();
        clock.Start();
        clock.Tick(TimeSpan.FromSeconds(3));
        clock.Reset();

        Assert.Equal(TimeSpan.Zero, clock.TotalTime);
        Assert.False(clock.IsRunning);
    }
}