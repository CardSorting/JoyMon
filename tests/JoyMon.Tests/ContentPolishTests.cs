using System.Text.Json;
using JoyMon.Content;

namespace JoyMon.Tests;

public class ContentPolishTests
{
    private static readonly string ContentRoot = FindContentRoot();

    [Fact]
    public void ContentValidation_PassesForAllContent()
    {
        var database = new ContentLoader(ContentRoot).Load();
        var mapsDirectory = Path.Combine(ContentRoot, "maps");
        var dialogueDirectory = Path.Combine(ContentRoot, "dialogue");
        var encountersDirectory = Path.Combine(ContentRoot, "encounters");
        var trainersDirectory = Path.Combine(ContentRoot, "trainers");
        var bossesDirectory = Path.Combine(ContentRoot, "bosses");

        var mapLoader = new MapLoader(mapsDirectory);
        foreach (var file in Directory.EnumerateFiles(mapsDirectory, "*.json"))
            mapLoader.Load(Path.GetFileName(file));

        var dialogueLoader = new DialogueLoader(dialogueDirectory);
        foreach (var file in Directory.EnumerateFiles(dialogueDirectory, "*.json"))
            dialogueLoader.Load(Path.GetFileName(file));

        var validCreatureIds = database.Species.Keys.ToHashSet();
        var encounterLoader = new EncounterLoader(encountersDirectory);
        foreach (var file in Directory.EnumerateFiles(encountersDirectory, "*.json"))
            encounterLoader.Load(Path.GetFileName(file), validCreatureIds);

        if (Directory.Exists(trainersDirectory))
        {
            var trainerLoader = new TrainerLoader(trainersDirectory);
            var validMoveIds = database.Moves.Keys.ToHashSet();
            foreach (var file in Directory.EnumerateFiles(trainersDirectory, "*.json"))
                trainerLoader.LoadAll(Path.GetFileName(file), validCreatureIds, validMoveIds);
        }

        if (Directory.Exists(bossesDirectory))
        {
            var bossLoader = new BossLoader(bossesDirectory);
            foreach (var file in Directory.EnumerateFiles(bossesDirectory, "*.json"))
                bossLoader.Load(Path.GetFileName(file), validCreatureIds);
        }
    }

    [Fact]
    public void EveryCreature_HasAtLeastOneMove()
    {
        var database = new ContentLoader(ContentRoot).Load();

        foreach (var creature in database.Creatures.Values)
        {
            Assert.NotEmpty(creature.Learnset);
            Assert.All(creature.Learnset, moveId => Assert.True(database.Moves.ContainsKey(moveId), $"{creature.Id} references missing move {moveId}."));
            Assert.NotEmpty(database.Species[creature.Id].Moves);
        }
    }

    [Fact]
    public void EveryEncounterCreature_HasValidLevelRange()
    {
        var database = new ContentLoader(ContentRoot).Load();
        var loader = new EncounterLoader(Path.Combine(ContentRoot, "encounters"));
        var validCreatureIds = database.Species.Keys.ToHashSet();

        foreach (var file in Directory.EnumerateFiles(Path.Combine(ContentRoot, "encounters"), "*.json"))
        {
            var table = loader.Load(Path.GetFileName(file), validCreatureIds);
            var levelCap = table.MapId == "abandoned-train" ? 27 : (table.MapId == "flowerline-fields" ? 24 : (table.MapId == "snowbell-shrine" ? 21 : 19));
            foreach (var entry in table.Entries)
            {
                Assert.True(entry.MinLevel > 0, $"{table.Id}:{entry.CreatureId} minLevel must be positive.");
                Assert.True(entry.MaxLevel >= entry.MinLevel, $"{table.Id}:{entry.CreatureId} maxLevel must be >= minLevel.");
                Assert.True(entry.MaxLevel <= levelCap, $"{table.Id}:{entry.CreatureId} is above the current region balance cap.");
            }
        }
    }

