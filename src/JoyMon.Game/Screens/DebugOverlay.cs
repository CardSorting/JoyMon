using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace JoyMon.Game.Screens;

/// <summary>
/// Debug overlay showing FPS, current game state, and rendering info.
/// Toggle visibility with F3.
/// </summary>
public sealed class DebugOverlay
{
    private SpriteFont _font = null!;
    private bool _visible;
    private int _fps;
    private int _frameCount;
    private float _fpsTimer;

    public bool Visible => _visible;
    public void Toggle() => _visible = !_visible;

    public void LoadContent(ContentManager content)
    {
        _font = content.Load<SpriteFont>("JoyMonFont");
    }

    public void Update(GameTime gameTime)
    {
        _frameCount++;
        _fpsTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_fpsTimer >= 1.0f)
        {
            _fps = _frameCount;
            _frameCount = 0;
            _fpsTimer -= 1.0f;
        }
    }

    public void Draw(
        SpriteBatch spriteBatch,
        GameState currentState,
        int renderScale,
        string mapId,
        int playerX,
        int playerY,
        string facing,
        bool isSliding = false,
        string currentMovementEffect = "normal")
    {
        if (!_visible)
            return;

        var y = 4f;
        DrawLine(spriteBatch, $"FPS:   {_fps}", ref y, Color.Yellow);
        DrawLine(spriteBatch, $"State: {currentState}", ref y, Color.Yellow);
        DrawLine(spriteBatch, $"Scale: {renderScale}x (320x180 -> {320 * renderScale}x{180 * renderScale})", ref y, Color.Yellow);

        if (currentState == GameState.Overworld)
        {
            DrawLine(spriteBatch, $"Map:   {mapId}", ref y, Color.Yellow);
            DrawLine(spriteBatch, $"Tile:  ({playerX},{playerY})", ref y, Color.Yellow);
            DrawLine(spriteBatch, $"Face:  {facing}", ref y, Color.Yellow);
            DrawLine(spriteBatch, $"IsSliding: {isSliding}", ref y, Color.Yellow);
            DrawLine(spriteBatch, $"CurrentMovementEffect: {currentMovementEffect}", ref y, Color.Yellow);
        }
    }

    private void DrawLine(SpriteBatch batch, string text, ref float y, Color color)
    {
        batch.DrawString(_font, text, new Vector2(4, y), color);
        y += _font.LineSpacing + 2;
    }
}