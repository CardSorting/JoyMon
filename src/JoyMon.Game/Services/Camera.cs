namespace JoyMon.Game.Services;

/// <summary>
/// Simple 2D camera. Currently position-locked at (0, 0) — the map
/// renders from the top-left corner. Will gain scroll/follow logic later.
/// </summary>
public sealed class Camera
{
    public int X { get; set; }
    public int Y { get; set; }

    /// <summary>Locked to (0, 0) for now.</summary>
    public void Reset()
    {
        X = 0;
        Y = 0;
    }

    /// <summary>
    /// Center the camera on a target position (in pixels) and clamp to map bounds.
    /// </summary>
    public void CenterOn(float targetX, float targetY, int viewW, int viewH, int mapPixelW, int mapPixelH)
    {
        // Center the camera on the target (offset by half viewport, plus half player size which is 8)
        float camX = targetX - (viewW / 2f) + 8f;
        float camY = targetY - (viewH / 2f) + 8f;

        // Clamp camera to map bounds
        int maxCamX = Math.Max(0, mapPixelW - viewW);
        int maxCamY = Math.Max(0, mapPixelH - viewH);

        X = (int)Math.Clamp(camX, 0, maxCamX);
        Y = (int)Math.Clamp(camY, 0, maxCamY);
    }
}