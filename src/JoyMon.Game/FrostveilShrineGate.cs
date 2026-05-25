using JoyMon.Core;

namespace JoyMon.Game;

public static class FrostveilShrineGate
{
    public const string UnlockedFlag = "frostveil_shrine_unlocked";
    public const int RequiredDefeatedTrainerCount = 2;

    public static readonly IReadOnlySet<string> TrainerIds = new HashSet<string>
    {
        "lodge-runner-mina",
        "static-skater-orro",
        "moss-hermit-pela",
    };

    public static int CountDefeated(IEnumerable<string> defeatedTrainerIds) =>
        defeatedTrainerIds.Count(TrainerIds.Contains);

    public static bool HasRequirement(IEnumerable<string> defeatedTrainerIds) =>
        CountDefeated(defeatedTrainerIds) >= RequiredDefeatedTrainerCount;

    public static bool RefreshProgress(PlayerProfile profile, IEnumerable<string> defeatedTrainerIds)
    {
        if (!HasRequirement(defeatedTrainerIds))
            return profile.HasFlag(UnlockedFlag);

        profile.SetFlag(UnlockedFlag, true);
        return true;
    }

    public static bool IsUnlocked(PlayerProfile profile, IEnumerable<string> defeatedTrainerIds) =>
        profile.HasFlag(UnlockedFlag) || HasRequirement(defeatedTrainerIds);

    public static string ResolveKeeperDialogueId(
        PlayerProfile profile,
        IEnumerable<string> defeatedTrainerIds,
        string defaultDialogueId)
    {
        if (profile.HasFlag(ShrineFlags.Cleared))
            return $"{defaultDialogueId}-cleared";
        return IsUnlocked(profile, defeatedTrainerIds)
            ? $"{defaultDialogueId}-unlocked"
            : defaultDialogueId;
    }
}
