using JoyMon.Core;
using JoyMon.Game;

namespace JoyMon.Tests;

public class BattleSceneTests
{
    private static DeterministicRng AlwaysHitRng => new(0.0);

    [Fact]
    public void BattleScene_InitializesFromEncounter()
    {
        var player = MakeSpecies("Mossprout", baseHp: 30, baseAtk: 9, baseDef: 8, baseSpd: 12).CreateInstance(5);
        var wild = MakeSpecies("Queuebee", baseHp: 24, baseAtk: 7, baseDef: 6, baseSpd: 8).CreateInstance(2);

        var scene = new BattleScene(player, wild, AlwaysHitRng);

        Assert.Same(player, scene.State.PlayerJoyMon);
        Assert.Same(wild, scene.State.OpponentJoyMon);
        Assert.Equal(BattleSceneMode.Message, scene.Mode);
        Assert.Equal("Wild Queuebee appeared!", scene.CurrentMessage);
    }

    [Fact]
    public void FightCommand_AdvancesCoreBattleState()
    {
        var player = MakeSpecies("Mossprout", baseHp: 30, baseAtk: 20, baseDef: 8, baseSpd: 20).CreateInstance(5);
        var wild = MakeSpecies("Queuebee", baseHp: 40, baseAtk: 7, baseDef: 6, baseSpd: 8).CreateInstance(2);
        var scene = ReadyForCommand(new BattleScene(player, wild, AlwaysHitRng));

        scene.Confirm();
        Assert.Equal(BattleSceneMode.Fight, scene.Mode);

        scene.Confirm();

        Assert.True(scene.State.Events.Count > 0);
        Assert.True(wild.CurrentHp < wild.MaxHp);
    }

    [Fact]
    public void Win_UpdatesPlayerJoyMonXp()
    {
        var player = MakeSpecies("Mossprout", baseHp: 30, baseAtk: 80, baseDef: 8, baseSpd: 20).CreateInstance(5);
        var wild = MakeSpecies("Queuebee", baseHp: 10, baseAtk: 7, baseDef: 2, baseSpd: 8).CreateInstance(2);
        wild.CurrentHp = 1;
        var scene = ReadyForCommand(new BattleScene(player, wild, AlwaysHitRng));

        scene.Confirm();
        scene.Confirm();

        Assert.Equal(BattleSceneOutcome.Won, scene.Outcome);
        Assert.True(player.Xp > 0);
        Assert.Contains(scene.State.Events, e => e is BattleEvent.XpGained);
    }

    [Fact]
    public void Loss_SetsSafeRecoveryState()
    {
        var player = MakeSpecies("Mossprout", baseHp: 10, baseAtk: 4, baseDef: 1, baseSpd: 1).CreateInstance(1);
        var wild = MakeSpecies("Queuebee", baseHp: 20, baseAtk: 80, baseDef: 5, baseSpd: 20).CreateInstance(5);
        player.CurrentHp = 1;
        var scene = ReadyForCommand(new BattleScene(player, wild, AlwaysHitRng));

        scene.Confirm();
        scene.Confirm();

        Assert.Equal(BattleSceneOutcome.Lost, scene.Outcome);
        Assert.True(scene.RequiresSafeRecovery);
    }

    [Fact]
    public void Run_ExitsWildBattle()
    {
        var player = MakeSpecies("Mossprout", baseHp: 30, baseAtk: 9, baseDef: 8, baseSpd: 20).CreateInstance(5);
        var wild = MakeSpecies("Queuebee", baseHp: 24, baseAtk: 7, baseDef: 6, baseSpd: 8).CreateInstance(2);
        var scene = ReadyForCommand(new BattleScene(player, wild, AlwaysHitRng));

        scene.MoveDown();
        scene.Confirm();

        Assert.True(scene.State.IsOver);
        Assert.Equal(BattleSceneOutcome.Escaped, scene.Outcome);
    }

    [Fact]
    public void FightMenu_CannotSubmitInvalidMove()
    {
        var player = MakeSpecies("Mossprout", baseHp: 30, baseAtk: 9, baseDef: 8, baseSpd: 20).CreateInstance(5);
        var wild = MakeSpecies("Queuebee", baseHp: 24, baseAtk: 7, baseDef: 6, baseSpd: 8).CreateInstance(2);
        var scene = ReadyForCommand(new BattleScene(player, wild, AlwaysHitRng));

        scene.Confirm();
        var accepted = scene.TrySubmitMove(99);

        Assert.False(accepted);
        Assert.Equal(BattleSceneMode.Fight, scene.Mode);
        Assert.Empty(scene.State.Events);
        Assert.Equal(wild.MaxHp, wild.CurrentHp);
    }

    private static BattleScene ReadyForCommand(BattleScene scene)
    {
        while (scene.Mode == BattleSceneMode.Message)
        {
            scene.Confirm();
        }

        Assert.Equal(BattleSceneMode.Command, scene.Mode);
        return scene;
    }

    private static JoyMonSpecies MakeSpecies(string name, int baseHp, int baseAtk, int baseDef, int baseSpd)
    {
        var move = new MoveDefinition("test-hit", "Test Hit", JoyMonType.Neutral, 35, 100, 20);
        return new JoyMonSpecies(name, JoyMonType.Neutral, baseHp, baseAtk, baseDef, baseSpd, new[] { move });
    }
}
