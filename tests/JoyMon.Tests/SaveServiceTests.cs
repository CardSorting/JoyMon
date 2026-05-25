using JoyMon.Content;
using JoyMon.Core;
using JoyMon.Game.Services;

namespace JoyMon.Tests;

public class SaveServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "JoyMonSaveTest_" + Guid.NewGuid());

    public SaveServiceTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best-effort cleanup */ }
    }

    [Fact]
    public void Save_SerializesToJson()
    {
        var service = CreateService();
        var profile = CreateProfile();
        var player = CreatePlayer(4, 7);

        var save = service.CreateSave(profile, player, "starter-town", new[] { "trainer-one" });
        var json = service.Serialize(save);

        Assert.Contains("\"schemaVersion\": 3", json);
        Assert.Contains("\"currentMap\": \"starter-town\"", json);
        Assert.Contains("\"inventory\": [", json);
        Assert.Contains("\"speciesId\": \"mossprout\"", json);
        Assert.Contains("\"trainer-one\"", json);
    }

    [Fact]
    public void Load_RestoresParty()
    {
        var service = CreateService();
        var savedProfile = CreateProfile();
        savedProfile.Party[0].CurrentHp = 12;
        savedProfile.Party[0].Xp = 6;
        var player = CreatePlayer(4, 7);
        service.Save(savedProfile, player, "starter-town");

        var restoredProfile = new PlayerProfile();
        var restoredPlayer = new Player();
        var save = service.LoadSave();
        service.Restore(save, restoredProfile, restoredPlayer);

        Assert.Single(restoredProfile.Party);
        Assert.Equal("Mossprout", restoredProfile.Party[0].Species.Name);
        Assert.Equal(12, restoredProfile.Party[0].CurrentHp);
        Assert.Equal(6, restoredProfile.Party[0].Xp);
    }

    [Fact]
    public void Load_RestoresInventory()
    {
        var service = CreateService();
        var profile = CreateProfile();
        profile.Items.SetQuantity(ItemCatalog.BerryTonicId, 2);
        profile.Items.SetQuantity(ItemCatalog.SyncCapsuleId, 1);
        service.Save(profile, CreatePlayer(4, 7), "starter-town");

        var restoredProfile = new PlayerProfile();
        var save = service.LoadSave();
        service.Restore(save, restoredProfile, new Player());

        Assert.Equal(2, restoredProfile.Items.GetQuantity(ItemCatalog.BerryTonicId));
        Assert.Equal(1, restoredProfile.Items.GetQuantity(ItemCatalog.SyncCapsuleId));
    }

    [Fact]
    public void Load_RestoresMapAndPosition()
    {
        var service = CreateService();
        service.Save(CreateProfile(), CreatePlayer(9, 3), "route-1");

        var restoredProfile = new PlayerProfile();
        var restoredPlayer = new Player();
        var save = service.LoadSave();
        service.Restore(save, restoredProfile, restoredPlayer);

        Assert.Equal("route-1", save.CurrentMap);
        Assert.Equal(9, restoredPlayer.X);
        Assert.Equal(3, restoredPlayer.Y);
    }

    [Fact]
    public void Load_RestoresFlags()
    {
        var service = CreateService();
        var profile = CreateProfile();
        profile.SetFlag("received_starter", true);
        profile.SetFlag("bridge_open", false);
        service.Save(profile, CreatePlayer(4, 7), "starter-town");

        var restoredProfile = new PlayerProfile();
        var save = service.LoadSave();
        service.Restore(save, restoredProfile, new Player());

        Assert.True(restoredProfile.HasFlag("received_starter"));
        Assert.True(restoredProfile.Flags.ContainsKey("bridge_open"));
        Assert.False(restoredProfile.HasFlag("bridge_open"));
    }

    [Fact]
    public void UnknownSchemaVersion_FailsWithClearError()
    {
        var service = CreateService();
        var json = """
        {
          "schemaVersion": 999,
          "profile": { "name": "Player", "syncCapsules": 5 },
          "currentMap": "starter-town",
          "playerTilePosition": { "x": 4, "y": 7 },
          "party": [],
          "inventory": [],
          "flags": {},
          "defeatedTrainers": [],
          "timestamp": "2026-05-25T00:00:00Z"
        }
        """;

        var ex = Assert.Throws<InvalidOperationException>(() => service.Deserialize(json));

        Assert.Contains("Unsupported save schemaVersion 999", ex.Message);
        Assert.Contains("Expected 3", ex.Message);
    }

    private SaveService CreateService()
    {
        return new SaveService(CreateContentDatabase(), Path.Combine(_tempDir, "save.json"));
    }

    private static PlayerProfile CreateProfile()
    {
        var profile = new PlayerProfile
        {
            Name = "Ari",
        };
        profile.Items.SetQuantity(ItemCatalog.SyncCapsuleId, 3);
        profile.Party.Add(CreateContentDatabase().Species["mossprout"].CreateInstance(5));
        return profile;
    }

    private static Player CreatePlayer(int x, int y)
    {
        var player = new Player();
        player.Initialize(x, y);
        return player;
    }

    private static ContentDatabase CreateContentDatabase()
    {
        var tackle = new MoveDefinition("tackle", "Tackle", JoyMonType.Neutral, 20, 95, 25);
        var leafTap = new MoveDefinition("leaf-tap", "Leaf Tap", JoyMonType.Moss, 30, 90, 15);
        var mossprout = new JoyMonSpecies(
            "Mossprout",
            JoyMonType.Moss,
            baseMaxHp: 48,
            baseAttack: 8,
            baseDefense: 9,
            baseSpeed: 6,
            new[] { leafTap, tackle });

        return new ContentDatabase(
            new Dictionary<string, CreatureContent>(),
            new Dictionary<string, MoveContent>(),
            new Dictionary<string, JoyMonSpecies> { ["mossprout"] = mossprout },
            new Dictionary<string, MoveDefinition>
            {
                ["tackle"] = tackle,
                ["leaf-tap"] = leafTap,
            });
    }
}
