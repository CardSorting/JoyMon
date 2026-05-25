using JoyMon.Core;

namespace JoyMon.Tests;

public class BattleSystemTests
{
    // ── Helpers ─────────────────────────────────────────────────

    /// <summary>RNG that always returns 0.0 → all accuracy rolls are 0, so any
    /// accuracy > 0 hits and accuracy = 0 misses.</summary>
    private static DeterministicRng AlwaysHitRng => new(0.0);

    /// <summary>RNG that always returns 1.0 → all accuracy rolls = 100, so any
    /// accuracy <= 100 misses.</summary>
    private static DeterministicRng AlwaysMissRng => new(1.0);

    /// <summary>Helper to create a custom species with one move for edge-case tests.</summary>
    private static JoyMonSpecies MakeSpecies(string name, int baseHp, int baseAtk,
        int baseDef, int baseSpd, int movePower = 10, int moveAcc = 100)
    {
        var move = new MoveDefinition("move_0", "Test Move", JoyMonType.Neutral,
            movePower, moveAcc, 20);
        return new JoyMonSpecies(name, JoyMonType.Neutral,
            baseHp, baseAtk, baseDef, baseSpd, new[] { move });
    }

    // ── 1. Faster JoyMon acts first ─────────────────────────────

    [Fact]
    public void FasterJoyMon_ActsFirst()
    {
        // Spark (Speed 27) is faster than Stone (Speed 19) at level 15
        var player = SpeciesLibrary.Spark.CreateInstance(15);
        var opponent = SpeciesLibrary.Stone.CreateInstance(15);
        var state = new BattleState(player, opponent);
        var sys = new BattleSystem(AlwaysHitRng);

        sys.ExecuteTurn(state, new BattleCommand.Fight(0));

        // First event should be player's move (Spark, Quick Sparks)
        // Second event should be damage dealt by Spark
        // Third event should be opponent's move (Stone, Rock Toss)
        Assert.Equal("Spark", Assert.IsType<BattleEvent.MoveUsed>(state.Events[0]).SpeciesName);
        Assert.Equal("Stone", Assert.IsType<BattleEvent.MoveUsed>(state.Events[2]).SpeciesName);
    }

    // ── 2. Slower JoyMon acts second ────────────────────────────

    [Fact]
    public void SlowerJoyMon_ActsSecond()
    {
        // Player is Stone (Speed 19), opponent is Spark (Speed 27)
        var player = SpeciesLibrary.Stone.CreateInstance(15);
        var opponent = SpeciesLibrary.Spark.CreateInstance(15);
        var state = new BattleState(player, opponent);
        var sys = new BattleSystem(AlwaysHitRng);

        sys.ExecuteTurn(state, new BattleCommand.Fight(0));

        // Opponent's MoveUsed should come first
        Assert.Equal("Spark", Assert.IsType<BattleEvent.MoveUsed>(state.Events[0]).SpeciesName);
        Assert.Equal("Stone", Assert.IsType<BattleEvent.MoveUsed>(state.Events[2]).SpeciesName);
    }

    // ── 3. Damage is deterministic ──────────────────────────────

    [Fact]
    public void Damage_IsDeterministic()
    {
        // Level 15 Spark uses Thunder Jolt (power 45) on Level 15 Stone (def 25)
        // damage = max(1, ((21 * 45) / 25) / 4) = max(1, (945 / 25) / 4)
        //       = max(1, 37 / 4) = max(1, 9) = 9
        var player = SpeciesLibrary.Spark.CreateInstance(15);
        var opponent = SpeciesLibrary.Stone.CreateInstance(15);
        var state = new BattleState(player, opponent);
        var sys = new BattleSystem(AlwaysHitRng);

        sys.ExecuteTurn(state, new BattleCommand.Fight(0)); // Thunder Jolt

        var dmgEvent = Assert.IsType<BattleEvent.DamageDealt>(state.Events[1]);
        Assert.Equal(9, dmgEvent.Damage);
    }

