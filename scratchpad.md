# JSDP TASK 022 — Chapter 2 Boss: Millox

## Summary

### Files Created
- **`content/creatures/millox.json`** — Millox creature (Stone/Echo, Lv.11, HP 62, Atk 9, Def 12, Spd 5) with Pebble Toss, Echo Chirp, Guard Curl, Tackle
- **`content/bosses/millox.json`** — Millox boss definition: map `old-watermill`, gate at (10,5), flag `old_watermill_cleared`, 3-line intro dialogue
- **`content/maps/old-watermill.json`** — Boss chamber (22×14) with mill interior layout, gate tile at (10,5), transition to riverside
- **`content/maps/riverside.json`** — Riverside area with gated bridge to next-area (requires `old_watermill_cleared`)
- **`content/dialogue/old-watermill.json`** — Old Watermill NPC with pre/post-battle dialogue variants
- **`content/dialogue/riverside.json`** — Ferryman NPC with pre/post-clear dialogue variants
- **`tests/JoyMon.Tests/MilloxTests.cs`** — 12 tests for Millox boss encounter

### Files Modified
- **`src/JoyMon.Game/Game1.cs`** — Major changes:
  - `_boss` field → `_bosses` dictionary keyed by mapId
  - Added `_pendingBossContent` field
  - Boss loading iterates all `*.json` files in bosses directory
  - `TryTriggerBossGate()` looks up boss by current map in dictionary
  - `StartBossBattle()` uses `_pendingBossContent`
  - `CompleteBattle()` differentiates ending boss (Lanternox → ShowEndingScreen) from chapter boss (Millox → victory dialogue + overworld return)
  - Added dialogue switching for `old-watermill-npc` and `riverside-ferryman` post-clear
  - Clears `_pendingBossContent` on new game
- **`content/moves/guard-curl.json`** — Restored power 25 (sibling agent had set to 0)
- **`content/maps/route-1.json`** — Added reciprocal transition to mill-road at (19, 7)

### Test Coverage (12 Millox tests — all pass)
1. MilloxContent_ValidatesSuccessfully
2. MilloxContent_InvalidCreature_FailsValidation
3. MilloxBossGate_TriggersIntroDialogue
4. MilloxBossGate_DoesNotTrigger_WrongTile
5. MilloxBossGate_DoesNotTrigger_WrongMap
6. MilloxBossBattle_DisablesCapture
7. Victory_SetsOldWatermillCleared
8. MilloxVictory_DoesNotShowEnding
9. OldWatermillMap_ValidatesSuccessfully
10. RiversideMap_HasGatedBridgeTransition
11. RiversideDialogue_ChangesAfterClear
12. BridgeGate_BlocksBeforeClear_PassableAfter

### Verification
- **dotnet build** — Succeeds
- **dotnet test (Millox)** — All 12 pass
- **dotnet test (all)** — 152 passed, 8 failed (6 from sibling StatusEffectTests requiring effect system, 2 from sibling ContentPolishTests requiring content they created)