using JoyMon.Content;

namespace JoyMon.Game;

public enum TrainerInteractionKind
{
    ShowDialogueOnly,
    ShowDialogueThenBattle,
}

public readonly record struct TrainerInteractionResult(
    TrainerInteractionKind Kind,
    TrainerDialogueContent Dialogue);

public static class TrainerInteraction
{
    public static TrainerInteractionResult Resolve(TrainerContent trainer, ISet<string> defeatedTrainerIds)
    {
        if (defeatedTrainerIds.Contains(trainer.Id))
            return new(TrainerInteractionKind.ShowDialogueOnly, trainer.DialogueAfter);

        return new(TrainerInteractionKind.ShowDialogueThenBattle, trainer.DialogueBefore);
    }

    public static bool CanStartBattle(TrainerContent trainer, ISet<string> defeatedTrainerIds) =>
        !defeatedTrainerIds.Contains(trainer.Id);

    public static void RecordDefeat(ISet<string> defeatedTrainerIds, string trainerId, BattleSceneOutcome outcome)
    {
        if (outcome == BattleSceneOutcome.Won)
            defeatedTrainerIds.Add(trainerId);
    }
}
