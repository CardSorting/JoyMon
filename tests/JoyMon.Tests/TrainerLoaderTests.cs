using System.Text.Json;
using JoyMon.Content;

namespace JoyMon.Tests;

public class TrainerLoaderTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "JoyMonTrainerTest_" + Guid.NewGuid());
    private readonly HashSet<string> _validCreatures = new() { "queuebee", "rootsnail" };
    private readonly HashSet<string> _validMoves = new() { "tackle", "spark-peck" };

    public TrainerLoaderTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private void WriteTrainer(string filename, object data) =>
        File.WriteAllText(Path.Combine(_tempDir, filename), JsonSerializer.Serialize(data));

    private TrainerLoader CreateLoader() => new(_tempDir);

    private object ValidTrainer() => new
    {
        id = "route-1-rival",
        displayName = "Kai",
        mapId = "route-1",
        tilePosition = new { x = 14, y = 7 },
        sightRange = 0,
        facingDirection = "left",
        spriteId = "rival",
        dialogueBefore = new
        {
            speaker = "Kai",
            lines = new[] { "Let's battle!" }
        },
        dialogueAfter = new
        {
            speaker = "Kai",
            lines = new[] { "Good fight!" }
        },
        party = new[]
        {
            new { creatureId = "queuebee", level = 4 }
        }
    };

    [Fact]
    public void ValidTrainer_LoadsSuccessfully()
    {
        WriteTrainer("valid.json", ValidTrainer());
        var loader = CreateLoader();

        var trainer = loader.Load("valid.json", _validCreatures, _validMoves);

        Assert.Equal("route-1-rival", trainer.Id);
        Assert.Equal("Kai", trainer.DisplayName);
        Assert.Equal("route-1", trainer.MapId);
        Assert.Equal(14, trainer.TilePosition.X);
        Assert.Equal(7, trainer.TilePosition.Y);
        Assert.Equal(0, trainer.SightRange);
        Assert.Single(trainer.Party);
        Assert.Equal("queuebee", trainer.Party[0].CreatureId);
        Assert.Equal(4, trainer.Party[0].Level);
        Assert.Equal("Kai", trainer.DialogueBefore.Speaker);
        Assert.Equal("Kai", trainer.DialogueAfter.Speaker);
    }

    [Fact]
    public void InvalidCreatureReference_FailsValidation()
    {
        WriteTrainer("invalid_creature.json", new
        {
            id = "bad-trainer",
            displayName = "Bad",
            mapId = "route-1",
            tilePosition = new { x = 1, y = 1 },
            sightRange = 0,
            dialogueBefore = new { speaker = "Bad", lines = new[] { "Hi" } },
            dialogueAfter = new { speaker = "Bad", lines = new[] { "Bye" } },
            party = new[] { new { creatureId = "unknown-mon", level = 3 } }
        });

        var loader = CreateLoader();
        var ex = Assert.Throws<InvalidContentException>(() => loader.Load("invalid_creature.json", _validCreatures, _validMoves));
        Assert.Contains("references unknown creature ID", ex.Message);
    }

    [Fact]
    public void InvalidMoveReference_FailsValidation()
    {
        WriteTrainer("invalid_move.json", new
        {
            id = "bad-trainer",
            displayName = "Bad",
            mapId = "route-1",
            tilePosition = new { x = 1, y = 1 },
            sightRange = 0,
            dialogueBefore = new { speaker = "Bad", lines = new[] { "Hi" } },
            dialogueAfter = new { speaker = "Bad", lines = new[] { "Bye" } },
            party = new[]
            {
                new { creatureId = "queuebee", level = 3, moves = new[] { "fake-move" } }
            }
        });

        var loader = CreateLoader();
        var ex = Assert.Throws<InvalidContentException>(() => loader.Load("invalid_move.json", _validCreatures, _validMoves));
        Assert.Contains("references unknown move ID", ex.Message);
    }
}
