namespace JoyMon.Core;

/// <summary>
/// Pure, deterministic battle engine. No I/O, no rendering, no MonoGame dependency.
/// All randomness is injected via <see cref="IBattleRng"/>.
/// </summary>
public class BattleSystem
{
    private readonly IBattleRng _rng;

    public BattleSystem(IBattleRng rng)
    {
        _rng = rng ?? throw new ArgumentNullException(nameof(rng));
    }

    /// <summary>
    /// Executes one full turn: determines turn order, resolves both combatants' actions,
    /// checks for faint/fatality, and awards XP on victory.
    /// </summary>
    public void ExecuteTurn(BattleState state, BattleCommand playerCommand)
    {
        if (state.IsOver) return;

        var player = state.PlayerJoyMon;
        var opponent = state.OpponentJoyMon;

        bool playerFirst = DetermineTurnOrder(player, opponent);

        if (playerFirst)
        {
            ResolveAction(state, playerCommand, player, opponent);
            if (!state.IsOver)
                ResolveAction(state, GetOpponentCommand(opponent), opponent, player);
        }
        else
        {
            ResolveAction(state, GetOpponentCommand(opponent), opponent, player);
            if (!state.IsOver)
                ResolveAction(state, playerCommand, player, opponent);
        }

        ExpireUnusedGuards(state);

        if (!state.IsOver)
            CheckBattleEnd(state);
    }

    private static bool DetermineTurnOrder(JoyMonInstance a, JoyMonInstance b)
    {
        if (a.Speed > b.Speed) return true;
        if (b.Speed > a.Speed) return false;
        return true;
    }

    private void ResolveAction(BattleState state, BattleCommand command, JoyMonInstance actor, JoyMonInstance target)
    {
        if (state.IsOver || actor.IsFainted || target.IsFainted) return;

        switch (command)
        {
            case BattleCommand.Fight fight:
                ResolveFight(state, fight, actor, target);
                break;
            case BattleCommand.Run:
                state.IsOver = true;
                state.PlayerWon = false;
                state.Events.Add(new BattleEvent.BattleLost());
                return;
        }

        if (!state.IsOver && !actor.IsFainted)
            ProcessBurnAfterActing(state, actor);
    }

    private void ResolveFight(BattleState state, BattleCommand.Fight fight, JoyMonInstance actor, JoyMonInstance target)
    {
        var moveIndex = fight.MoveIndex;
        if (moveIndex < 0 || moveIndex >= actor.Species.Moves.Count)
        {
            ActivateStruggle(state, actor, target);
            return;
        }

        if (actor.RemainingUses[moveIndex] <= 0)
        {
            ActivateStruggle(state, actor, target);
            return;
        }

        var move = actor.Species.Moves[moveIndex];
        actor.RemainingUses[moveIndex]--;

        state.Events.Add(new BattleEvent.MoveUsed(actor.Species.Name, move.Name));

        double roll = _rng.NextDouble() * 100.0;
        if (roll >= move.Accuracy)
        {
            state.Events.Add(new BattleEvent.MoveMissed(actor.Species.Name, move.Name));
            return;
        }

        if (move.AppliesGuard)
        {
            actor.IsGuarding = true;
            state.Events.Add(new BattleEvent.StatusApplied(actor.Species.Name, BattleStatus.Guard));
            return;
        }

        int damage = CalculateDamage(actor, target, move);
        damage = ApplyGuardReduction(target, damage);
        target.CurrentHp -= damage;
        state.Events.Add(new BattleEvent.DamageDealt(actor.Species.Name, target.Species.Name, damage));

        if (!target.IsFainted)
            TryApplyBurn(state, move, target);

        if (target.IsFainted)
            state.Events.Add(new BattleEvent.JoyMonFainted(target.Species.Name));
    }

