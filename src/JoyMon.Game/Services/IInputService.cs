namespace JoyMon.Game.Services;

/// <summary>
/// Abstraction over keyboard/gamepad input for the game loop.
/// All values report edge-triggered "just pressed this frame" unless noted.
/// </summary>
public interface IInputService
{
    void Update();

    /// <summary>Enter, Space, or GamePad A.</summary>
    bool ConfirmPressed { get; }

    /// <summary>Escape or GamePad B.</summary>
    bool CancelPressed { get; }

    bool UpPressed { get; }
    bool DownPressed { get; }
    bool LeftPressed { get; }
    bool RightPressed { get; }

    bool UpHeld { get; }
    bool DownHeld { get; }
    bool LeftHeld { get; }
    bool RightHeld { get; }

    /// <summary>F3 or GamePad Y + Back.</summary>
    bool DebugTogglePressed { get; }

    /// <summary>Any Start-like key (Enter, Space, GamePad Start) for boot/title transitions.</summary>
    bool StartPressed { get; }
}