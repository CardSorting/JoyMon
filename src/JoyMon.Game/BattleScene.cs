using JoyMon.Core;

namespace JoyMon.Game;

public enum BattleSceneMode
{
    Message,
    Command,
    Fight,
    Item,
    Finished,
}

public enum BattleSceneOutcome
{
    None,
    Won,
    Lost,
    Escaped,
    Captured,
}

/// <summary>
/// UI-facing controller for wild and trainer battles. The controller owns menu state
/// and delegates deterministic turn resolution to JoyMon.Core.
/// </summary>
public sealed class BattleScene
{
    private static readonly string[] WildCommandLabels = { "Fight", "Item", "Capture", "Run" };
    private static readonly string[] TrainerCommandLabels = { "Fight", "Item" };

    private readonly string[] _commandLabels;
    private readonly BattleSystem _battleSystem;
    private readonly CaptureCalculator _captureCalculator;
    private readonly Queue<string> _messages = new();
    private readonly PlayerProfile? _profile;
    private readonly List<string> _battleItems = new();

    public BattleState State { get; }
    public bool IsTrainerBattle { get; }
    public bool IsBossBattle { get; }
    public bool CanRun => !IsRestrictedBattle;
    public bool CanCapture => !IsRestrictedBattle;
    private bool IsRestrictedBattle => IsTrainerBattle || IsBossBattle;
    public BattleSceneMode Mode { get; private set; } = BattleSceneMode.Message;
    public BattleSceneOutcome Outcome { get; private set; } = BattleSceneOutcome.None;
    public int CommandIndex { get; private set; }
    public int MoveIndex { get; private set; }
    public int ItemIndex { get; private set; }

    public IReadOnlyList<string> Commands => _commandLabels;
    public IReadOnlyList<string> BattleItems => _battleItems;
    public IReadOnlyList<MoveDefinition> KnownMoves => State.PlayerJoyMon.Species.Moves;
    public string CurrentMessage => _messages.Count > 0 ? _messages.Peek() : string.Empty;
    public bool RequiresSafeRecovery => Outcome == BattleSceneOutcome.Lost && !IsRestrictedBattle;

