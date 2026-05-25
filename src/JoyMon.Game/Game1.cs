using JoyMon.Content;
using JoyMon.Core;
using JoyMon.Game.Screens;
using JoyMon.Game.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JoyMon.Game;

/// <summary>
/// Main game loop. Routes update/draw calls by <see cref="GameState"/>.
/// No gameplay logic lives here — only orchestration.
/// </summary>
public sealed class Game1 : Microsoft.Xna.Framework.Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private SpriteFont _font = null!;

    // Services
    private readonly InputService _input = new();
    private readonly VirtualResolution _virt = new();
    private readonly MapRenderer _mapRenderer = new();
    private readonly Camera _camera = new();

    // Screens
    private readonly TitleScreen _title = new();
    private readonly DebugOverlay _debug = new();

    // State
    public GameState State { get; private set; } = GameState.Boot;
    private double _bootTimer;

    // Loaded content
    private MapContent? _currentMap;

    private void TransitionToMap(string mapId, int spawnX, int spawnY, bool autosave = true)
    {
        try
        {
            var mapDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "maps");
            var loader = new MapLoader(mapDir);
            var nextMap = loader.Load($"{mapId}.json");
            _currentMap = nextMap;
            _mapRenderer.SetMap(nextMap);
            _player.Initialize(spawnX, spawnY);
            UpdateSafeRecoveryPoint(nextMap.Id, spawnX, spawnY);
            if (autosave)
                AutosaveCurrentGame();
            
            // Recenter camera immediately
            _camera.Reset();
            _camera.CenterOn(
                spawnX * nextMap.TileSize,
                spawnY * nextMap.TileSize,
                VirtualResolution.VirtualWidth,
                VirtualResolution.VirtualHeight,
                nextMap.Width * nextMap.TileSize,
                nextMap.Height * nextMap.TileSize
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to transition to map '{mapId}': {ex.Message}");
        }
    }

    private void UpdateSafeRecoveryPoint(string mapId, int tileX, int tileY)
    {
        if (mapId != "starter-town")
            return;

        _safeMapId = mapId;
        _safeTileX = tileX;
        _safeTileY = tileY;
    }

    // Player & Profile
    private readonly Player _player = new();
    private Texture2D _playerTexture = null!;
    private readonly PlayerProfile _profile = new();
    private readonly HashSet<string> _defeatedTrainers = new();
    private bool _talkingToDrCedar;
    private ContentDatabase _contentDb = null!;
    private SaveService? _saveService;
    private int _titleSelectionIndex = 1;
    private string? _saveStatusMessage;
    private double _saveStatusTimer;

    // Dialogue and NPCs
    private readonly DialogueState _dialogue = new();
    private readonly List<Npc> _npcs = new();
    private readonly Dictionary<string, DialogueContent> _dialogues = new();
    private readonly Dictionary<string, Texture2D> _npcTextures = new();
    private Texture2D _pixel = null!;

    // Starter choice UI cache
    private int _starterChoiceIndex;
    private readonly string[] _starterIds = { "mossprout", "staticrow", "pebblit" };
    private readonly Dictionary<string, Texture2D> _starterTextures = new();

    // Encounters
    private readonly IRng _rng;
    private readonly Dictionary<string, EncounterTableContent> _encounterTables = new();
    private JoyMonInstance? _wildEncounter;
    private BattleScene? _battleScene;

    // Trainers
    private readonly Dictionary<string, TrainerContent> _trainers = new();
    private string? _pendingTrainerBattleId;
    private string? _activeTrainerId;

    // Boss
    private readonly Dictionary<string, BossContent> _bossesByMapId = new();
    private BossContent? _pendingBoss;
    private BossContent? _activeBoss;
    private bool _activeBossBattle;
    private EndingScreenData? _endingData;
    private string? _lastWildSpeciesId;

    // Recovery point used after wild battle losses.
    private string _safeMapId = "starter-town";
    private int _safeTileX = 5;
    private int _safeTileY = 10;

    public Game1(IRng? rng = null)
    {
        _rng = rng ?? new DefaultRng();
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "";

        _graphics.PreparingDeviceSettings += (_, e) =>
            e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage =
                RenderTargetUsage.PreserveContents;

        _graphics.PreferredBackBufferWidth = 640;
        _graphics.PreferredBackBufferHeight = 360;
        _graphics.IsFullScreen = false;
    }

    // ── Initialization ──────────────────────────────────────────

    protected override void Initialize()
    {
        _virt.Initialize(GraphicsDevice);
        _virt.Update(
            GraphicsDevice.PresentationParameters.BackBufferWidth,
            GraphicsDevice.PresentationParameters.BackBufferHeight);

        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += (_, _) =>
            _virt.Update(
                GraphicsDevice.PresentationParameters.BackBufferWidth,
                GraphicsDevice.PresentationParameters.BackBufferHeight);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("JoyMonFont");

        _title.LoadContent(Content);
        _debug.LoadContent(Content);
        _mapRenderer.LoadContent(GraphicsDevice);

        // Create programmatic player texture
        _playerTexture = CreatePlayerTexture(GraphicsDevice);

        // Create 1x1 pixel texture for solid rectangle drawing
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        // Load starter town map from JSON
        try
        {
            var mapDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "maps");
            var loader = new MapLoader(mapDir);
            _currentMap = loader.Load("starter-town.json");
            _mapRenderer.SetMap(_currentMap);
            _camera.Reset();

            if (_currentMap.SpawnPoint is not null)
            {
                _player.Initialize(_currentMap.SpawnPoint.X, _currentMap.SpawnPoint.Y);
                UpdateSafeRecoveryPoint(_currentMap.Id, _currentMap.SpawnPoint.X, _currentMap.SpawnPoint.Y);
            }
            else
            {
                _player.Initialize(0, 0);
                UpdateSafeRecoveryPoint(_currentMap.Id, 0, 0);
            }
        }
        catch (InvalidContentException ex)
        {
            // Map failed to load — will draw placeholder text instead
            System.Diagnostics.Debug.WriteLine($"Map load failed: {ex.Message}");
            _player.Initialize(0, 0);
        }

        // Load dialogues and NPCs from JSON
        try
        {
            var dialogueDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dialogue");
            if (Directory.Exists(dialogueDir))
            {
                var loader = new DialogueLoader(dialogueDir);
                foreach (var file in Directory.EnumerateFiles(dialogueDir, "*.json"))
                {
                    var dialogueFile = loader.Load(Path.GetFileName(file));

                    foreach (var npcContent in dialogueFile.Npcs)
                    {
                        var facing = ParseDirection(npcContent.FacingDirection);
                        var npc = new Npc(
                            npcContent.Id,
                            npcContent.Name,
                            npcContent.TilePosition.X,
                            npcContent.TilePosition.Y,
                            facing,
                            npcContent.DialogueId,
                            npcContent.SpriteId,
                            npcContent.MapId
                        );
                        _npcs.Add(npc);

                        if (!_npcTextures.ContainsKey(npc.SpriteId))
                        {
                            _npcTextures[npc.SpriteId] = CreateNpcTexture(GraphicsDevice, npc.SpriteId);
                        }
                    }

                    foreach (var dlgContent in dialogueFile.Dialogues)
                    {
                        _dialogues[dlgContent.Id] = dlgContent;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dialogue load failed: {ex.Message}");
        }

        // Load moves and creatures JSON database
        try
        {
            var loader = new ContentLoader(AppDomain.CurrentDomain.BaseDirectory);
            _contentDb = loader.Load();
            _saveService = new SaveService(_contentDb);
            _titleSelectionIndex = _saveService.SaveExists ? 0 : 1;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load creatures/moves database: {ex.Message}");
        }

        // Create starter textures
        _starterTextures["mossprout"] = CreateMossproutTexture(GraphicsDevice);
        _starterTextures["staticrow"] = CreateStaticrowTexture(GraphicsDevice);
        _starterTextures["pebblit"] = CreatePebblitTexture(GraphicsDevice);

        // Load encounters from JSON
        try
        {
            var encountersDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "encounters");
            if (Directory.Exists(encountersDir))
            {
                var loader = new EncounterLoader(encountersDir);
                var validCreatureIds = _contentDb != null ? _contentDb.Species.Keys.ToHashSet() : new HashSet<string>();
                foreach (var file in Directory.EnumerateFiles(encountersDir, "*.json"))
                {
                    var table = loader.Load(Path.GetFileName(file), validCreatureIds);
                    _encounterTables[table.MapId] = table;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Encounters load failed: {ex.Message}");
        }

        // Load trainers from JSON
        try
        {
            var trainersDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "trainers");
            if (Directory.Exists(trainersDir) && _contentDb is not null)
            {
                var loader = new TrainerLoader(trainersDir);
                var validCreatureIds = _contentDb.Species.Keys.ToHashSet();
                var validMoveIds = _contentDb.Moves.Keys.ToHashSet();

                foreach (var file in Directory.EnumerateFiles(trainersDir, "*.json"))
                {
                    var trainer = loader.Load(Path.GetFileName(file), validCreatureIds, validMoveIds);
                    _trainers[trainer.Id] = trainer;

                    var facing = ParseDirection(trainer.FacingDirection);
                    var trainerNpc = new Npc(
                        trainer.Id,
                        trainer.DisplayName,
                        trainer.TilePosition.X,
                        trainer.TilePosition.Y,
                        facing,
                        dialogueId: trainer.Id,
                        trainer.SpriteId,
                        trainer.MapId);
                    _npcs.Add(trainerNpc);

                    if (!_npcTextures.ContainsKey(trainer.SpriteId))
                    {
                        _npcTextures[trainer.SpriteId] = CreateNpcTexture(GraphicsDevice, trainer.SpriteId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Trainers load failed: {ex.Message}");
        }

        // Load bosses from JSON
        try
        {
            var bossesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bosses");
            if (Directory.Exists(bossesDir) && _contentDb is not null)
            {
                var loader = new BossLoader(bossesDir);
                var validCreatureIds = _contentDb.Species.Keys.ToHashSet();
                _bossesByMapId.Clear();
                foreach (var file in Directory.EnumerateFiles(bossesDir, "*.json"))
                {
                    var boss = loader.Load(Path.GetFileName(file), validCreatureIds);
                    _bossesByMapId[boss.MapId] = boss;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Boss load failed: {ex.Message}");
        }
    }

    private static Texture2D CreateMossproutTexture(GraphicsDevice device)
    {
        var tex = new Texture2D(device, 16, 16);
        var pixels = new Color[16 * 16];
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                pixels[y * 16 + x] = Color.Transparent;
                int dx = x - 8;
                int dy = y - 9;
                if (dx * dx + dy * dy <= 25) // Body
                {
                    pixels[y * 16 + x] = Color.LimeGreen;
                    if ((x == 6 || x == 10) && y == 9)
                        pixels[y * 16 + x] = Color.Black; // Eyes
                }
                // Leaf/stem details
                if (y >= 2 && y <= 4 && x == 8)
                    pixels[y * 16 + x] = Color.SaddleBrown;
                if (y == 2 && (x == 7 || x == 9))
                    pixels[y * 16 + x] = Color.ForestGreen;
            }
        }
        tex.SetData(pixels);
        return tex;
    }

    private static Texture2D CreateStaticrowTexture(GraphicsDevice device)
    {
        var tex = new Texture2D(device, 16, 16);
        var pixels = new Color[16 * 16];
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                pixels[y * 16 + x] = Color.Transparent;
                int dx = x - 8;
                int dy = y - 9;
                if (dx * dx + dy * dy <= 25) // Body
                {
                    pixels[y * 16 + x] = Color.Yellow;
                    if ((x == 6 || x == 10) && y == 9)
                        pixels[y * 16 + x] = Color.Black; // Eyes
                }
                // Beak
                if (y == 10 && x >= 7 && x <= 9)
                    pixels[y * 16 + x] = Color.Orange;
                // Wings
                if (y >= 8 && y <= 11 && (x == 2 || x == 3 || x == 13 || x == 14))
                    pixels[y * 16 + x] = Color.DarkGoldenrod;
            }
        }
        tex.SetData(pixels);
        return tex;
    }

    private static Texture2D CreatePebblitTexture(GraphicsDevice device)
    {
        var tex = new Texture2D(device, 16, 16);
        var pixels = new Color[16 * 16];
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                pixels[y * 16 + x] = Color.Transparent;
                if (x >= 4 && x <= 12 && y >= 5 && y <= 13) // Rock body
                {
                    pixels[y * 16 + x] = Color.Gray;
                    if ((x == 6 || x == 10) && y == 8)
                        pixels[y * 16 + x] = Color.Black; // Eyes
                    if ((x == 5 && y == 6) || (x == 11 && y == 12))
                        pixels[y * 16 + x] = Color.DimGray; // Crack details
                }
            }
        }
        tex.SetData(pixels);
        return tex;
    }

    private static Direction ParseDirection(string dirStr)
    {
        if (Enum.TryParse<Direction>(dirStr, ignoreCase: true, out var result))
            return result;
        return Direction.Down;
    }

    private static Direction GetOppositeDirection(Direction dir)
    {
        return dir switch
        {
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            Direction.Left => Direction.Right,
            Direction.Right => Direction.Left,
            _ => Direction.None
        };
    }

    private static Texture2D CreateNpcTexture(GraphicsDevice device, string spriteId)
    {
        var tex = new Texture2D(device, 16, 16);
        var pixels = new Color[16 * 16];

        if (spriteId == "sign")
        {
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    pixels[y * 16 + x] = Color.Transparent;
                    if (x >= 3 && x <= 12 && y >= 3 && y <= 9)
                        pixels[y * 16 + x] = Color.SaddleBrown;
                    if (x >= 4 && x <= 11 && y >= 4 && y <= 8)
                        pixels[y * 16 + x] = Color.Goldenrod;
                    if (x >= 7 && x <= 8 && y >= 10 && y <= 14)
                        pixels[y * 16 + x] = Color.SaddleBrown;
                }
            }

            tex.SetData(pixels);
            return tex;
        }
        
        Color clothingColor = spriteId switch
        {
            "dr-cedar" => Color.White,       // White lab coat
            "guard" => Color.DarkSlateBlue,  // Guard uniform
            "kid" => Color.ForestGreen,      // Green shirt
            "operator" => Color.DarkRed,     // Red/Orange shirt
            "rival" => Color.Crimson,
            _ => Color.Purple
        };

        Color hairColor = spriteId switch
        {
            "dr-cedar" => Color.Gray,
            "guard" => Color.Brown,
            "kid" => Color.Gold,
            "operator" => Color.DarkSlateGray,
            _ => Color.Black
        };

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                pixels[y * 16 + x] = Color.Transparent;
                
                int dx = x - 8;
                int dy = y - 8;
                if (dx * dx + dy * dy <= 49)
                {
                    if (y < 5)
                    {
                        pixels[y * 16 + x] = hairColor; // Cap/Hair
                    }
                    else if (y < 9)
                    {
                        if ((x == 5 || x == 10) && y == 7)
                            pixels[y * 16 + x] = Color.Black; // Eyes
                        else
                            pixels[y * 16 + x] = Color.PeachPuff; // Skin
                    }
                    else
                    {
                        pixels[y * 16 + x] = clothingColor; // Body/Clothes
                    }
                }
            }
        }
        
        tex.SetData(pixels);
        return tex;
    }

    private static Texture2D CreatePlayerTexture(GraphicsDevice device)
    {
        var tex = new Texture2D(device, 16, 16);
        var pixels = new Color[16 * 16];
        
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                pixels[y * 16 + x] = Color.Transparent;
                
                int dx = x - 8;
                int dy = y - 8;
                if (dx * dx + dy * dy <= 49) // Radius ~ 7
                {
                    if (y < 5)
                    {
                        pixels[y * 16 + x] = Color.Red; // Cap
                    }
                    else if (y < 9)
                    {
                        if ((x == 5 || x == 10) && y == 7)
                            pixels[y * 16 + x] = Color.Black; // Eyes
                        else
                            pixels[y * 16 + x] = Color.PeachPuff; // Skin
                    }
                    else
                    {
                        pixels[y * 16 + x] = Color.Blue; // Body
                    }
                }
            }
        }
        
        tex.SetData(pixels);
        return tex;
    }

    // ── Update ──────────────────────────────────────────────────

    protected override void Update(GameTime gameTime)
    {
        _input.Update();
        _debug.Update(gameTime);
        TrackPlayTime(gameTime);

        if (_input.DebugTogglePressed)
            _debug.Toggle();

        switch (State)
        {
            case GameState.Boot:
                UpdateBoot(gameTime);
                break;
            case GameState.Title:
                UpdateTitle(gameTime);
                break;
            case GameState.Overworld:
                UpdateOverworld(gameTime);
                break;
            case GameState.Battle:
                UpdateBattle(gameTime);
                break;
            case GameState.StarterChoice:
                UpdateStarterChoice(gameTime);
                break;
            case GameState.PartyScreen:
                UpdatePartyScreen(gameTime);
                break;
            case GameState.InventoryScreen:
                UpdateInventoryScreen(gameTime);
                break;
            case GameState.EndingScreen:
                UpdateEndingScreen(gameTime);
                break;
        }

        if (_saveStatusTimer > 0)
        {
            _saveStatusTimer -= gameTime.ElapsedGameTime.TotalSeconds;
            if (_saveStatusTimer <= 0)
                _saveStatusMessage = null;
        }

        base.Update(gameTime);
    }

    private void UpdateBoot(GameTime gameTime)
    {
        if (_input.StartPressed)
        {
            State = GameState.Title;
            return;
        }
        _bootTimer += gameTime.ElapsedGameTime.TotalSeconds;
        if (_bootTimer >= 1.5)
            State = GameState.Title;
    }

    private void UpdateTitle(GameTime gameTime)
    {
        _title.Update(gameTime);

        var continueEnabled = _saveService?.SaveExists == true;
        if (!continueEnabled)
            _titleSelectionIndex = 1;

        if (continueEnabled)
        {
            if (_input.UpPressed || _input.DownPressed)
                _titleSelectionIndex = _titleSelectionIndex == 0 ? 1 : 0;
        }

        if (_input.StartPressed)
        {
            if (_titleSelectionIndex == 0 && continueEnabled)
                LoadSavedGame();
            else
                StartNewGame();
        }
    }

    private void StartNewGame()
    {
        _profile.Name = "Player";
        _profile.Party.Clear();
        _profile.ResetDefaultItems();
        _profile.Flags.Clear();
        _profile.Captures.Clear();
        _profile.PlayTimeSeconds = 0;
        _defeatedTrainers.Clear();
        _endingData = null;
        _pendingBoss = null;
        _activeBoss = null;
        _talkingToDrCedar = false;
        _dialogue.Close();

        TransitionToMap("starter-town", 5, 10, autosave: false);
        State = GameState.Overworld;
    }

    private void LoadSavedGame()
    {
        if (_saveService is null)
            return;

        try
        {
            var save = _saveService.LoadSave();
            TransitionToMap(save.CurrentMap, save.PlayerTilePosition.X, save.PlayerTilePosition.Y, autosave: false);
            _saveService.Restore(save, _profile, _player);
            _defeatedTrainers.Clear();
            foreach (var trainerId in save.DefeatedTrainers)
                _defeatedTrainers.Add(trainerId);

            _talkingToDrCedar = false;
            _dialogue.Close();
            State = GameState.Overworld;
        }
        catch (Exception ex)
        {
            _saveStatusMessage = ex.Message;
            _saveStatusTimer = 3.0;
            System.Diagnostics.Debug.WriteLine($"Failed to load save: {ex.Message}");
        }
    }

    private void SaveCurrentGame()
    {
        if (_saveService is null || _currentMap is null)
            return;

        try
        {
            _saveService.Save(_profile, _player, _currentMap.Id, _defeatedTrainers);
            _saveStatusMessage = "Saved.";
            _saveStatusTimer = 2.0;
        }
        catch (Exception ex)
        {
            _saveStatusMessage = "Save failed.";
            _saveStatusTimer = 3.0;
            System.Diagnostics.Debug.WriteLine($"Failed to save game: {ex.Message}");
        }
    }

    private void AutosaveCurrentGame()
    {
        if (_saveService is null || _currentMap is null)
            return;

        try
        {
            _saveService.Save(_profile, _player, _currentMap.Id, _defeatedTrainers);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Autosave failed: {ex.Message}");
        }
    }

    private void UpdateOverworld(GameTime gameTime)
    {
        if (_dialogue.IsActive)
        {
            if (_input.ConfirmPressed)
            {
                _dialogue.Advance();
                if (!_dialogue.IsActive)
                {
                    _dialogue.Close();

                    if (_talkingToDrCedar)
                    {
                        _talkingToDrCedar = false;
                        if (!_profile.HasFlag("received_starter"))
                        {
                            State = GameState.StarterChoice;
                        }
                    }

                    if (_pendingTrainerBattleId is not null)
                    {
                        var trainerId = _pendingTrainerBattleId;
                        _pendingTrainerBattleId = null;
                        StartTrainerBattle(trainerId);
                    }
                    else if (_pendingBoss is not null)
                    {
                        var boss = _pendingBoss;
                        _pendingBoss = null;
                        StartBossBattle(boss);
                    }
                }
            }
            return; // Lock input/movement during dialogue
        }

        if (_input.CancelPressed)
        {
            State = GameState.PartyScreen;
            return;
        }

        if (_input.InventoryPressed)
        {
            State = GameState.InventoryScreen;
            return;
        }

        if (_input.ConfirmPressed)
        {
            // Interact in front of player
            int dx = 0;
            int dy = 0;
            switch (_player.Facing)
            {
                case Direction.Up: dy = -1; break;
                case Direction.Down: dy = 1; break;
                case Direction.Left: dx = -1; break;
                case Direction.Right: dx = 1; break;
            }
            int tx = _player.X + dx;
            int ty = _player.Y + dy;

            var mapId = _currentMap?.Id ?? string.Empty;
            var npc = _npcs.FirstOrDefault(n => n.MapId == mapId && n.X == tx && n.Y == ty);
            if (npc is not null)
            {
                npc.Facing = GetOppositeDirection(_player.Facing);

                if (_trainers.TryGetValue(npc.Id, out var trainer))
                {
                    InteractWithTrainer(trainer);
                    return;
                }

                if (npc.Id == "dr-cedar")
                {
                    _talkingToDrCedar = true;
                }
                else if (npc.Id is "trial-grove-healer" or "ashbend-healer" or "ashbend-mine-healer" or "snowbell-healer")
                {
                    HealParty();
                    AutosaveCurrentGame();
                }
                string dialogueId = npc.DialogueId;
                if (npc.Id == "ashbend-foreman")
                {
                    if (!_profile.HasFlag(MineFlags.MinePass))
                    {
                        _profile.SetFlag(MineFlags.MinePass, true);
                        dialogueId = "ashbend-foreman-talk";
                        AutosaveCurrentGame();
                    }
                    else
                    {
                        dialogueId = "ashbend-foreman-talk-pass";
                    }
                }
                else if (npc.Id == "dr-cedar" && _profile.HasFlag("received_starter"))
                {
                    dialogueId = "dr-cedar-after";
                }
                else if (npc.Id == "sleepy-guard" && !_profile.HasFlag("received_starter"))
                {
                    dialogueId = "sleepy-guard-block";
                }
                else if (npc.MapId == "ashbend-camp" && _profile.HasFlag("ashbend_mine_cleared"))
                {
                    var clearedDialogueId = $"{npc.DialogueId}-cleared";
                    if (_dialogues.ContainsKey(clearedDialogueId))
                        dialogueId = clearedDialogueId;
                }

                if (_dialogues.TryGetValue(dialogueId, out var dlg))
                {
                    _dialogue.Start(dlg.Speaker, dlg.Lines);
                }
                return;
            }

            if (_currentMap is not null
                && MapInteractionService.TryInteract(_currentMap, _profile, tx, ty, out var triggerMessage, HealParty))
            {
                if (!string.IsNullOrWhiteSpace(triggerMessage))
                    _dialogue.Start("Notice", new[] { triggerMessage });
                AutosaveCurrentGame();
                return;
            }
        }

        // Track last position to detect step completion
        int lastX = _player.X;
        int lastY = _player.Y;

        // Determine input direction
        Direction inputDir = Direction.None;
        if (_input.UpHeld) inputDir = Direction.Up;
        else if (_input.DownHeld) inputDir = Direction.Down;
        else if (_input.LeftHeld) inputDir = Direction.Left;
        else if (_input.RightHeld) inputDir = Direction.Right;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _player.Update(dt, inputDir, (x, y) =>
        {
            if (_currentMap is null) return false;
            // Map boundary check
            if (x < 0 || x >= _currentMap.Width || y < 0 || y >= _currentMap.Height)
                return false;
            // NPC solid collision check
            if (_npcs.Any(npc => npc.MapId == _currentMap.Id && npc.X == x && npc.Y == y))
                return false;
            // Collision layer check
            if (!MapInteractionService.IsTileWalkable(_currentMap, _profile, x, y))
            {
                if (MapInteractionService.TryGetBlockedMessage(_currentMap, _profile, x, y, out var blockedMessage)
                    && !string.IsNullOrWhiteSpace(blockedMessage))
                {
                    _dialogue.Start("Notice", new[] { blockedMessage });
                }
                return false;
            }

            return true;
        });

        // Center camera on player visual position
        if (_currentMap is not null)
        {
            float visualX = MathHelper.Lerp(_player.X, _player.TargetX, _player.MoveProgress);
            float visualY = MathHelper.Lerp(_player.Y, _player.TargetY, _player.MoveProgress);

            _camera.CenterOn(
                visualX * _currentMap.TileSize,
                visualY * _currentMap.TileSize,
                VirtualResolution.VirtualWidth,
                VirtualResolution.VirtualHeight,
                _currentMap.Width * _currentMap.TileSize,
                _currentMap.Height * _currentMap.TileSize
            );
        }

        // Check for map transition
        bool transitionOccurred = false;
        if (_player.State == MovementState.Idle && _currentMap is not null)
        {
            var transition = _currentMap.Transitions.FirstOrDefault(t => t.FromTile.X == _player.X && t.FromTile.Y == _player.Y);
            if (transition is not null)
            {
                if (string.IsNullOrEmpty(transition.RequiredFlag) || _profile.HasFlag(transition.RequiredFlag))
                {
                    TransitionToMap(transition.ToMapId, transition.ToTile.X, transition.ToTile.Y);
                    transitionOccurred = true;
                }
            }
        }

        // Check for grass encounter if player successfully completed a step onto a new tile
        if (!transitionOccurred && (_player.X != lastX || _player.Y != lastY))
        {
            if (TryTriggerBossGate())
                return;

            if (_profile.HasFlag("received_starter") && _currentMap is not null)
            {
                if (_encounterTables.TryGetValue(_currentMap.Id, out var table))
                {
                    int tileId = _currentMap.Layers.Ground[_player.Y][_player.X];
                    if (table.TileIds.Contains(tileId))
                    {
                        if (_rng.NextDouble() < table.EncounterRate)
                        {
                            TriggerEncounter(table);
                        }
                    }
                }
            }
        }
    }

    private bool TryTriggerBossGate()
    {
        if (_currentMap is null || _dialogue.IsActive)
            return false;

        if (!_bossesByMapId.TryGetValue(_currentMap.Id, out var boss))
            return false;

        var trigger = BossInteraction.TryTriggerGate(
            boss,
            _profile,
            _currentMap.Id,
            _player.X,
            _player.Y);

        if (trigger != BossGateTriggerResult.StartIntroDialogue)
            return false;

        _dialogue.Start(boss.IntroDialogue.Speaker, boss.IntroDialogue.Lines);
        _pendingBoss = boss;
        return true;
    }

    private void TriggerEncounter(EncounterTableContent table)
    {
        if (table.Entries == null || table.Entries.Count == 0) return;

        int totalWeight = table.Entries.Sum(e => e.Weight);
        if (totalWeight <= 0) return;

        int roll = _rng.Next(totalWeight);
        int currentSum = 0;
        EncounterEntryContent? selectedEntry = null;

        foreach (var entry in table.Entries)
        {
            currentSum += entry.Weight;
            if (roll < currentSum)
            {
                selectedEntry = entry;
                break;
            }
        }

        if (selectedEntry is not null)
        {
            int level = selectedEntry.MinLevel;
            if (selectedEntry.MaxLevel > selectedEntry.MinLevel)
            {
                level = _rng.Next(selectedEntry.MinLevel, selectedEntry.MaxLevel + 1);
            }

            if (_contentDb != null && _contentDb.Species.TryGetValue(selectedEntry.CreatureId, out var species))
            {
                StartWildBattle(species.CreateInstance(level), selectedEntry.CreatureId);
            }
        }
    }

    private void StartWildBattle(JoyMonInstance wildJoyMon, string? speciesId = null)
    {
        var activeJoyMon = _profile.Party.FirstOrDefault(joymon => !joymon.IsFainted)
            ?? _profile.Party.FirstOrDefault();

        if (activeJoyMon is null)
            return;

        _activeTrainerId = null;
        _activeBossBattle = false;
        _activeBoss = null;
        _lastWildSpeciesId = speciesId;
        _wildEncounter = wildJoyMon;
        _battleScene = new BattleScene(activeJoyMon, wildJoyMon, new BattleRngAdapter(_rng), _profile);

        // Stop movement state immediately on transition to avoid chaining issues later.
        _player.State = MovementState.Idle;
        _player.TargetX = _player.X;
        _player.TargetY = _player.Y;
        _player.MoveProgress = 1.0f;

        State = GameState.Battle;
    }

    private void InteractWithTrainer(TrainerContent trainer)
    {
        var interaction = TrainerInteraction.Resolve(trainer, _defeatedTrainers);
        _dialogue.Start(interaction.Dialogue.Speaker, interaction.Dialogue.Lines);

        if (interaction.Kind == TrainerInteractionKind.ShowDialogueThenBattle
            && TrainerInteraction.CanStartBattle(trainer, _defeatedTrainers))
        {
            _pendingTrainerBattleId = trainer.Id;
        }
    }

    private void StartTrainerBattle(string trainerId)
    {
        if (!_trainers.TryGetValue(trainerId, out var trainer))
            return;

        if (!TrainerInteraction.CanStartBattle(trainer, _defeatedTrainers))
            return;

        var activeJoyMon = _profile.Party.FirstOrDefault(joymon => !joymon.IsFainted)
            ?? _profile.Party.FirstOrDefault();

        if (activeJoyMon is null || trainer.Party.Count == 0)
            return;

        var opponent = CreateTrainerJoyMon(trainer.Party[0]);
        if (opponent is null)
            return;

        _activeTrainerId = trainerId;
        _activeBossBattle = false;
        _activeBoss = null;
        _wildEncounter = null;
        _battleScene = new BattleScene(
            activeJoyMon,
            opponent,
            new BattleRngAdapter(_rng),
            _profile,
            isTrainerBattle: true,
            opponentTrainerName: trainer.DisplayName);

        _player.State = MovementState.Idle;
        _player.TargetX = _player.X;
        _player.TargetY = _player.Y;
        _player.MoveProgress = 1.0f;

        State = GameState.Battle;
    }

    private JoyMonInstance? CreateTrainerJoyMon(TrainerPartyMemberContent member)
    {
        if (_contentDb is null)
            return null;

        if (!_contentDb.Species.TryGetValue(member.CreatureId, out var species))
            return null;

        return species.CreateInstance(member.Level);
    }

    private void StartBossBattle(BossContent boss)
    {
        if (BossInteraction.IsCleared(_profile, boss))
            return;

        var activeJoyMon = _profile.Party.FirstOrDefault(joymon => !joymon.IsFainted)
            ?? _profile.Party.FirstOrDefault();

        if (activeJoyMon is null || _contentDb is null)
            return;

        if (!_contentDb.Species.TryGetValue(boss.CreatureId, out var species))
            return;

        var opponent = species.CreateInstance(boss.Level);
        _activeTrainerId = null;
        _activeBossBattle = true;
        _activeBoss = boss;
        _wildEncounter = null;
        _battleScene = new BattleScene(
            activeJoyMon,
            opponent,
            new BattleRngAdapter(_rng),
            _profile,
            isBossBattle: true,
            bossDisplayName: boss.DisplayName);

        _player.State = MovementState.Idle;
        _player.TargetX = _player.X;
        _player.TargetY = _player.Y;
        _player.MoveProgress = 1.0f;

        State = GameState.Battle;
    }

    private void UpdateBattle(GameTime gameTime)
    {
        if (_battleScene is null)
        {
            _wildEncounter = null;
            State = GameState.Overworld;
            return;
        }

        if (_input.UpPressed)
            _battleScene.MoveUp();
        else if (_input.DownPressed)
            _battleScene.MoveDown();

        if (_input.CancelPressed)
            _battleScene.Cancel();

        if (_input.ConfirmPressed)
            _battleScene.Confirm();

        if (_battleScene.Mode == BattleSceneMode.Finished)
        {
            CompleteBattle(_battleScene.Outcome);
        }
    }

    private void CompleteBattle(BattleSceneOutcome outcome)
    {
        var trainerId = _activeTrainerId;
        var wasTrainerBattle = trainerId is not null;
        var wasBossBattle = _activeBossBattle;
        var boss = _activeBoss;
        _activeTrainerId = null;
        _activeBossBattle = false;
        _activeBoss = null;
        _battleScene = null;
        _wildEncounter = null;

        if (trainerId is not null)
            TrainerInteraction.RecordDefeat(_defeatedTrainers, trainerId, outcome);

        if (outcome == BattleSceneOutcome.Captured && !string.IsNullOrWhiteSpace(_lastWildSpeciesId))
            _profile.RecordCapture(_lastWildSpeciesId);
        _lastWildSpeciesId = null;

        if (wasBossBattle && boss is not null && outcome == BattleSceneOutcome.Won)
        {
            BossInteraction.RecordVictory(_profile, boss);
            if (boss.Id == "lanternox")
            {
                ShowEndingScreen();
                AutosaveCurrentGame();
                return;
            }

            AutosaveCurrentGame();
        }

        if (outcome == BattleSceneOutcome.Won && trainerId is not null && _trainers.TryGetValue(trainerId, out var trainer))
        {
            _dialogue.Start(trainer.DialogueAfter.Speaker, trainer.DialogueAfter.Lines);
            AutosaveCurrentGame();
        }
        else if (outcome == BattleSceneOutcome.Lost)
        {
            HealParty();
            _profile.SetFlag("last_battle_lost", true);
            if (!wasTrainerBattle && !wasBossBattle)
                TransitionToMap(_safeMapId, _safeTileX, _safeTileY);
            else
                AutosaveCurrentGame();
        }

        State = GameState.Overworld;
    }

    private void ShowEndingScreen()
    {
        _endingData = new EndingScreenData
        {
            Party = _profile.Party.ToList(),
            Captures = _profile.Captures.ToList(),
            PlayTimeSeconds = _profile.PlayTimeSeconds,
        };
        State = GameState.EndingScreen;
    }

    private void TrackPlayTime(GameTime gameTime)
    {
        if (State is GameState.Title or GameState.Boot or GameState.EndingScreen)
            return;

        _profile.PlayTimeSeconds += gameTime.ElapsedGameTime.TotalSeconds;
    }

    private void UpdateEndingScreen(GameTime gameTime)
    {
        if (_input.ConfirmPressed || _input.StartPressed)
        {
            _endingData = null;
            State = GameState.Title;
        }
    }

    private void HealParty()
    {
        foreach (var joymon in _profile.Party)
        {
            joymon.CurrentHp = joymon.MaxHp;
            for (int i = 0; i < joymon.RemainingUses.Length && i < joymon.Species.Moves.Count; i++)
            {
                joymon.RemainingUses[i] = joymon.Species.Moves[i].MaxUses;
            }
        }
    }

    private void UpdateStarterChoice(GameTime gameTime)
    {
        if (_input.LeftPressed)
        {
            _starterChoiceIndex = (_starterChoiceIndex + 2) % 3;
        }
        else if (_input.RightPressed)
        {
            _starterChoiceIndex = (_starterChoiceIndex + 1) % 3;
        }
        else if (_input.ConfirmPressed)
        {
            string chosenId = _starterIds[_starterChoiceIndex];
            if (_contentDb != null && _contentDb.Species.TryGetValue(chosenId, out var species))
            {
                var starter = species.CreateInstance(5);
                _profile.Party.Add(starter);
                _profile.SetFlag("received_starter", true);
                AutosaveCurrentGame();

                _dialogue.Start("Dr. Cedar", new[] { 
                    $"You chose {species.Name}!",
                    "Take good care of your partner!"
                });
                State = GameState.Overworld;
            }
            else
            {
                // Fallback to built-in SpeciesLibrary definitions if JSON content fails
                var speciesFallback = SpeciesLibrary.All[_starterChoiceIndex];
                var starter = speciesFallback.CreateInstance(5);
                _profile.Party.Add(starter);
                _profile.SetFlag("received_starter", true);
                AutosaveCurrentGame();

                _dialogue.Start("Dr. Cedar", new[] { 
                    $"You chose {speciesFallback.Name}!",
                    "Take good care of your partner!"
                });
                State = GameState.Overworld;
            }
        }
    }

    private void UpdatePartyScreen(GameTime gameTime)
    {
        if (_input.ConfirmPressed)
        {
            SaveCurrentGame();
            return;
        }

        if (_input.CancelPressed)
        {
            State = GameState.Overworld;
        }
    }

    private void UpdateInventoryScreen(GameTime gameTime)
    {
        if (_input.CancelPressed)
        {
            State = GameState.Overworld;
        }
    }

    // ── Draw ────────────────────────────────────────────────────

    protected override void Draw(GameTime gameTime)
    {
        _virt.BeginDraw(GraphicsDevice);

        switch (State)
        {
            case GameState.Boot:
                DrawBoot();
                break;
            case GameState.Title:
                DrawTitle();
                break;
            case GameState.Overworld:
                DrawOverworld(gameTime);
                break;
            case GameState.Battle:
                DrawBattle();
                break;
            case GameState.StarterChoice:
                DrawStarterChoice(gameTime);
                break;
            case GameState.PartyScreen:
                DrawPartyScreen();
                break;
            case GameState.InventoryScreen:
                DrawInventoryScreen();
                break;
            case GameState.EndingScreen:
                DrawEndingScreen();
                break;
        }

        _debug.Draw(
            _spriteBatch,
            State,
            _virt.Scale,
            _currentMap?.Id ?? "none",
            _player.X,
            _player.Y,
            _player.Facing.ToString()
        );
        _virt.EndDraw(_spriteBatch, GraphicsDevice);

        base.Draw(gameTime);
    }

    private void DrawBoot()
    {
        _spriteBatch.Begin();
        _spriteBatch.DrawString(_font, "Loading...", new Vector2(4, 4), Color.Gray);
        _spriteBatch.End();
    }

    private void DrawTitle()
    {
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _title.Draw(_spriteBatch, _saveService?.SaveExists == true, _titleSelectionIndex);
        if (!string.IsNullOrWhiteSpace(_saveStatusMessage))
        {
            var size = _font.MeasureString(_saveStatusMessage);
            _spriteBatch.DrawString(_font, _saveStatusMessage, new Vector2((320 - size.X) / 2f, 162), Color.Yellow);
        }
        _spriteBatch.End();
    }

    private void DrawOverworld(GameTime gameTime)
    {
        if (_currentMap is not null && _mapRenderer.CurrentMap is not null)
        {
            var cameraMatrix = Matrix.CreateTranslation(-_camera.X, -_camera.Y, 0);
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: cameraMatrix);

            _mapRenderer.Draw(_spriteBatch);

            // Draw NPCs
            foreach (var npc in _npcs)
            {
                if (npc.MapId == _currentMap.Id && _npcTextures.TryGetValue(npc.SpriteId, out var npcTex))
                {
                    _spriteBatch.Draw(
                        npcTex,
                        new Vector2(npc.X * _currentMap.TileSize, npc.Y * _currentMap.TileSize),
                        Color.White
                    );
                }
            }

            // Draw player visual position interpolated smoothly
            float visualX = MathHelper.Lerp(_player.X, _player.TargetX, _player.MoveProgress);
            float visualY = MathHelper.Lerp(_player.Y, _player.TargetY, _player.MoveProgress);
            _spriteBatch.Draw(
                _playerTexture,
                new Vector2(visualX * _currentMap.TileSize, visualY * _currentMap.TileSize),
                Color.White
            );

            _spriteBatch.End();

            // Draw UI elements without camera offset
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.DrawString(
                _font,
                _currentMap.Name,
                new Vector2(4, 4),
                new Color(255, 255, 255, 160)
            );

            // Draw Dialogue Box if active
            if (_dialogue.IsActive)
            {
                // Background dark box with border (x=4, y=126, w=312, h=50)
                DrawBorderedRect(_spriteBatch, new Rectangle(4, 126, 312, 50), new Color(15, 15, 15, 235), new Color(180, 180, 180), 1);

                // Speaker Name
                _spriteBatch.DrawString(_font, $"[{_dialogue.Speaker}]", new Vector2(10, 129), Color.Gold);

                // Dialogue Line
                _spriteBatch.DrawString(_font, _dialogue.CurrentLine, new Vector2(10, 144), Color.White);

                // Pulsing indicator arrow
                double time = gameTime.TotalGameTime.TotalSeconds;
                float pulse = (float)Math.Sin(time * 6.0) * 1.5f;
                if (((int)(time * 2)) % 2 == 0)
                {
                    _spriteBatch.DrawString(_font, ">", new Vector2(306, 159 + pulse), Color.Yellow);
                }
            }

            _spriteBatch.End();
        }
        else
        {
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            var text = "Overworld (map unavailable)";
            var size = _font.MeasureString(text);
            _spriteBatch.DrawString(_font, text,
                new Vector2((320 - size.X) / 2f, (180 - size.Y) / 2f),
                Color.CornflowerBlue);
            _spriteBatch.End();
        }
    }

    private void DrawBorderedRect(SpriteBatch batch, Rectangle rect, Color bgColor, Color borderColor, int borderThickness)
    {
        // Draw background
        batch.Draw(_pixel, rect, bgColor);

        // Draw borders
        batch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, borderThickness), borderColor); // Top
        batch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - borderThickness, rect.Width, borderThickness), borderColor); // Bottom
        batch.Draw(_pixel, new Rectangle(rect.X, rect.Y, borderThickness, rect.Height), borderColor); // Left
        batch.Draw(_pixel, new Rectangle(rect.Right - borderThickness, rect.Y, borderThickness, rect.Height), borderColor); // Right
    }

    private void DrawBattle()
    {
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, 320, 180), new Color(36, 44, 54));

        if (_battleScene is null)
        {
            var text = "Returning to overworld...";
            var size = _font.MeasureString(text);
            _spriteBatch.DrawString(_font, text, new Vector2((320 - size.X) / 2f, 82), Color.White);
            _spriteBatch.End();
            return;
        }

        var enemy = _battleScene.State.OpponentJoyMon;
        var player = _battleScene.State.PlayerJoyMon;

        DrawJoyMonStand(enemy, new Rectangle(216, 44, 48, 34), isEnemy: true);
        DrawJoyMonStand(player, new Rectangle(54, 92, 52, 38), isEnemy: false);

        DrawHpPanel(enemy, new Rectangle(12, 14, 132, 42), showHpText: true);
        DrawHpPanel(player, new Rectangle(176, 86, 132, 42), showHpText: true);

        DrawBorderedRect(_spriteBatch, new Rectangle(4, 132, 312, 44), new Color(15, 15, 18, 245), Color.White, 1);

        if (_battleScene.Mode == BattleSceneMode.Command)
        {
            DrawMessageText("What will you do?", new Vector2(12, 140));
            DrawCommandMenu(_battleScene);
        }
        else if (_battleScene.Mode == BattleSceneMode.Fight)
        {
            DrawFightMenu(_battleScene);
        }
        else if (_battleScene.Mode == BattleSceneMode.Item)
        {
            DrawItemMenu(_battleScene);
        }
        else
        {
            DrawMessageText(_battleScene.CurrentMessage, new Vector2(12, 144));
        }

        _spriteBatch.End();
    }

    private void DrawJoyMonStand(JoyMonInstance joymon, Rectangle rect, bool isEnemy)
    {
        var bodyColor = TypeColor(joymon.Species.Type);
        var shadowRect = new Rectangle(rect.X - 6, rect.Bottom - 5, rect.Width + 12, 7);
        _spriteBatch.Draw(_pixel, shadowRect, new Color(0, 0, 0, 80));

        var bodyRect = isEnemy
            ? new Rectangle(rect.X + 8, rect.Y, rect.Width - 12, rect.Height)
            : new Rectangle(rect.X, rect.Y + 2, rect.Width - 10, rect.Height - 2);

        _spriteBatch.Draw(_pixel, bodyRect, bodyColor);
        _spriteBatch.Draw(_pixel, new Rectangle(bodyRect.X + 6, bodyRect.Y + 6, 4, 4), Color.Black);
        _spriteBatch.Draw(_pixel, new Rectangle(bodyRect.Right - 10, bodyRect.Y + 6, 4, 4), Color.Black);
        _spriteBatch.Draw(_pixel, new Rectangle(bodyRect.X + 6, bodyRect.Bottom - 7, bodyRect.Width - 12, 3), Color.White);
    }

    private void DrawHpPanel(JoyMonInstance joymon, Rectangle rect, bool showHpText)
    {
        DrawBorderedRect(_spriteBatch, rect, new Color(248, 248, 240), Color.Black, 1);

        _spriteBatch.DrawString(_font, joymon.Species.Name, new Vector2(rect.X + 6, rect.Y + 4), Color.Black);
        _spriteBatch.DrawString(_font, $"Lv.{joymon.Level}", new Vector2(rect.Right - 38, rect.Y + 4), Color.Black);

        var hpBarRect = new Rectangle(rect.X + 30, rect.Y + 22, rect.Width - 42, 7);
        _spriteBatch.DrawString(_font, "HP", new Vector2(rect.X + 8, rect.Y + 19), Color.DarkGreen);
        _spriteBatch.Draw(_pixel, hpBarRect, Color.Black);

        int hpWidth = 0;
        if (joymon.MaxHp > 0)
        {
            float hpRatio = MathHelper.Clamp((float)joymon.CurrentHp / joymon.MaxHp, 0f, 1f);
            hpWidth = (int)((hpBarRect.Width - 2) * hpRatio);
        }

        var hpColor = joymon.CurrentHp <= joymon.MaxHp / 4 ? Color.Red :
            joymon.CurrentHp <= joymon.MaxHp / 2 ? Color.Gold : Color.LimeGreen;
        _spriteBatch.Draw(_pixel, new Rectangle(hpBarRect.X + 1, hpBarRect.Y + 1, hpWidth, hpBarRect.Height - 2), hpColor);

        if (showHpText)
        {
            var hpText = $"{Math.Max(0, joymon.CurrentHp)}/{joymon.MaxHp}";
            var hpSize = _font.MeasureString(hpText);
            _spriteBatch.DrawString(_font, hpText, new Vector2(rect.Right - hpSize.X - 6, rect.Y + 31), Color.Black);
        }

        var statusLabel = joymon.BattleStatusLabel;
        if (!string.IsNullOrEmpty(statusLabel))
        {
            var statusColor = statusLabel == "BRN" ? new Color(200, 60, 40) : new Color(50, 90, 160);
            _spriteBatch.DrawString(_font, statusLabel, new Vector2(rect.X + 8, rect.Y + 31), statusColor);
        }
    }

    private void DrawCommandMenu(BattleScene scene)
    {
        var menuRect = new Rectangle(202, 134, 106, 38);
        DrawBorderedRect(_spriteBatch, menuRect, new Color(248, 248, 240), Color.Black, 1);

        for (int i = 0; i < scene.Commands.Count; i++)
        {
            int y = menuRect.Y + 4 + i * 11;
            _spriteBatch.DrawString(_font, scene.CommandIndex == i ? ">" : " ", new Vector2(menuRect.X + 6, y), Color.Black);
            _spriteBatch.DrawString(_font, scene.Commands[i], new Vector2(menuRect.X + 18, y), Color.Black);
        }
    }

    private void DrawItemMenu(BattleScene scene)
    {
        DrawMessageText("Use which item?", new Vector2(12, 140));

        var menuRect = new Rectangle(202, 134, 106, 38);
        DrawBorderedRect(_spriteBatch, menuRect, new Color(248, 248, 240), Color.Black, 1);

        for (int i = 0; i < scene.BattleItems.Count; i++)
        {
            if (!ItemCatalog.TryGet(scene.BattleItems[i], out var definition))
                continue;

            int y = menuRect.Y + 4 + i * 11;
            _spriteBatch.DrawString(_font, scene.ItemIndex == i ? ">" : " ", new Vector2(menuRect.X + 6, y), Color.Black);
            _spriteBatch.DrawString(_font, definition.Name, new Vector2(menuRect.X + 18, y), Color.Black);
        }
    }

    private void DrawFightMenu(BattleScene scene)
    {
        for (int i = 0; i < scene.KnownMoves.Count; i++)
        {
            var move = scene.KnownMoves[i];
            int x = 14 + (i % 2) * 138;
            int y = 140 + (i / 2) * 16;
            var color = scene.MoveIndex == i ? Color.Yellow : Color.White;
            _spriteBatch.DrawString(_font, scene.MoveIndex == i ? ">" : " ", new Vector2(x, y), color);
            _spriteBatch.DrawString(_font, move.Name, new Vector2(x + 12, y), color);

            string pp = $"{scene.State.PlayerJoyMon.RemainingUses[i]}/{move.MaxUses}";
            var ppSize = _font.MeasureString(pp);
            _spriteBatch.DrawString(_font, pp, new Vector2(x + 126 - ppSize.X, y), Color.LightGray);
        }
    }

    private void DrawMessageText(string text, Vector2 position)
    {
        _spriteBatch.DrawString(_font, text, position, Color.White);
    }

    private static Color TypeColor(JoyMonType type)
    {
        return type switch
        {
            JoyMonType.Moss => Color.ForestGreen,
            JoyMonType.Spark => Color.Gold,
            JoyMonType.Stone => Color.Gray,
            JoyMonType.Ember => Color.OrangeRed,
            JoyMonType.Tide => Color.DeepSkyBlue,
            JoyMonType.Echo => Color.MediumPurple,
            JoyMonType.Frost => Color.LightCyan,
            _ => Color.LightSteelBlue,
        };
    }

    private void DrawStarterChoice(GameTime gameTime)
    {
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // Draw title
        var titleText = "CHOOSE YOUR STARTER JOYMON";
        var titleSize = _font.MeasureString(titleText);
        _spriteBatch.DrawString(_font, titleText, new Vector2((320 - titleSize.X) / 2f, 10), Color.Gold);

        // Draw cards
        for (int i = 0; i < 3; i++)
        {
            string id = _starterIds[i];
            var species = _contentDb?.Species.GetValueOrDefault(id);
            if (species is null)
            {
                // Fallback to built-in templates if JSON database not loaded
                species = SpeciesLibrary.All[i];
            }

            int x = 20 + i * 100;
            int y = 32;
            int w = 80;
            int h = 136;

            var cardRect = new Rectangle(x, y, w, h);
            var isSelected = (i == _starterChoiceIndex);
            var borderColor = isSelected ? Color.Gold : Color.DimGray;
            var bgColor = isSelected ? new Color(35, 35, 35, 240) : new Color(20, 20, 20, 240);

            DrawBorderedRect(_spriteBatch, cardRect, bgColor, borderColor, isSelected ? 2 : 1);

            // Draw Name
            var nameSize = _font.MeasureString(species.Name);
            _spriteBatch.DrawString(_font, species.Name, new Vector2(x + (w - nameSize.X) / 2f, y + 6), Color.White);

            // Draw Sprite scaled 2x (32x32)
            if (_starterTextures.TryGetValue(id, out var tex))
            {
                _spriteBatch.Draw(tex, new Rectangle(x + (w - 32) / 2, y + 24, 32, 32), Color.White);
            }

            // Draw Type
            var typeText = $"Type: {species.Type}";
            var typeSize = _font.MeasureString(typeText);
            _spriteBatch.DrawString(_font, typeText, new Vector2(x + (w - typeSize.X) / 2f, y + 62), Color.LightGray);

            // Draw Stats
            int statY = y + 78;
            _spriteBatch.DrawString(_font, $"HP:  {species.BaseMaxHp}", new Vector2(x + 10, statY), Color.White);
            _spriteBatch.DrawString(_font, $"ATK: {species.BaseAttack}", new Vector2(x + 10, statY + 12), Color.White);
            _spriteBatch.DrawString(_font, $"DEF: {species.BaseDefense}", new Vector2(x + 10, statY + 24), Color.White);
            _spriteBatch.DrawString(_font, $"SPD: {species.BaseSpeed}", new Vector2(x + 10, statY + 36), Color.White);
        }

        _spriteBatch.End();
    }

    private void DrawPartyScreen()
    {
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // Draw background
        DrawBorderedRect(_spriteBatch, new Rectangle(10, 10, 300, 160), new Color(15, 15, 15, 245), Color.DarkGray, 2);

        // Draw title
        var titleText = "PLAYER PARTY";
        var titleSize = _font.MeasureString(titleText);
        _spriteBatch.DrawString(_font, titleText, new Vector2((320 - titleSize.X) / 2f, 16), Color.Gold);

        if (_profile.Party.Count == 0)
        {
            var noJoyText = "No JoyMon in party!";
            var noJoySize = _font.MeasureString(noJoyText);
            _spriteBatch.DrawString(_font, noJoyText, new Vector2((320 - noJoySize.X) / 2f, 80), Color.Gray);
        }
        else
        {
            // Draw party members in a list format
            for (int i = 0; i < _profile.Party.Count; i++)
            {
                var member = _profile.Party[i];
                int x = 20;
                int y = 46 + i * 20;

                _spriteBatch.DrawString(_font, $"{member.Species.Name}  Lv.{member.Level}", new Vector2(x, y), Color.White);
                
                string hpText = $"HP: {member.CurrentHp}/{member.MaxHp}";
                _spriteBatch.DrawString(_font, hpText, new Vector2(x + 100, y), Color.LightGreen);

                string statsText = $"ATK:{member.Attack} DEF:{member.Defense} SPD:{member.Speed}";
                _spriteBatch.DrawString(_font, statsText, new Vector2(x + 180, y), Color.LightGray);
            }
        }

        if (!string.IsNullOrWhiteSpace(_saveStatusMessage))
        {
            var statusSize = _font.MeasureString(_saveStatusMessage);
            _spriteBatch.DrawString(_font, _saveStatusMessage, new Vector2((320 - statusSize.X) / 2f, 132), Color.LightGreen);
        }

        // Draw return prompt
        var promptText = "Enter: Save   ESC: Return";
        var promptSize = _font.MeasureString(promptText);
        _spriteBatch.DrawString(_font, promptText, new Vector2((320 - promptSize.X) / 2f, 146), Color.Yellow);

        _spriteBatch.End();
    }

    private void DrawInventoryScreen()
    {
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        DrawBorderedRect(_spriteBatch, new Rectangle(10, 10, 300, 160), new Color(15, 15, 15, 245), Color.DarkGray, 2);

        var titleText = "INVENTORY";
        var titleSize = _font.MeasureString(titleText);
        _spriteBatch.DrawString(_font, titleText, new Vector2((320 - titleSize.X) / 2f, 16), Color.Gold);

        if (_profile.Items.Slots.Count == 0)
        {
            var emptyText = "No items carried!";
            var emptySize = _font.MeasureString(emptyText);
            _spriteBatch.DrawString(_font, emptyText, new Vector2((320 - emptySize.X) / 2f, 80), Color.Gray);
        }
        else
        {
            for (int i = 0; i < _profile.Items.Slots.Count; i++)
            {
                var slot = _profile.Items.Slots[i];
                if (!ItemCatalog.TryGet(slot.ItemId, out var definition))
                    continue;

                int y = 46 + i * 20;
                _spriteBatch.DrawString(_font, definition.Name, new Vector2(20, y), Color.White);
                _spriteBatch.DrawString(_font, $"x{slot.Quantity}", new Vector2(220, y), Color.LightGreen);
            }
        }

        var promptText = "Press ESC to return  |  I to open";
        var promptSize = _font.MeasureString(promptText);
        _spriteBatch.DrawString(_font, promptText, new Vector2((320 - promptSize.X) / 2f, 146), Color.Yellow);

        _spriteBatch.End();
    }

    private void DrawEndingScreen()
    {
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        DrawBorderedRect(_spriteBatch, new Rectangle(8, 8, 304, 164), new Color(12, 18, 28, 250), Color.Gold, 2);

        var title = "JoyMon v0.1 complete.";
        var titleSize = _font.MeasureString(title);
        _spriteBatch.DrawString(_font, title, new Vector2((320 - titleSize.X) / 2f, 14), Color.Gold);

        var data = _endingData;
        int y = 36;

        if (data is not null)
        {
            var playTime = FormatPlayTime(data.PlayTimeSeconds);
            _spriteBatch.DrawString(_font, $"Play time: {playTime}", new Vector2(16, y), Color.LightGray);
            y += 16;

            _spriteBatch.DrawString(_font, "Party:", new Vector2(16, y), Color.White);
            y += 14;
            if (data.Party.Count == 0)
            {
                _spriteBatch.DrawString(_font, "  (empty)", new Vector2(16, y), Color.Gray);
                y += 14;
            }
            else
            {
                foreach (var member in data.Party)
                {
                    var line = $"  {member.Species.Name} Lv.{member.Level}  {member.Species.TypeDisplay}  HP {member.CurrentHp}/{member.MaxHp}";
                    _spriteBatch.DrawString(_font, line, new Vector2(16, y), Color.White);
                    y += 12;
                }
            }

            y += 4;
            _spriteBatch.DrawString(_font, "Captures:", new Vector2(16, y), Color.White);
            y += 14;
            if (data.Captures.Count == 0)
            {
                _spriteBatch.DrawString(_font, "  (none)", new Vector2(16, y), Color.Gray);
            }
            else
            {
                foreach (var captureId in data.Captures)
                {
                    var displayName = captureId;
                    if (_contentDb?.Species.TryGetValue(captureId, out var species) == true)
                        displayName = species.Name;
                    _spriteBatch.DrawString(_font, $"  {displayName}", new Vector2(16, y), Color.LightGreen);
                    y += 12;
                }
            }
        }

        var prompt = "Press ENTER to return to title";
        var promptSize = _font.MeasureString(prompt);
        _spriteBatch.DrawString(_font, prompt, new Vector2((320 - promptSize.X) / 2f, 158), Color.Yellow);

        _spriteBatch.End();
    }

    private static string FormatPlayTime(double totalSeconds)
    {
        var span = TimeSpan.FromSeconds(Math.Max(0, totalSeconds));
        if (span.TotalHours >= 1)
            return $"{(int)span.TotalHours}:{span.Minutes:D2}:{span.Seconds:D2}";
        return $"{span.Minutes}:{span.Seconds:D2}";
    }
}
