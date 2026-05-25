namespace JoyMon.Game;

/// <summary>
/// Top-level game state routing the update/draw loop.
/// </summary>
public enum GameState
{
    /// <summary>Fade-in / logo splash (transitions to Title).</summary>
    Boot,
    /// <summary>Title screen with JOYMON branding.</summary>
    Title,
    /// <summary>Overworld exploration (placeholder).</summary>
    Overworld,
    /// <summary>Battle screen (placeholder).</summary>
    Battle,
    /// <summary>Choice UI for starter JoyMon selection.</summary>
    StarterChoice,
    /// <summary>Party screen displaying current party member statistics.</summary>
    PartyScreen,
}