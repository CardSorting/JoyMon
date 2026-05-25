using JoyMon.Content;
using JoyMon.Core;

namespace JoyMon.Game;

public enum BossGateTriggerResult
{
    None,
    StartIntroDialogue,
}

public static class BossInteraction
{
    public const string DefaultClearedFlag = "trial_grove_cleared";

    public static bool IsGateTile(BossContent boss, int tileX, int tileY) =>
        boss.GateTile.X == tileX && boss.GateTile.Y == tileY;

    public static bool IsCleared(PlayerProfile profile, BossContent boss) =>
        profile.HasFlag(boss.ClearedFlag);

    public static BossGateTriggerResult TryTriggerGate(
        BossContent boss,
        PlayerProfile profile,
        string currentMapId,
        int tileX,
        int tileY)
    {
        if (currentMapId != boss.MapId)
            return BossGateTriggerResult.None;

        if (IsCleared(profile, boss))
            return BossGateTriggerResult.None;

        if (!IsGateTile(boss, tileX, tileY))
            return BossGateTriggerResult.None;

        return BossGateTriggerResult.StartIntroDialogue;
    }

    public static void RecordVictory(PlayerProfile profile, BossContent boss) =>
        profile.SetFlag(boss.ClearedFlag, true);

    public static bool ShouldShowEnding(BattleSceneOutcome outcome, bool wasBossBattle) =>
        wasBossBattle && outcome == BattleSceneOutcome.Won;
}
