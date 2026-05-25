using JoyMon.Core;
using JoyMon.Game;

namespace JoyMon.Tests;

public class StatusEffectTests
{
    private static readonly MoveDefinition EmberNudge = new(
        "ember-nudge", "Ember Nudge", JoyMonType.Ember, 35, 100, 25,
        MoveEffects.Burn, 30);

    private static readonly MoveDefinition GuardCurl = new(
        "guard-curl", "Guard Curl", JoyMonType.Tide, 0, 100, 20,
        MoveEffects.Guard);

    private static readonly MoveDefinition HeavyHit = new(
        "heavy-hit", "Heavy Hit", JoyMonType.Neutral, 80, 100, 20);

    private static readonly MoveDefinition Distract = new(
        "distract", "Distract", JoyMonType.Neutral, 10, 0, 20);

    private static readonly MoveDefinition Wait = new(
        "wait", "Wait", JoyMonType.Neutral, 0, 100, 20);

    private static JoyMonSpecies MakeSpecies(string name, int hp, int atk, int def, int spd, params MoveDefinition[] moves)
    {
        return new JoyMonSpecies(name, JoyMonType.Neutral, hp, atk, def, spd, moves);
    }

    private static JoyMonInstance MakeBattler(string name, int hp, int atk, int def, int spd, params MoveDefinition[] moves)
    {
        return MakeSpecies(name, hp, atk, def, spd, moves).CreateInstance(10);
    }

    // ── 1. Burn applies deterministically ───────────────────────

    [Fact]
    public void Burn_AppliesDeterministically()
    {
        var player = MakeBattler("Ember", 50, 20, 10, 50, EmberNudge);
        var opponent = MakeBattler("Target", 80, 10, 10, 1, HeavyHit);
        var state = new BattleState(player, opponent);
        var sys = new BattleSystem(new DeterministicRng(0.0));

        sys.ExecuteTurn(state, new BattleCommand.Fight(0));

        Assert.Contains(state.Events, e =>
            e is BattleEvent.StatusApplied { StatusName: BattleStatus.Burn, SpeciesName: "Target" });
        Assert.True(opponent.BurnTurnsRemaining > 0);
    }

    [Fact]
    public void Burn_DoesNotApply_WhenRollFails()
    {
        var player = MakeBattler("Ember", 50, 20, 10, 50, EmberNudge);
        var opponent = MakeBattler("Target", 80, 10, 10, 1, HeavyHit);
        var state = new BattleState(player, opponent);
        var sys = new BattleSystem(new SequenceRng(0.0, 0.5));

        sys.ExecuteTurn(state, new BattleCommand.Fight(0));

        Assert.DoesNotContain(state.Events, e => e is BattleEvent.StatusApplied);
        Assert.Equal(0, opponent.BurnTurnsRemaining);
    }

    // ── 2. Burn deals damage after acting ─────────────────────────

    [Fact]
    public void Burn_DealsDamageAfterActing()
    {
        var player = MakeBattler("Fast", 80, 10, 10, 50, HeavyHit);
        var opponent = MakeBattler("Burned", 80, 10, 10, 1, HeavyHit);
        opponent.BurnTurnsRemaining = BattleStatus.BurnDurationTurns;
        var hpBefore = opponent.CurrentHp;
        var state = new BattleState(player, opponent);
        var sys = new BattleSystem(new DeterministicRng(0.0));

        sys.ExecuteTurn(state, new BattleCommand.Fight(0));

        var burnDamageIndex = state.Events.FindIndex(e =>
            e is BattleEvent.StatusDamage { StatusName: BattleStatus.Burn, SpeciesName: "Burned" });
        var opponentMoveIndex = state.Events.FindIndex(e =>
            e is BattleEvent.MoveUsed { SpeciesName: "Burned" });

        Assert.True(burnDamageIndex > opponentMoveIndex);
        Assert.Contains(state.Events, e =>
            e is BattleEvent.StatusDamage { Damage: BattleStatus.BurnDamagePerTick });
        Assert.True(opponent.CurrentHp < hpBefore);
    }

    // ── 3. Burn expires after 3 turns ───────────────────────────

    [Fact]
    public void Burn_ExpiresAfterThreeTurns()
    {
        var player = MakeBattler("Fast", 80, 10, 10, 50, HeavyHit);
        var opponent = MakeBattler("Burned", 80, 10, 10, 1, HeavyHit);
        opponent.BurnTurnsRemaining = BattleStatus.BurnDurationTurns;
        var state = new BattleState(player, opponent);
        var sys = new BattleSystem(new DeterministicRng(0.0));

        for (int turn = 0; turn < 3; turn++)
        {
            state.Events.Clear();
            if (state.IsOver) break;
            sys.ExecuteTurn(state, new BattleCommand.Fight(0));
        }

        Assert.Equal(0, opponent.BurnTurnsRemaining);
        Assert.Contains(state.Events, e =>
            e is BattleEvent.StatusExpired { StatusName: BattleStatus.Burn, SpeciesName: "Burned" });
    }

    // ── 4. Guard reduces damage ───────────────────────────────────

    [Fact]
    public void Guard_ReducesIncomingDamage()
    {
        var player = MakeBattler("Guardian", 80, 10, 10, 50, GuardCurl, HeavyHit);
        var opponent = MakeBattler("Foe", 80, 20, 5, 1, HeavyHit);
        var state = new BattleState(player, opponent);
        var sys = new BattleSystem(new DeterministicRng(0.0));

        var fullDamage = BattleSystem.CalculateDamage(opponent, player, HeavyHit);
        var hpBefore = player.CurrentHp;

        sys.ExecuteTurn(state, new BattleCommand.Fight(0));

        var actualDamage = hpBefore - player.CurrentHp;
        var expectedGuarded = Math.Max(1, fullDamage / 2);

        Assert.Equal(expectedGuarded, actualDamage);
        Assert.True(actualDamage < fullDamage);
    }

