using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace JoyMon.Game.Screens;

/// <summary>
/// Title screen: shows JOYMON branding and a blinking prompt.
/// </summary>
public sealed class TitleScreen
{
    private const int VirtualWidth = 320;

    private SpriteFont _font = null!;
    private float _blinkTimer;
    private bool _showPrompt = true;

    public void LoadContent(ContentManager content)
    {
        _font = content.Load<SpriteFont>("JoyMonFont");
    }

    public void Update(GameTime gameTime)
    {
        _blinkTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_blinkTimer >= 0.5f)
        {
            _showPrompt = !_showPrompt;
            _blinkTimer -= 0.5f;
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // ── Title ──
        var title = "JOYMON";
        var titleSize = _font.MeasureString(title);
        float titleX = (VirtualWidth - titleSize.X) / 2f;
        spriteBatch.DrawString(_font, title, new Vector2(titleX, 40), Color.White);

        // ── Subtitle ──
        var sub = "A Joy-Monitoring Adventure";
        var subSize = _font.MeasureString(sub);
        float subX = (VirtualWidth - subSize.X) / 2f;
        spriteBatch.DrawString(_font, sub, new Vector2(subX, 68), Color.LightGray);

        // ── Blinking prompt ──
        if (_showPrompt)
        {
            var prompt = "Press Enter / Start";
            var promptSize = _font.MeasureString(prompt);
            float promptX = (VirtualWidth - promptSize.X) / 2f;
            spriteBatch.DrawString(_font, prompt, new Vector2(promptX, 130), Color.White);
        }
    }
}