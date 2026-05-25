using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace JoyMon.Game.Services;

/// <summary>
/// Concrete input reader. Tracks previous-frame state for edge detection.
/// Supports keyboard (primary) and gamepad (fallback).
/// </summary>
public sealed class InputService : IInputService
{
    private KeyboardState _prevKeyboard;
    private KeyboardState _currKeyboard;
    private GamePadState _prevGamepad;
    private GamePadState _currGamepad;

    public void Update()
    {
        _prevKeyboard = _currKeyboard;
        _currKeyboard = Keyboard.GetState();

        _prevGamepad = _currGamepad;
        _currGamepad = GamePad.GetState(PlayerIndex.One);
    }

    public bool ConfirmPressed =>
        Pressed(Keys.Enter) || Pressed(Keys.Space) || ButtonPressed(Buttons.A);

    public bool CancelPressed =>
        Pressed(Keys.Escape) || ButtonPressed(Buttons.B);

    public bool UpPressed =>
        Pressed(Keys.Up) || Pressed(Keys.W) || ButtonPressed(Buttons.DPadUp) || LeftThumbUp();

    public bool DownPressed =>
        Pressed(Keys.Down) || Pressed(Keys.S) || ButtonPressed(Buttons.DPadDown) || LeftThumbDown();

    public bool LeftPressed =>
        Pressed(Keys.Left) || Pressed(Keys.A) || ButtonPressed(Buttons.DPadLeft) || LeftThumbLeft();

    public bool RightPressed =>
        Pressed(Keys.Right) || Pressed(Keys.D) || ButtonPressed(Buttons.DPadRight) || LeftThumbRight();

    public bool UpHeld =>
        Held(Keys.Up) || Held(Keys.W) || ButtonHeld(Buttons.DPadUp) || LeftThumbUpHeld();

    public bool DownHeld =>
        Held(Keys.Down) || Held(Keys.S) || ButtonHeld(Buttons.DPadDown) || LeftThumbDownHeld();

    public bool LeftHeld =>
        Held(Keys.Left) || Held(Keys.A) || ButtonHeld(Buttons.DPadLeft) || LeftThumbLeftHeld();

    public bool RightHeld =>
        Held(Keys.Right) || Held(Keys.D) || ButtonHeld(Buttons.DPadRight) || LeftThumbRightHeld();

    public bool DebugTogglePressed =>
        Pressed(Keys.F3) || (ButtonPressed(Buttons.Y) && ButtonHeld(Buttons.Back));

    public bool StartPressed =>
        Pressed(Keys.Enter) || Pressed(Keys.Space) || ButtonPressed(Buttons.Start);

    // ── Helpers ──

    private bool Pressed(Keys key) =>
        _currKeyboard.IsKeyDown(key) && _prevKeyboard.IsKeyUp(key);

    private bool Held(Keys key) =>
        _currKeyboard.IsKeyDown(key);

    private bool ButtonPressed(Buttons button) =>
        _currGamepad.IsButtonDown(button) && _prevGamepad.IsButtonUp(button);

    private bool ButtonHeld(Buttons button) =>
        _currGamepad.IsButtonDown(button);

    private bool LeftThumbUp() =>
        _currGamepad.ThumbSticks.Left.Y > 0.5f && _prevGamepad.ThumbSticks.Left.Y <= 0.5f;

    private bool LeftThumbDown() =>
        _currGamepad.ThumbSticks.Left.Y < -0.5f && _prevGamepad.ThumbSticks.Left.Y >= -0.5f;

    private bool LeftThumbLeft() =>
        _currGamepad.ThumbSticks.Left.X < -0.5f && _prevGamepad.ThumbSticks.Left.X >= -0.5f;

    private bool LeftThumbRight() =>
        _currGamepad.ThumbSticks.Left.X > 0.5f && _prevGamepad.ThumbSticks.Left.X <= 0.5f;

    private bool LeftThumbUpHeld() =>
        _currGamepad.ThumbSticks.Left.Y > 0.5f;

    private bool LeftThumbDownHeld() =>
        _currGamepad.ThumbSticks.Left.Y < -0.5f;

    private bool LeftThumbLeftHeld() =>
        _currGamepad.ThumbSticks.Left.X < -0.5f;

    private bool LeftThumbRightHeld() =>
        _currGamepad.ThumbSticks.Left.X > 0.5f;
}