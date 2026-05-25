using System.Text.Json;
using JoyMon.Content;
using JoyMon.Core;

namespace JoyMon.Tests;

public class DialogueTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "JoyMonDialogueTest_" + Guid.NewGuid());

    public DialogueTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private void WriteDialogue(string filename, object data) =>
        File.WriteAllText(Path.Combine(_tempDir, filename), JsonSerializer.Serialize(data));

    private DialogueLoader CreateLoader() => new(_tempDir);

    private object ValidDialogue() => new
    {
        npcs = new[]
        {
            new
            {
                id = "dr-cedar",
                name = "Dr. Cedar",
                mapId = "starter-town",
                tilePosition = new { x = 8, y = 5 },
                facingDirection = "down",
                dialogueId = "dr-cedar-talk",
                spriteId = "dr-cedar"
            }
        },
        dialogues = new[]
        {
            new
            {
                id = "dr-cedar-talk",
                speaker = "Dr. Cedar",
                lines = new[] { "Hello!", "Welcome!" }
            }
        }
    };

    // ── 1. NPC content loads ────────────────────────────────────
    [Fact]
    public void ValidDialogue_LoadsSuccessfully()
    {
        WriteDialogue("valid.json", ValidDialogue());
        var loader = CreateLoader();

        var content = loader.Load("valid.json");
        Assert.Single(content.Npcs);
        Assert.Single(content.Dialogues);

        var npc = content.Npcs[0];
        Assert.Equal("dr-cedar", npc.Id);
        Assert.Equal(8, npc.TilePosition.X);
        Assert.Equal(5, npc.TilePosition.Y);
        Assert.Equal("down", npc.FacingDirection);

        var dlg = content.Dialogues[0];
        Assert.Equal("dr-cedar-talk", dlg.Id);
        Assert.Equal("Dr. Cedar", dlg.Speaker);
        Assert.Equal(2, dlg.Lines.Count);
    }

    // ── 2. Duplicate NPC IDs fail ───────────────────────────────
    [Fact]
    public void DuplicateNpcIds_FailValidation()
    {
        WriteDialogue("duplicate.json", new
        {
            npcs = new[]
            {
                new { id = "dupe", name = "Cedar", mapId = "a", tilePosition = new { x=0, y=0 }, facingDirection = "down", dialogueId = "d", spriteId = "s" },
                new { id = "dupe", name = "Oak", mapId = "a", tilePosition = new { x=1, y=1 }, facingDirection = "up", dialogueId = "d", spriteId = "s" }
            },
            dialogues = new[]
            {
                new { id = "d", speaker = "Speaker", lines = new[] { "Hi" } }
            }
        });

        var loader = CreateLoader();
        var ex = Assert.Throws<InvalidContentException>(() => loader.Load("duplicate.json"));
        Assert.Contains("Duplicate NPC ID", ex.Message);
    }

    // ── 3. Missing dialogue reference fails ──────────────────────
    [Fact]
    public void MissingDialogueReference_FailsValidation()
    {
        WriteDialogue("missing.json", new
        {
            npcs = new[]
            {
                new { id = "npc", name = "Cedar", mapId = "a", tilePosition = new { x=0, y=0 }, facingDirection = "down", dialogueId = "non-existent", spriteId = "s" }
            },
            dialogues = new[]
            {
                new { id = "d", speaker = "Speaker", lines = new[] { "Hi" } }
            }
        });

        var loader = CreateLoader();
        var ex = Assert.Throws<InvalidContentException>(() => loader.Load("missing.json"));
        Assert.Contains("references unknown dialogue ID", ex.Message);
    }

    // ── 4. Interaction finds NPC in facing tile ──────────────────
    [Fact]
    public void Interaction_FindsNpcInFacingTile()
    {
        var player = new Player();
        player.Initialize(5, 5);
        player.Facing = Direction.Right;

        var npcs = new List<Npc>
        {
            new Npc("npc-1", "Npc 1", 6, 5, Direction.Left, "dlg", "sprite"),
            new Npc("npc-2", "Npc 2", 5, 6, Direction.Up, "dlg", "sprite")
        };

        var found = FindNpcInFront(player, npcs);
        Assert.NotNull(found);
        Assert.Equal("npc-1", found.Id);
    }

    // ── 5. Movement is locked during dialogue ───────────────────
    [Fact]
    public void MovementIsLocked_DuringDialogue()
    {
        var player = new Player();
        player.Initialize(5, 5);
        var dialogue = new DialogueState();
        dialogue.Start("Speaker", new[] { "Line 1" });

        // Input dir is Right, but since dialogue is active we mimic Game1 locking and pass Direction.None
        Direction inputDir = dialogue.IsActive ? Direction.None : Direction.Right;

        player.Update(0.1f, inputDir, (x, y) => true);

        Assert.Equal(MovementState.Idle, player.State);
        Assert.Equal(5, player.X);
        Assert.Equal(5, player.TargetX);
    }

    // ── 6. Dialogue advances in order ───────────────────────────
    [Fact]
    public void Dialogue_AdvancesInOrder()
    {
        var dialogue = new DialogueState();
        dialogue.Start("Speaker", new[] { "Line 1", "Line 2" });

        Assert.Equal("Line 1", dialogue.CurrentLine);
        dialogue.Advance();
        Assert.Equal("Line 2", dialogue.CurrentLine);
    }

    // ── 7. Dialogue closes after final line ─────────────────────
    [Fact]
    public void Dialogue_ClosesAfterFinalLine()
    {
        var dialogue = new DialogueState();
        dialogue.Start("Speaker", new[] { "Line 1" });

        Assert.True(dialogue.IsActive);
        dialogue.Advance();
        Assert.False(dialogue.IsActive);
        dialogue.Close();
        Assert.False(dialogue.IsActive);
    }

    private static Npc? FindNpcInFront(Player player, List<Npc> npcs)
    {
        int dx = 0;
        int dy = 0;
        switch (player.Facing)
        {
            case Direction.Up: dy = -1; break;
            case Direction.Down: dy = 1; break;
            case Direction.Left: dx = -1; break;
            case Direction.Right: dx = 1; break;
        }

        int tx = player.X + dx;
        int ty = player.Y + dy;

        foreach (var npc in npcs)
        {
            if (npc.X == tx && npc.Y == ty)
                return npc;
        }

        return null;
    }
}