    [Fact]
    public void EveryMapTransition_HasReciprocalOrIntentionalOneWayMarker()
    {
        var mapsDirectory = Path.Combine(ContentRoot, "maps");
        var loader = new MapLoader(mapsDirectory);
        var maps = Directory.EnumerateFiles(mapsDirectory, "*.json")
            .Select(file => loader.Load(Path.GetFileName(file)))
            .ToDictionary(map => map.Id);

        foreach (var map in maps.Values)
        {
            foreach (var transition in map.Transitions)
            {
                Assert.True(maps.TryGetValue(transition.ToMapId, out var targetMap), $"{map.Id} transitions to missing map {transition.ToMapId}.");

                var hasReciprocal = targetMap!.Transitions.Any(candidate => candidate.ToMapId == map.Id);
                var hasOneWayMarker = TransitionHasOneWayMarker(map.Id, transition.ToMapId);

                Assert.True(
                    hasReciprocal || hasOneWayMarker,
                    $"{map.Id} -> {transition.ToMapId} needs a reciprocal transition or oneWayReason marker.");
            }
        }
    }

    [Fact]
    public void EveryDialogueReference_Resolves()
    {
        var dialogueDirectory = Path.Combine(ContentRoot, "dialogue");
        var loader = new DialogueLoader(dialogueDirectory);

        foreach (var file in Directory.EnumerateFiles(dialogueDirectory, "*.json"))
        {
            var dialogueFile = loader.Load(Path.GetFileName(file));
            var dialogueIds = dialogueFile.Dialogues.Select(dialogue => dialogue.Id).ToHashSet();

            foreach (var npc in dialogueFile.Npcs)
                Assert.Contains(npc.DialogueId, dialogueIds);
        }
    }

    [Fact]
    public void TrialGrove_UsesExpectedEncounterTable()
    {
        var database = new ContentLoader(ContentRoot).Load();
        var loader = new EncounterLoader(Path.Combine(ContentRoot, "encounters"));
        var table = loader.Load("trial-grove.json", database.Species.Keys.ToHashSet());

        Assert.Equal("trial-grove", table.MapId);
        Assert.Equal(new[] { "glimmoo", "queuebee", "rootsnail" }, table.Entries.Select(entry => entry.CreatureId).Order().ToArray());
    }

    [Fact]
    public void TrialGrove_HealingNpcIsDiscoverableFromEntrance()
    {
        var map = new MapLoader(Path.Combine(ContentRoot, "maps")).Load("trial-grove.json");
        var dialogue = new DialogueLoader(Path.Combine(ContentRoot, "dialogue")).Load("starter-town.json");
        var healer = Assert.Single(dialogue.Npcs.Where(npc => npc.Id == "trial-grove-healer"));

        Assert.Equal("trial-grove", healer.MapId);
        Assert.NotNull(map.SpawnPoint);

        var distanceFromSpawn = Math.Abs(healer.TilePosition.X - map.SpawnPoint!.X)
            + Math.Abs(healer.TilePosition.Y - map.SpawnPoint.Y);
        Assert.True(distanceFromSpawn <= 2, "Trial Grove healer should be visible near the entrance.");
    }

    private static bool TransitionHasOneWayMarker(string fromMapId, string toMapId)
    {
        var path = Path.Combine(ContentRoot, "maps", $"{fromMapId}.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));

        if (!document.RootElement.TryGetProperty("transitions", out var transitions))
            return false;

        foreach (var transition in transitions.EnumerateArray())
        {
            if (!transition.TryGetProperty("toMapId", out var toMapElement))
                continue;

            if (toMapElement.GetString() != toMapId)
                continue;

            return transition.TryGetProperty("oneWayReason", out var reason)
                && !string.IsNullOrWhiteSpace(reason.GetString());
        }

        return false;
    }

    private static string FindContentRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var contentPath = Path.Combine(directory.FullName, "content");
            if (Directory.Exists(contentPath) && File.Exists(Path.Combine(directory.FullName, "JoyMon.sln")))
                return contentPath;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository content directory.");
    }
}