    // ── 5. Guard expires after hit or end of turn ────────────────

    [Fact]
    public void Guard_ExpiresAfterIncomingHit()
    {
        var player = MakeBattler("Guardian", 80, 10, 10, 50, GuardCurl, HeavyHit);
        var opponent = MakeBattler("Foe", 80, 20, 5, 1, HeavyHit);
        var state = new BattleState(player, opponent);
        var sys = new BattleSystem(new DeterministicRng(0.0));

        sys.ExecuteTurn(state, new BattleCommand.Fight(0));

        Assert.False(player.IsGuarding);
        Assert.Contains(state.Events, e =>
            e is BattleEvent.StatusApplied { StatusName: BattleStatus.Guard, SpeciesName: "Guardian" });
    }

    [Fact]
    public void Guard_ExpiresAtEndOfTurn_IfUnused()
    {
        var player = MakeBattler("Guardian", 80, 10, 10, 50, GuardCurl, HeavyHit);
        var opponent = MakeBattler("Foe", 80, 20, 5, 1, Distract);
        var state = new BattleState(player, opponent);
        var sys = new BattleSystem(new DeterministicRng(0.0));

        sys.ExecuteTurn(state, new BattleCommand.Fight(0));

        Assert.False(player.IsGuarding);
        Assert.Contains(state.Events, e =>
            e is BattleEvent.StatusExpired { StatusName: BattleStatus.Guard, SpeciesName: "Guardian" });
    }

    // ── 6. Status events appear in order ────────────────────────

    [Fact]
    public void StatusEvents_AppearInOrder()
    {
        var player = MakeBattler("Ember", 50, 20, 10, 50, EmberNudge, Wait);
        var opponent = MakeBattler("Burned", 80, 10, 10, 1, Distract);
        var state = new BattleState(player, opponent);
        var sys = new BattleSystem(new DeterministicRng(0.0));

        sys.ExecuteTurn(state, new BattleCommand.Fight(0));

        var types = state.Events.Select(e => e.GetType()).ToList();
        var appliedIndex = types.FindIndex(t => t == typeof(BattleEvent.StatusApplied));
        var burnDamageIndex = types.FindIndex(t => t == typeof(BattleEvent.StatusDamage));

        Assert.True(appliedIndex >= 0);
        Assert.True(burnDamageIndex > appliedIndex);

        var sawExpired = false;
        for (int turn = 0; turn < 5 && !state.IsOver; turn++)
        {
            state.Events.Clear();
            sys.ExecuteTurn(state, new BattleCommand.Fight(1));
            sawExpired = state.Events.Any(e =>
                e is BattleEvent.StatusExpired { StatusName: BattleStatus.Burn });
            if (sawExpired) break;
        }

        Assert.True(sawExpired);
        var expiredIndex = state.Events.FindIndex(e =>
            e is BattleEvent.StatusExpired { StatusName: BattleStatus.Burn });
        var lastDamageIndex = state.Events.FindLastIndex(e =>
            e is BattleEvent.StatusDamage { StatusName: BattleStatus.Burn });
        Assert.True(expiredIndex > lastDamageIndex);
    }

    [Fact]
    public void GuardCurl_AppliesGuardWithoutDamage()
    {
        var player = MakeBattler("Guardian", 80, 10, 10, 50, GuardCurl);
        var opponent = MakeBattler("Foe", 80, 20, 5, 1, HeavyHit);
        var state = new BattleState(player, opponent);
        var sys = new BattleSystem(new DeterministicRng(0.0));

        sys.ExecuteTurn(state, new BattleCommand.Fight(0));

        Assert.Contains(state.Events, e =>
            e is BattleEvent.StatusApplied { StatusName: BattleStatus.Guard, SpeciesName: "Guardian" });
        Assert.DoesNotContain(state.Events, e =>
            e is BattleEvent.DamageDealt { TargetName: "Foe" });
    }

    [Fact]
    public void EmberNudge_ContentValidates()
    {
        var contentRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "content"));
        var loader = new JoyMon.Content.ContentLoader(contentRoot);
        var db = loader.Load();

        Assert.True(db.MoveDefinitions.ContainsKey("ember-nudge"));
        var move = db.MoveDefinitions["ember-nudge"];
        Assert.Equal(MoveEffects.Burn, move.Effect);
        Assert.Equal(30, move.EffectChance);

        var guard = db.MoveDefinitions["guard-curl"];
        Assert.Equal(MoveEffects.Guard, guard.Effect);
        Assert.Equal(0, guard.Power);
    }

    [Fact]
    public void BattleScene_MapsStatusMessages()
    {
        var player = MakeBattler("Ember", 50, 20, 10, 50, EmberNudge);
        var opponent = MakeBattler("Target", 80, 10, 10, 1, HeavyHit);
        var scene = ReadyForCommand(new BattleScene(player, opponent, new DeterministicRng(0.0)));

        scene.Confirm();
        scene.Confirm();

        var sawBurnMessage = false;
        while (scene.Mode == BattleSceneMode.Message)
        {
            if (scene.CurrentMessage.Contains("burned", StringComparison.OrdinalIgnoreCase))
                sawBurnMessage = true;
            scene.Confirm();
        }

        Assert.True(sawBurnMessage);
    }

    private static BattleScene ReadyForCommand(BattleScene scene)
    {
        while (scene.Mode == BattleSceneMode.Message)
            scene.Confirm();
        return scene;
    }
}