    private void ActivateStruggle(BattleState state, JoyMonInstance actor, JoyMonInstance target)
    {
        state.Events.Add(new BattleEvent.MoveUsed(actor.Species.Name, "Struggle"));

        int damage = Math.Max(1, ((actor.Attack * 10) / Math.Max(1, target.Defense)) / 4);
        damage = ApplyGuardReduction(target, damage);
        target.CurrentHp -= damage;
        state.Events.Add(new BattleEvent.DamageDealt(actor.Species.Name, target.Species.Name, damage));

        if (target.IsFainted)
            state.Events.Add(new BattleEvent.JoyMonFainted(target.Species.Name));
    }

    private void TryApplyBurn(BattleState state, MoveDefinition move, JoyMonInstance target)
    {
        if (!move.CanInflictBurn) return;

        var chance = move.EffectChance > 0
            ? move.EffectChance
            : BattleStatus.DefaultBurnChancePercent;

        if (_rng.NextDouble() * 100.0 >= chance)
            return;

        target.BurnTurnsRemaining = BattleStatus.BurnDurationTurns;
        state.Events.Add(new BattleEvent.StatusApplied(target.Species.Name, BattleStatus.Burn));
    }

    private static int ApplyGuardReduction(JoyMonInstance defender, int damage)
    {
        if (!defender.IsGuarding) return damage;

        defender.IsGuarding = false;
        return Math.Max(1, damage / 2);
    }

    private void ProcessBurnAfterActing(BattleState state, JoyMonInstance actor)
    {
        if (actor.BurnTurnsRemaining <= 0) return;

        actor.CurrentHp -= BattleStatus.BurnDamagePerTick;
        state.Events.Add(new BattleEvent.StatusDamage(
            actor.Species.Name,
            BattleStatus.Burn,
            BattleStatus.BurnDamagePerTick));

        actor.BurnTurnsRemaining--;
        if (actor.BurnTurnsRemaining == 0)
            state.Events.Add(new BattleEvent.StatusExpired(actor.Species.Name, BattleStatus.Burn));

        if (actor.IsFainted)
            state.Events.Add(new BattleEvent.JoyMonFainted(actor.Species.Name));
    }

    private static void ExpireUnusedGuards(BattleState state)
    {
        ExpireGuardIfActive(state, state.PlayerJoyMon);
        ExpireGuardIfActive(state, state.OpponentJoyMon);
    }

    private static void ExpireGuardIfActive(BattleState state, JoyMonInstance joyMon)
    {
        if (!joyMon.IsGuarding) return;

        joyMon.IsGuarding = false;
        state.Events.Add(new BattleEvent.StatusExpired(joyMon.Species.Name, BattleStatus.Guard));
    }

    public static int CalculateDamage(JoyMonInstance attacker, JoyMonInstance defender, MoveDefinition move)
    {
        if (move.AppliesGuard || move.Power <= 0)
            return 0;

        return Math.Max(1, ((attacker.Attack * move.Power) / Math.Max(1, defender.Defense)) / 4);
    }

    private static BattleCommand GetOpponentCommand(JoyMonInstance joymon)
    {
        for (int i = 0; i < joymon.Species.Moves.Count; i++)
        {
            if (joymon.RemainingUses[i] > 0)
                return new BattleCommand.Fight(i);
        }

        return new BattleCommand.Fight(0);
    }

    private void CheckBattleEnd(BattleState state)
    {
        var player = state.PlayerJoyMon;
        var opponent = state.OpponentJoyMon;

        if (opponent.IsFainted)
        {
            state.IsOver = true;
            state.PlayerWon = true;
            state.Events.Add(new BattleEvent.BattleWon());

            int xpAmount = opponent.Species.BaseMaxHp + opponent.Level;
            int levelsGained = player.GrantXp(xpAmount);
            state.Events.Add(new BattleEvent.XpGained(xpAmount, player.Xp));

            if (levelsGained > 0)
                state.Events.Add(new BattleEvent.LevelUp(player.Species.Name, player.Level));
        }
        else if (player.IsFainted)
        {
            state.IsOver = true;
            state.PlayerWon = false;
            state.Events.Add(new BattleEvent.BattleLost());
        }
    }
}