    // ── 4. Damage never goes below 1 ────────────────────────────

    [Fact]
    public void Damage_NeverBelowOne()
    {
        // Use a custom super-fast species so player always goes first.
        // Player atk=1, power=1 vs opponent def=999 → damage floor of 1.
        var lowDmg = new MoveDefinition("poke", "Poke", JoyMonType.Neutral, 1, 100, 10);
        var fastSpecies = new JoyMonSpecies("Fast", JoyMonType.Neutral, 50, 1, 1, 999, new[] { lowDmg });
        var tankSpecies = new JoyMonSpecies("Tank", JoyMonType.Neutral, 999, 1, 999, 1, new[] { lowDmg });

        var player = fastSpecies.CreateInstance(5);
        var opponent = tankSpecies.CreateInstance(5);
        var state = new BattleState(player, opponent);
        var sys = new BattleSystem(AlwaysHitRng);

        sys.ExecuteTurn(state, new BattleCommand.Fight(0));

        // Player's DamageDealt should be at index 1 (MoveUsed[0], DamageDealt[1])
        // damage = max(1, ((1+5) * 1) / max(1, 1+5) / 4 = 6/6/4 = 0/4 = 0 → 1)
        var dmgEvent = Assert.IsType<BattleEvent.DamageDealt>(state.Events[1]);
        Assert.Equal(1, dmgEvent.Damage);
    }

    // ── 5. Accuracy 0 always misses ─────────────────────────────

    [Fact]
    public void AccuracyZero_AlwaysMisses()
    {
        var missMove = new MoveDefinition("miss", "Missile", JoyMonType.Neutral, 1, 0, 10);
        var missSpecies = new JoyMonSpecies("MissBot", JoyMonType.Neutral, 50, 10, 10, 10, new[] { missMove });
        var player = missSpecies.CreateInstance(5);
        var opponent = missSpecies.CreateInstance(5);
        var state = new BattleState(player, opponent);
        var sys = new BattleSystem(AlwaysHitRng); // RNG returns 0 → roll = 0 → 0 >= 0 is true → miss!

        sys.ExecuteTurn(state, new BattleCommand.Fight(0));

        // MoveUsed followed by MoveMissed, no DamageDealt
        Assert.IsType<BattleEvent.MoveUsed>(state.Events[0]);
        Assert.IsType<BattleEvent.MoveMissed>(state.Events[1]);
        Assert.DoesNotContain(state.Events, e => e is BattleEvent.DamageDealt);
    }

    // ── 6. Accuracy 100 always hits ─────────────────────────────

    [Fact]
    public void Accuracy100_AlwaysHits()
    {
        var player = SpeciesLibrary.Spark.CreateInstance(5);
        var opponent = SpeciesLibrary.Stone.CreateInstance(5);
        var state = new BattleState(player, opponent);
        var sys = new BattleSystem(AlwaysHitRng); // RNG 0 → roll 0 < 100 → hit

        sys.ExecuteTurn(state, new BattleCommand.Fight(0));

        Assert.IsType<BattleEvent.MoveUsed>(state.Events[0]);
        Assert.IsType<BattleEvent.DamageDealt>(state.Events[1]);
        Assert.DoesNotContain(state.Events, e => e is BattleEvent.MoveMissed);
    }

    // ── 7. Fainting ends battle ─────────────────────────────────

    [Fact]
    public void Fainting_EndsBattle()
    {
        // Set opponent's HP to a value that a single Thunder Jolt will KO
        var player = SpeciesLibrary.Spark.CreateInstance(15);
        var opponent = SpeciesLibrary.Stone.CreateInstance(15);
        opponent.CurrentHp = 5; // Thunder Jolt does 9 damage → KO
        var state = new BattleState(player, opponent);
        var sys = new BattleSystem(AlwaysHitRng);

        sys.ExecuteTurn(state, new BattleCommand.Fight(0));

        Assert.True(state.IsOver);
        Assert.True(state.PlayerWon);
        Assert.Contains(state.Events, e => e is BattleEvent.JoyMonFainted);
    }

