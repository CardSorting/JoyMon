using JoyMon.Core;

namespace JoyMon.Game;

public enum BattleSceneMode
{
    Message,
    Command,
    Fight,
    Finished,
}

public enum BattleSceneOutcome
{
    None,
    Won,
    Lost,
    Escaped,
}

/// <summary>
/// UI-facing controller for a wild one-on-one battle. The controller owns menu state
/// and delegates deterministic turn resolution to JoyMon.Core.
/// </summary>
public sealed class BattleScene
{
    private static readonly string[] CommandLabels = { "Fight", "Run" };

    private readonly BattleSystem _battleSystem;
    private readonly Queue<string> _messages = new();

    public BattleState State { get; }
    public BattleSceneMode Mode { get; private set; } = BattleSceneMode.Message;
    public BattleSceneOutcome Outcome { get; private set; } = BattleSceneOutcome.None;
    public int CommandIndex { get; private set; }
    public int MoveIndex { get; private set; }

    public IReadOnlyList<string> Commands => CommandLabels;
    public IReadOnlyList<MoveDefinition> KnownMoves => State.PlayerJoyMon.Species.Moves;
    public string CurrentMessage => _messages.Count > 0 ? _messages.Peek() : string.Empty;
    public bool RequiresSafeRecovery => Outcome == BattleSceneOutcome.Lost;

    public BattleScene(JoyMonInstance playerJoyMon, JoyMonInstance wildJoyMon, IBattleRng battleRng)
    {
        State = CreateWildBattleState(playerJoyMon, wildJoyMon);
        _battleSystem = new BattleSystem(battleRng);

        _messages.Enqueue($"Wild {wildJoyMon.Species.Name} appeared!");
        _messages.Enqueue($"Go, {playerJoyMon.Species.Name}!");
    }

    public static BattleState CreateWildBattleState(JoyMonInstance playerJoyMon, JoyMonInstance wildJoyMon)
    {
        return new BattleState(playerJoyMon, wildJoyMon);
    }

    public void MoveUp()
    {
        switch (Mode)
        {
            case BattleSceneMode.Command:
                CommandIndex = (CommandIndex + CommandLabels.Length - 1) % CommandLabels.Length;
                break;
            case BattleSceneMode.Fight:
                if (KnownMoves.Count > 0)
                    MoveIndex = (MoveIndex + KnownMoves.Count - 1) % KnownMoves.Count;
                break;
        }
    }

    public void MoveDown()
    {
        switch (Mode)
        {
            case BattleSceneMode.Command:
                CommandIndex = (CommandIndex + 1) % CommandLabels.Length;
                break;
            case BattleSceneMode.Fight:
                if (KnownMoves.Count > 0)
                    MoveIndex = (MoveIndex + 1) % KnownMoves.Count;
                break;
        }
    }

    public void Cancel()
    {
        if (Mode == BattleSceneMode.Fight)
            Mode = BattleSceneMode.Command;
    }

    public bool Confirm()
    {
        return Mode switch
        {
            BattleSceneMode.Message => AdvanceMessage(),
            BattleSceneMode.Command => ConfirmCommand(),
            BattleSceneMode.Fight => TrySubmitMove(MoveIndex),
            _ => false,
        };
    }

    public bool TrySubmitMove(int moveIndex)
    {
        if (Mode != BattleSceneMode.Fight || State.IsOver)
            return false;

        if (moveIndex < 0 || moveIndex >= KnownMoves.Count)
            return false;

        ResolveTurn(new BattleCommand.Fight(moveIndex), runSubmitted: false);
        return true;
    }

    private bool AdvanceMessage()
    {
        if (_messages.Count > 0)
            _messages.Dequeue();

        if (_messages.Count == 0)
            Mode = Outcome == BattleSceneOutcome.None ? BattleSceneMode.Command : BattleSceneMode.Finished;

        return true;
    }

    private bool ConfirmCommand()
    {
        if (CommandIndex == 0)
        {
            MoveIndex = Math.Clamp(MoveIndex, 0, Math.Max(0, KnownMoves.Count - 1));
            Mode = BattleSceneMode.Fight;
            return true;
        }

        ResolveTurn(new BattleCommand.Run(), runSubmitted: true);
        return true;
    }

    private void ResolveTurn(BattleCommand command, bool runSubmitted)
    {
        int eventStart = State.Events.Count;
        _battleSystem.ExecuteTurn(State, command);

        for (int i = eventStart; i < State.Events.Count; i++)
        {
            var message = ToMessage(State.Events[i], runSubmitted);
            if (!string.IsNullOrWhiteSpace(message))
                _messages.Enqueue(message);
        }

        if (State.IsOver)
        {
            if (runSubmitted && !State.PlayerJoyMon.IsFainted)
                Outcome = BattleSceneOutcome.Escaped;
            else
                Outcome = State.PlayerWon ? BattleSceneOutcome.Won : BattleSceneOutcome.Lost;
        }

        Mode = _messages.Count > 0 ? BattleSceneMode.Message : BattleSceneMode.Command;
        if (Outcome != BattleSceneOutcome.None && _messages.Count == 0)
            Mode = BattleSceneMode.Finished;
    }

    private static string ToMessage(BattleEvent battleEvent, bool runSubmitted)
    {
        return battleEvent switch
        {
            BattleEvent.MoveUsed e => $"{e.SpeciesName} used {e.MoveName}!",
            BattleEvent.MoveMissed => "It missed!",
            BattleEvent.DamageDealt e => $"{e.TargetName} took {e.Damage} damage!",
            BattleEvent.JoyMonFainted e => $"{e.SpeciesName} fainted!",
            BattleEvent.BattleWon => "You won the battle!",
            BattleEvent.BattleLost => runSubmitted ? "Got away safely!" : "You blacked out!",
            BattleEvent.XpGained e => $"Gained {e.Amount} XP!",
            BattleEvent.LevelUp e => $"{e.SpeciesName} grew to Lv.{e.NewLevel}!",
            _ => string.Empty,
        };
    }
}

public sealed class BattleRngAdapter : IBattleRng
{
    private readonly IRng _rng;

    public BattleRngAdapter(IRng rng)
    {
        _rng = rng;
    }

    public double NextDouble() => _rng.NextDouble();
}
