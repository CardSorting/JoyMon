using JoyMon.Content;
using JoyMon.Core;
using JoyMon.Game;

namespace JoyMon.Tests;

public class TrainerBattleTests
{
    private static DeterministicRng AlwaysHitRng => new(0.0);

    [Fact]
    public void TrainerInteraction_Undefeated_StartsBattleAfterDialogue()
    {
        var trainer = CreateTrainer();
        var defeated = new HashSet<string>();

        var result = TrainerInteraction.Resolve(trainer, defeated);

        Assert.Equal(TrainerInteractionKind.ShowDialogueThenBattle, result.Kind);
        Assert.Equal("Kai", result.Dialogue.Speaker);
        Assert.True(TrainerInteraction.CanStartBattle(trainer, defeated));
    }

    [Fact]
    public void TrainerInteraction_Defeated_ShowsAfterDialogueOnly()
    {
        var trainer = CreateTrainer();
        var defeated = new HashSet<string> { "route-1-rival" };

        var result = TrainerInteraction.Resolve(trainer, defeated);

        Assert.Equal(TrainerInteractionKind.ShowDialogueOnly, result.Kind);
        Assert.Contains("Good fight", result.Dialogue.Lines[0]);
        Assert.False(TrainerInteraction.CanStartBattle(trainer, defeated));
    }

    [Fact]
    public void TrainerBattle_DisablesRunAndCapture()
    {
        var player = MakeSpecies("Mossprout", baseHp: 30, baseAtk: 20, baseDef: 8, baseSpd: 20).CreateInstance(5);
        var opponent = MakeSpecies("Queuebee", baseHp: 24, baseAtk: 7, baseDef: 6, baseSpd: 8).CreateInstance(4);
        var scene = ReadyForCommand(new BattleScene(player, opponent, AlwaysHitRng, isTrainerBattle: true, opponentTrainerName: "Kai"));

        Assert.True(scene.IsTrainerBattle);
        Assert.False(scene.CanRun);
        Assert.False(scene.CanCapture);
        Assert.Equal(2, scene.Commands.Count);
        Assert.DoesNotContain("Run", scene.Commands);
        Assert.DoesNotContain("Capture", scene.Commands);
    }

    [Fact]
    public void TrainerBattle_CannotEscape()
    {
        var player = MakeSpecies("Mossprout", baseHp: 30, baseAtk: 9, baseDef: 8, baseSpd: 20).CreateInstance(5);
        var opponent = MakeSpecies("Queuebee", baseHp: 24, baseAtk: 7, baseDef: 6, baseSpd: 8).CreateInstance(4);
        var scene = ReadyForCommand(new BattleScene(player, opponent, AlwaysHitRng, isTrainerBattle: true));

        Assert.Null(scene.TryCapture());
        Assert.Equal(BattleSceneOutcome.None, scene.Outcome);
        Assert.False(scene.State.IsOver);
    }

    [Fact]
    public void RecordDefeat_PersistsTrainerIdOnWin()
    {
        var defeated = new HashSet<string>();

        TrainerInteraction.RecordDefeat(defeated, "route-1-rival", BattleSceneOutcome.Won);

        Assert.Contains("route-1-rival", defeated);
    }

    [Fact]
    public void RecordDefeat_DoesNotPersistOnLossOrEscape()
    {
        var defeated = new HashSet<string>();

        TrainerInteraction.RecordDefeat(defeated, "route-1-rival", BattleSceneOutcome.Lost);
        TrainerInteraction.RecordDefeat(defeated, "route-1-rival", BattleSceneOutcome.Escaped);

        Assert.Empty(defeated);
    }

    [Fact]
    public void TrainerBattle_StartsWithTrainerIntro()
    {
        var player = MakeSpecies("Mossprout", baseHp: 30, baseAtk: 9, baseDef: 8, baseSpd: 20).CreateInstance(5);
        var opponent = MakeSpecies("Queuebee", baseHp: 24, baseAtk: 7, baseDef: 6, baseSpd: 8).CreateInstance(4);
        var scene = new BattleScene(player, opponent, AlwaysHitRng, isTrainerBattle: true, opponentTrainerName: "Kai");

        Assert.Contains("Kai wants to battle!", scene.CurrentMessage);
    }

    private static TrainerContent CreateTrainer() => new()
    {
        Id = "route-1-rival",
        DisplayName = "Kai",
        MapId = "route-1",
        DialogueBefore = new TrainerDialogueContent
        {
            Speaker = "Kai",
            Lines = new List<string> { "Let's battle!" },
        },
        DialogueAfter = new TrainerDialogueContent
        {
            Speaker = "Kai",
            Lines = new List<string> { "Good fight!" },
        },
        Party = new List<TrainerPartyMemberContent>
        {
            new() { CreatureId = "queuebee", Level = 4 },
        },
    };

    private static BattleScene ReadyForCommand(BattleScene scene)
    {
        while (scene.Mode == BattleSceneMode.Message)
            scene.Confirm();

        Assert.Equal(BattleSceneMode.Command, scene.Mode);
        return scene;
    }

    private static JoyMonSpecies MakeSpecies(string name, int baseHp, int baseAtk, int baseDef, int baseSpd)
    {
        var move = new MoveDefinition("test-hit", "Test Hit", JoyMonType.Neutral, 35, 100, 20);
        return new JoyMonSpecies(name, JoyMonType.Neutral, baseHp, baseAtk, baseDef, baseSpd, new[] { move });
    }
}
