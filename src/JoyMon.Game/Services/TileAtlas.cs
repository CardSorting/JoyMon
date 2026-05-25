using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace JoyMon.Game.Services;

/// <summary>
/// Generates a programmatic tileset texture and manages tile-to-texture mapping.
/// Each tile is <see cref="TileSize"/>×<see cref="TileSize"/> pixels.
/// Tile ID 0 is transparent and is never drawn.
/// </summary>
public sealed class TileAtlas
{
    public const int TileSize = 16;

    /// <summary>Maps tile ID → solid color (0 = transparent).</summary>
    private static readonly Color[] TileColors =
    {
        Color.Transparent,              // 0: empty
        new Color(0x4C, 0xAF, 0x50),    // 1: grass
        new Color(0xD2, 0xB4, 0x8C),    // 2: path
        new Color(0x55, 0x55, 0x55),    // 3: wall / stone
        new Color(0x21, 0x96, 0xF3),    // 4: water
        new Color(0xB0, 0xB0, 0xB0),    // 5: floor
        new Color(0x2E, 0x7D, 0x32),    // 6: tree
        new Color(0xFF, 0xC1, 0x07),    // 7: sign
        new Color(0x8D, 0x6E, 0x63),    // 8: roof / building
    };

    /// <summary>Number of tile columns in the generated texture.</summary>
    public const int TilesPerRow = 4;

    private Texture2D _texture = null!;

    /// <summary>The generated tileset texture.</summary>
    public Texture2D Texture => _texture;

    /// <summary>
    /// Creates a 4×n tileset texture where each tile is filled with its <see cref="TileColors"/> color.
    /// For tile 0 the region is left transparent; for tile > 0 a 1-pixel border is added for visual
    /// separation and a simple detail pixel is placed in the center.
    /// </summary>
    public void Generate(GraphicsDevice device)
    {
        int atlasCols = TilesPerRow;
        int atlasRows = (TileColors.Length + atlasCols - 1) / atlasCols;
        int pixelW = atlasCols * TileSize;
        int pixelH = atlasRows * TileSize;

        _texture = new Texture2D(device, pixelW, pixelH);
        Color[] pixels = new Color[pixelW * pixelH];

        for (int i = 0; i < TileColors.Length; i++)
        {
            int tileX = (i % atlasCols) * TileSize;
            int tileY = (i / atlasCols) * TileSize;
            FillTile(pixels, pixelW, tileX, tileY, i);
        }

        _texture.SetData(pixels);
    }

    private static void FillTile(Color[] pixels, int stride, int originX, int originY, int tileId)
    {
        Color baseColor = TileColors[tileId];

        for (int y = 0; y < TileSize; y++)
        {
            for (int x = 0; x < TileSize; x++)
            {
                int px = originX + x;
                int py = originY + y;

                if (tileId == 0)
                {
                    pixels[py * stride + px] = Color.Transparent;
                    continue;
                }

                Color color = baseColor;

                // 1-pixel darker border
                if (x == 0 || y == 0 || x == TileSize - 1 || y == TileSize - 1)
                    color = Darken(baseColor, 0.6f);

                pixels[py * stride + px] = color;
            }
        }
    }

    private static Color Darken(Color c, float factor) =>
        new((int)(c.R * factor), (int)(c.G * factor), (int)(c.B * factor), c.A);

    /// <summary>
    /// Returns the source rectangle in the atlas for the given tile ID.
    /// Tile 0 returns an empty rectangle (transparent).
    /// </summary>
    public Rectangle GetSourceRect(int tileId)
    {
        if (tileId <= 0) return Rectangle.Empty;

        int atlasCols = TilesPerRow;
        int idx = Math.Clamp(tileId, 0, TileColors.Length - 1);
        return new Rectangle(
            (idx % atlasCols) * TileSize,
            (idx / atlasCols) * TileSize,
            TileSize,
            TileSize);
    }
}