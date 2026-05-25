# JSDP TASK 016 — Complete

## Summary

### Files Created
- **`content/creatures/glimmoo.json`** — New creature (Moss-type, HP 46, balanced stats)
- **`content/encounters/trial-grove.json`** — Encounter table: Rootsnail (40%), Queuebee (35%), Glimmoo (25%), levels 3-5, 12% encounter rate on grass tiles
- **`content/maps/trial-grove.json`** — Maze map (22×14) with winding corridors from entrance (10,12) → healer alcove at (4,8) → boss gate at (10,1)
- **`content/dialogue/trial-grove.json`** — Elder Willow NPC at (4,8) with healing dialogue
- **`tests/JoyMon.Tests/TrialGroveTests.cs`** — 13 tests covering map validation, transitions, healing, boss gate, encounters, and dialogue

### Files Modified
- **`src/JoyMon.Game/Game1.cs`** — Dialogue loader now iterates all `*.json` files in the dialogue directory instead of hardcoding `starter-town.json` only

### Already In Place (no changes needed)
- `content/maps/route-1.json` already had the north transition to Trial Grove at (10, 0)
- `src/JoyMon.Game/Game1.cs` already had `trial-grove-healer` NPC integration with `HealParty()` call
- `content/bosses/lanternox-trial.json` already defines boss gate at (10, 1) on trial-grove

### Verification
- **dotnet build** — Succeeds with 0 errors
- **dotnet test** — All 117 tests pass (104 pre-existing + 13 new Trial Grove tests)

### Test Coverage
1. TrialGroveMap_ValidatesSuccessfully — Map JSON loads and validates
2. Route1_HasNorthTransitionToTrialGrove — Route 1 → Trial Grove transition exists
3. TrialGrove_HasSouthTransitionToRoute1 — Trial Grove → Route 1 return transition exists
4. Player_CanTransitionFromRoute1_ToTrialGrove — Walkability + transition trigger works
5. HealParty_RestoresAllJoyMonHP — Healing restores full HP
6. HealParty_RestoresMoveUses — Healing restores full PP/uses
7. BossGate_OnTrialGrove_TriggersIntroDialogue — Boss gate triggers intro dialogue
8. BossGate_DoesNotTrigger_WrongMap — Boss gate doesn't trigger off-map
9. BossGate_DoesNotTrigger_AfterCleared — Boss gate doesn't trigger when cleared
10. TrialGroveEncounterTable_LoadsSuccessfully — Encounter table loads with correct entries
11. TrialGroveEncounter_InvalidCreature_FailsValidation — Invalid creature reference rejected
12. PlayerCanWalk_FromEntrance_ToBossGatePosition — Key tiles (spawn, gate, exit) are walkable
13. TrialGroveDialogue_LoadsSuccessfully — Dialogue file with NPC definition validates