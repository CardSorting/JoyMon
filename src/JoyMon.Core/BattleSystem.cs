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

        // Determine turn order — higher Speed goes first
        bool playerFirst = DetermineTurnOrder(player, opponent);

        // Resolve actions in Speed order
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

        // Post-action win/loss check
        if (!state.IsOver)
            CheckBattleEnd(state);
    }

    // ── Turn order ──────────────────────────────────────────────

    private static bool DetermineTurnOrder(JoyMonInstance a, JoyMonInstance b)
    {
        if (a.Speed > b.Speed) return true;  // a (player) first
        if (b.Speed > a.Speed) return false; // b (opponent) first
        return true; // tie → player goes first
    }

    // ── Action resolution ───────────────────────────────────────

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
                break;
        }
    }

    // ── Fight resolution ────────────────────────────────────────

    private void ResolveFight(BattleState state, BattleCommand.Fight fight, JoyMonInstance actor, JoyMonInstance target)
    {
        var moveIndex = fight.MoveIndex;
        if (moveIndex < 0 || moveIndex >= actor.Species.Moves.Count)
        {
            // Invalid move index — treat as Struggle
            ActivateStruggle(state, actor, target);
            return;
        }

        // No PP? Use Struggle
        if (actor.RemainingUses[moveIndex] <= 0)
        {
            ActivateStruggle(state, actor, target);
            return;
        }

        var move = actor.Species.Moves[moveIndex];

        // Deduct PP
        actor.RemainingUses[moveIndex]--;

        // Log move usage
        state.Events.Add(new BattleEvent.MoveUsed(actor.Species.Name, move.Name));

        // Accuracy check: RNG * 100 < move.Accuracy → hit
        double roll = _rng.NextDouble() * 100.0;
        if (roll >= move.Accuracy)
        {
            state.Events.Add(new BattleEvent.MoveMissed(actor.Species.Name, move.Name));
            return;
        }

        // Calculate and apply damage
        int damage = CalculateDamage(actor, target, move);
        target.CurrentHp -= damage;
        state.Events.Add(new BattleEvent.DamageDealt(actor.Species.Name, target.Species.Name, damage));

        // Check faint
        if (target.IsFainted)
        {
            state.Events.Add(new BattleEvent.JoyMonFainted(target.Species.Name));
        }
    }

    /// <summary>
    /// Fallback when no valid move has PP remaining.
    /// </summary>
    private void ActivateStruggle(BattleState state, JoyMonInstance actor, JoyMonInstance target)
    {
        state.Events.Add(new BattleEvent.MoveUsed(actor.Species.Name, "Struggle"));

        // Struggle always hits, low power, minor recoil
        int damage = Math.Max(1, ((actor.Attack * 10) / Math.Max(1, target.Defense)) / 4);
        target.CurrentHp -= damage;
        state.Events.Add(new BattleEvent.DamageDealt(actor.Species.Name, target.Species.Name, damage));

        if (target.IsFainted)
            state.Events.Add(new BattleEvent.JoyMonFainted(target.Species.Name));
    }

    // ── Damage formula ──────────────────────────────────────────

    /// <summary>
    /// damage = max(1, ((attacker.Attack * move.Power) / max(1, defender.Defense)) / 4)
    /// </summary>
    public static int CalculateDamage(JoyMonInstance attacker, JoyMonInstance defender, MoveDefinition move)
    {
        return Math.Max(1, ((attacker.Attack * move.Power) / Math.Max(1, defender.Defense)) / 4);
    }

    // ── Opponent AI ─────────────────────────────────────────────

    private static BattleCommand GetOpponentCommand(JoyMonInstance joymon)
    {
        // Pick first non-depleted move
        for (int i = 0; i < joymon.Species.Moves.Count; i++)
        {
            if (joymon.RemainingUses[i] > 0)
                return new BattleCommand.Fight(i);
        }

        // All moves depleted - use Struggle via index 0 (will trigger ActivateStruggle)
        return new BattleCommand.Fight(0);
    }

    // ── End-of-turn checks ──────────────────────────────────────

    private void CheckBattleEnd(BattleState state)
    {
        var player = state.PlayerJoyMon;
        var opponent = state.OpponentJoyMon;

        if (opponent.IsFainted)
        {
            state.IsOver = true;
            state.PlayerWon = true;
            state.Events.Add(new BattleEvent.BattleWon());

            // Award XP based on opponent's strength
            int xpAmount = opponent.Species.BaseMaxHp + opponent.Level;
            int levelsGained = player.GrantXp(xpAmount);
            state.Events.Add(new BattleEvent.XpGained(xpAmount, player.Xp));

            if (levelsGained > 0)
            {
                state.Events.Add(new BattleEvent.LevelUp(player.Species.Name, player.Level));
            }
        }
        else if (player.IsFainted)
        {
            state.IsOver = true;
            state.PlayerWon = false;
            state.Events.Add(new BattleEvent.BattleLost());
        }
    }
}