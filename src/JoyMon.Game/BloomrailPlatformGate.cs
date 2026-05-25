using JoyMon.Core;
using System.Collections.Generic;
using System.Linq;

namespace JoyMon.Game;

public static class BloomrailPlatformGate
{
    public const string SolvedFlag = "bloomrail_fields_solved";
    public const int RequiredDefeatedTrainerCount = 2;

    public static readonly IReadOnlySet<string> TrainerIds = new HashSet<string>
    {
        "fields-keeper-sera",
        "fields-railhand-tiko",
        "fields-chef-loma",
    };

    public static int CountDefeated(IEnumerable<string> defeatedTrainerIds) =>
        defeatedTrainerIds.Count(TrainerIds.Contains);

    public static bool HasRequirement(IEnumerable<string> defeatedTrainerIds) =>
        CountDefeated(defeatedTrainerIds) >= RequiredDefeatedTrainerCount;

    public static bool RefreshProgress(PlayerProfile profile, IEnumerable<string> defeatedTrainerIds)
    {
        if (!HasRequirement(defeatedTrainerIds))
            return profile.HasFlag(SolvedFlag);

        profile.SetFlag(SolvedFlag, true);
        return true;
    }

    public static bool IsSolved(PlayerProfile profile, IEnumerable<string> defeatedTrainerIds) =>
        profile.HasFlag(SolvedFlag) || HasRequirement(defeatedTrainerIds);

    public static string ResolveConductorDialogueId(
        PlayerProfile profile,
        IEnumerable<string> defeatedTrainerIds,
        string defaultDialogueId)
    {
        if (profile.HasFlag("abandoned_train_cleared"))
            return $"{defaultDialogueId}-cleared";
        return IsSolved(profile, defeatedTrainerIds)
            ? $"{defaultDialogueId}-solved"
            : defaultDialogueId;
    }
}
