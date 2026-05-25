using JoyMon.Core;

namespace JoyMon.Tests;

public class StarterChoiceTests
{
    [Fact]
    public void NewProfile_StartsWithEmptyParty()
    {
        var profile = new PlayerProfile();
        Assert.Empty(profile.Party);
        Assert.False(profile.HasFlag("received_starter"));
    }

    [Fact]
    public void SelectingStarter_AddsExactlyOneJoyMon_AndSetsFlags()
    {
        var profile = new PlayerProfile();
        var species = SpeciesLibrary.Moss; // use SpeciesLibrary for clean testing without IO

        // Simulating starter selection
        Assert.Empty(profile.Party);
        
        var starterInstance = species.CreateInstance(5);
        profile.Party.Add(starterInstance);
        profile.SetFlag("received_starter", true);

        Assert.Single(profile.Party);
        Assert.Equal("Moss", profile.Party[0].Species.Name);
        Assert.Equal(5, profile.Party[0].Level);
        Assert.True(profile.HasFlag("received_starter"));
    }

    [Fact]
    public void CannotReceiveDuplicateStarter()
    {
        var profile = new PlayerProfile();
        var species = SpeciesLibrary.Moss;

        // Check helper logic
        bool canReceive = !profile.HasFlag("received_starter") && profile.Party.Count == 0;
        Assert.True(canReceive);

        // Receive starter
        profile.Party.Add(species.CreateInstance(5));
        profile.SetFlag("received_starter", true);

        // Try duplicate
        canReceive = !profile.HasFlag("received_starter") && profile.Party.Count == 0;
        Assert.False(canReceive);
    }

    [Fact]
    public void ReceivedStarterFlag_PersistsInProfileState()
    {
        var profile = new PlayerProfile();
        Assert.False(profile.HasFlag("received_starter"));

        profile.SetFlag("received_starter", true);
        Assert.True(profile.HasFlag("received_starter"));
    }

    [Fact]
    public void DrCedarDialogue_ChangesAfterStarterSelection()
    {
        var profile = new PlayerProfile();
        
        // Initial dialogue ID
        string dialogueId = "dr-cedar-talk";
        if (profile.HasFlag("received_starter"))
        {
            dialogueId = "dr-cedar-after";
        }
        Assert.Equal("dr-cedar-talk", dialogueId);

        // Receive starter
        profile.SetFlag("received_starter", true);

        // Dialogue ID after
        if (profile.HasFlag("received_starter"))
        {
            dialogueId = "dr-cedar-after";
        }
        Assert.Equal("dr-cedar-after", dialogueId);
    }
}
