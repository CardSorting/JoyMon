using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JoyMon.Game.Services;

/// <summary>
/// Manages a 320×180 virtual resolution with integer scaling and letterboxing.
/// All game rendering targets the virtual canvas; the final blit handles
/// pixel-perfect upscaling to the window back-buffer.
/// </summary>
public sealed class VirtualResolution
{
    /// <summary>Internal virtual canvas width.</summary>
    public const int VirtualWidth = 320;

    /// <summary>Internal virtual canvas height.</summary>
    public const int VirtualHeight = 180;

    private RenderTarget2D _renderTarget = null!;
    private int _scale = 1;
    private int _viewportX;
    private int _viewportY;
    private int _viewportW;
    private int _viewportH;

    /// <summary>The integer scale factor (e.g. 4 on a 1280×720 window).</summary>
    public int Scale => _scale;

    /// <summary>Viewport rectangle on the back buffer (scaled + centered).</summary>
    public Rectangle Viewport => new(_viewportX, _viewportY, _viewportW, _viewportH);

    // ── Lifecycle ───────────────────────────────────────────────

    public void Initialize(GraphicsDevice device)
    {
        _renderTarget = new RenderTarget2D(
            device,
            VirtualWidth,
            VirtualHeight,
            mipMap: false,
            preferredFormat: SurfaceFormat.Color,
            preferredDepthFormat: DepthFormat.None,
            preferredMultiSampleCount: 0,
            usage: RenderTargetUsage.PreserveContents);
    }

    /// <summary>
    /// Recalculate scale and viewport position. Call whenever the window is resized.
    /// </summary>
    public void Update(int backBufferWidth, int backBufferHeight)
    {
        _scale = Math.Max(1, Math.Min(
            backBufferWidth / VirtualWidth,
            backBufferHeight / VirtualHeight));

        _viewportW = VirtualWidth * _scale;
        _viewportH = VirtualHeight * _scale;
        _viewportX = (backBufferWidth - _viewportW) / 2;
        _viewportY = (backBufferHeight - _viewportH) / 2;
    }

    // ── Rendering ───────────────────────────────────────────────

    /// <summary>
    /// Call at the start of <c>Game.Draw()</c>: sets the render target to the
    /// virtual canvas and clears it to black.
    /// </summary>
    public void BeginDraw(GraphicsDevice device)
    {
        device.SetRenderTarget(_renderTarget);
        device.Clear(Color.Black);
    }

    /// <summary>
    /// Call at the end of <c>Game.Draw()</c>: commits the virtual canvas to the
    /// back buffer with pixel-perfect integer scaling.
    /// </summary>
    public void EndDraw(SpriteBatch batch, GraphicsDevice device)
    {
        device.SetRenderTarget(null);
        device.Clear(Color.Black);

        batch.Begin(samplerState: SamplerState.PointClamp);
        batch.Draw(_renderTarget, Viewport, Color.White);
        batch.End();
    }
}