    public BattleScene(
        JoyMonInstance playerJoyMon,
        JoyMonInstance opponentJoyMon,
        IBattleRng battleRng,
        PlayerProfile? profile = null,
        bool isTrainerBattle = false,
        string? opponentTrainerName = null,
        bool isBossBattle = false,
        string? bossDisplayName = null)
    {
        IsTrainerBattle = isTrainerBattle;
        IsBossBattle = isBossBattle;
        _commandLabels = IsRestrictedBattle ? TrainerCommandLabels : WildCommandLabels;
        State = new BattleState(playerJoyMon, opponentJoyMon);
        _battleSystem = new BattleSystem(battleRng);
        _captureCalculator = new CaptureCalculator(battleRng);
        _profile = profile;
        RefreshBattleItems();

        if (isBossBattle)
        {
            var bossName = string.IsNullOrWhiteSpace(bossDisplayName) ? opponentJoyMon.Species.Name : bossDisplayName;
            _messages.Enqueue($"{bossName} appeared!");
            _messages.Enqueue("The Trial Grove guardian awaits!");
            _messages.Enqueue($"Go, {playerJoyMon.Species.Name}!");
        }
        else if (isTrainerBattle)
        {
            var trainerName = string.IsNullOrWhiteSpace(opponentTrainerName) ? "Trainer" : opponentTrainerName;
            _messages.Enqueue($"{trainerName} wants to battle!");
            _messages.Enqueue($"{opponentJoyMon.Species.Name} was sent out!");
            _messages.Enqueue($"Go, {playerJoyMon.Species.Name}!");
        }
        else
        {
            _messages.Enqueue($"Wild {opponentJoyMon.Species.Name} appeared!");
            _messages.Enqueue($"Go, {playerJoyMon.Species.Name}!");
        }
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
                CommandIndex = (CommandIndex + _commandLabels.Length - 1) % _commandLabels.Length;
                break;
            case BattleSceneMode.Fight:
                if (KnownMoves.Count > 0)
                    MoveIndex = (MoveIndex + KnownMoves.Count - 1) % KnownMoves.Count;
                break;
            case BattleSceneMode.Item:
                if (_battleItems.Count > 0)
                    ItemIndex = (ItemIndex + _battleItems.Count - 1) % _battleItems.Count;
                break;
        }
    }

    public void MoveDown()
    {
        switch (Mode)
        {
            case BattleSceneMode.Command:
                CommandIndex = (CommandIndex + 1) % _commandLabels.Length;
                break;
            case BattleSceneMode.Fight:
                if (KnownMoves.Count > 0)
                    MoveIndex = (MoveIndex + 1) % KnownMoves.Count;
                break;
            case BattleSceneMode.Item:
                if (_battleItems.Count > 0)
                    ItemIndex = (ItemIndex + 1) % _battleItems.Count;
                break;
        }
    }

    public void Cancel()
    {
        if (Mode == BattleSceneMode.Fight || Mode == BattleSceneMode.Item)
            Mode = BattleSceneMode.Command;
    }

    public bool Confirm()
    {
        return Mode switch
        {
            BattleSceneMode.Message => AdvanceMessage(),
            BattleSceneMode.Command => ConfirmCommand(),
            BattleSceneMode.Fight => TrySubmitMove(MoveIndex),
            BattleSceneMode.Item => TryUseSelectedItem(),
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

    public UseItemResult? TryUseItem(string itemId, JoyMonInstance? target = null)
    {
        if (Mode != BattleSceneMode.Command && Mode != BattleSceneMode.Item)
            return null;

        if (_profile is null)
            return null;

        target ??= State.PlayerJoyMon;
        var result = ItemService.TryUse(_profile.Items, itemId, target);

        switch (result)
        {
            case UseItemResult.Success:
                if (ItemCatalog.TryGet(itemId, out var definition))
                    _messages.Enqueue($"Used {definition.Name}!");
                RefreshBattleItems();
                break;
            case UseItemResult.MissingItem:
                _messages.Enqueue("You don't have that item!");
                break;
            case UseItemResult.TargetFainted:
                _messages.Enqueue("It won't have any effect!");
                break;
            case UseItemResult.AlreadyFullHp:
                _messages.Enqueue("HP is already full!");
                break;
            case UseItemResult.InvalidTarget:
            case UseItemResult.UnknownItem:
                _messages.Enqueue("That item can't be used now!");
                break;
        }

        if (_messages.Count > 0)
            Mode = BattleSceneMode.Message;

        return result;
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

        if (CommandIndex == 1)
        {
            RefreshBattleItems();
            if (_battleItems.Count == 0)
            {
                _messages.Enqueue("No usable items!");
                Mode = BattleSceneMode.Message;
                return true;
            }

            ItemIndex = Math.Clamp(ItemIndex, 0, _battleItems.Count - 1);
            Mode = BattleSceneMode.Item;
            return true;
        }

        if (!IsRestrictedBattle && CommandIndex == 2)
        {
            TryCapture();
            return true;
        }

        if (!IsRestrictedBattle && CommandIndex == 3)
        {
            ResolveTurn(new BattleCommand.Run(), runSubmitted: true);
            return true;
        }

        if (IsRestrictedBattle)
            _messages.Enqueue(IsBossBattle
                ? "You can't flee from a boss battle!"
                : "You can't run from a trainer battle!");

        Mode = BattleSceneMode.Message;
        return true;
    }

    private bool TryUseSelectedItem()
    {
        if (Mode != BattleSceneMode.Item || State.IsOver)
            return false;

        if (ItemIndex < 0 || ItemIndex >= _battleItems.Count)
            return false;

        TryUseItem(_battleItems[ItemIndex], State.PlayerJoyMon);
        return true;
    }

    public CaptureResult? TryCapture()
    {
        if (IsRestrictedBattle || Mode != BattleSceneMode.Command || State.IsOver)
            return null;

        if (_profile is null)
        {
            _messages.Enqueue("No Sync Capsules are ready!");
            Mode = BattleSceneMode.Message;
            return CaptureResult.Failed;
        }

        if (_profile.Items.GetQuantity(ItemCatalog.SyncCapsuleId) <= 0)
        {
            _messages.Enqueue("No Sync Capsules left!");
            Mode = BattleSceneMode.Message;
            return CaptureResult.Failed;
        }

        var result = _captureCalculator.TryCapture(
            State.OpponentJoyMon,
            State.PlayerJoyMon.Level,
            _profile.Party.Count,
            PlayerProfile.PartyLimit);

        switch (result)
        {
            case CaptureResult.Success:
                _profile.Items.TryConsume(ItemCatalog.SyncCapsuleId);
                _profile.Party.Add(State.OpponentJoyMon);
                State.IsOver = true;
                State.PlayerWon = true;
                Outcome = BattleSceneOutcome.Captured;
                _messages.Enqueue($"{State.OpponentJoyMon.Species.Name} was captured!");
                break;
            case CaptureResult.Failed:
                _profile.Items.TryConsume(ItemCatalog.SyncCapsuleId);
                _messages.Enqueue("Oh no! The JoyMon broke free!");
                break;
            case CaptureResult.PartyFull:
                _messages.Enqueue("Your party is full!");
                break;
        }

        RefreshBattleItems();
        Mode = BattleSceneMode.Message;
        return result;
    }

    private void RefreshBattleItems()
    {
        _battleItems.Clear();

        if (_profile is null)
            return;

        if (_profile.Items.GetQuantity(ItemCatalog.BerryTonicId) > 0)
            _battleItems.Add(ItemCatalog.BerryTonicId);
    }

    private void ResolveTurn(BattleCommand command, bool runSubmitted)
    {
        if (IsRestrictedBattle && command is BattleCommand.Run)
            return;

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