    // ── 8. Victory grants XP ────────────────────────────────────

    [Fact]
    public void Victory_GrantsXp()
    {
        var player = SpeciesLibrary.Spark.CreateInstance(15);
        var opponent = SpeciesLibrary.Stone.CreateInstance(15);
        opponent.CurrentHp = 5; // One-hit KO
        var state = new BattleState(player, opponent);
        var sys = new BattleSystem(AlwaysHitRng);

        sys.ExecuteTurn(state, new BattleCommand.Fight(0));

        Assert.Contains(state.Events, e => e is BattleEvent.XpGained);
        Assert.True(player.Xp > 0);
    }

    // ── 9. Level-up increases stats ─────────────────────────────

    [Fact]
    public void LevelUp_IncreasesStats()
    {
        // Spark at level 14, almost at threshold
        var player = SpeciesLibrary.Spark.CreateInstance(14);
        player.Xp = 139; // threshold = 140, so 1 XP more would trigger level-up
        var initialLevel = player.Level;
        var initialMaxHp = player.MaxHp;
        var initialAtk = player.Attack;
        var initialDef = player.Defense;
        var initialSpd = player.Speed;

        // Battle a low-level opponent that grants enough XP to cross threshold
        var opponent = SpeciesLibrary.Stone.CreateInstance(1);
        opponent.CurrentHp = 1; // Guarantee one-hit KO
        var state = new BattleState(player, opponent);
        var sys = new BattleSystem(AlwaysHitRng);

        sys.ExecuteTurn(state, new BattleCommand.Fight(0));

        Assert.Contains(state.Events, e => e is BattleEvent.LevelUp);
        Assert.Equal(initialLevel + 1, player.Level);
        Assert.Equal(initialMaxHp + 3, player.MaxHp);
        Assert.Equal(initialAtk + 1, player.Attack);
        Assert.Equal(initialDef + 1, player.Defense);
        Assert.Equal(initialSpd + 1, player.Speed);
    }

    // ── 10. Event log order ─────────────────────────────────────

    [Fact]
    public void EventLog_ContainsExpectedEventsInOrder()
    {
        // Level 1 Spark (spd 13, atk 7, hp 38) vs Level 1 Stone (spd 5, def 11)
        // Turn order: Spark first
        // Spark Quick Sparks (power 25): damage = max(1, (7*25)/11/4) = 3
        // Stone Rock Toss (power 50):   damage = max(1, (6*50)/6/4)  = 12
        var player = SpeciesLibrary.Spark.CreateInstance(1);
        var opponent = SpeciesLibrary.Stone.CreateInstance(1);
        var state = new BattleState(player, opponent);
        var sys = new BattleSystem(AlwaysHitRng);

        sys.ExecuteTurn(state, new BattleCommand.Fight(1)); // Quick Sparks

        // Expected: MoveUsed(Spark) → DamageDealt(Spark→Stone) → MoveUsed(Stone) → DamageDealt(Stone→Spark)
        Assert.Equal(4, state.Events.Count);
        Assert.Equal("Spark", Assert.IsType<BattleEvent.MoveUsed>(state.Events[0]).SpeciesName);
        Assert.Equal("Spark", Assert.IsType<BattleEvent.DamageDealt>(state.Events[1]).SourceName);
        Assert.Equal("Stone", Assert.IsType<BattleEvent.MoveUsed>(state.Events[2]).SpeciesName);
        Assert.Equal("Stone", Assert.IsType<BattleEvent.DamageDealt>(state.Events[3]).SourceName);

        // Spark base HP = 35 + level*3 = 38, minus 12 = 26
        // Stone base HP = 55 + level*3 = 58, minus 3 = 55
        Assert.Equal(26, player.CurrentHp);
        Assert.Equal(55, opponent.CurrentHp);
    }
}