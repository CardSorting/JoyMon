using JoyMon.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace JoyMon.Game.Services;

/// <summary>
/// Draws tilemap layers using the <see cref="TileAtlas"/>.
/// Ground layer first, then decoration on top. Collision is loaded but not drawn.
/// </summary>
public sealed class MapRenderer
{
    private TileAtlas _atlas = null!;
    private MapContent? _map;

    /// <summary>The currently loaded map, or null.</summary>
    public MapContent? CurrentMap => _map;

    /// <summary>The shared tile atlas.</summary>
    public TileAtlas Atlas => _atlas;

    public void LoadContent(GraphicsDevice device)
    {
        _atlas = new TileAtlas();
        _atlas.Generate(device);
    }

    /// <summary>
    /// Load a map into the renderer. Pass the deserialized <see cref="MapContent"/>.
    /// </summary>
    public void SetMap(MapContent map)
    {
        _map = map;
    }

    /// <summary>
    /// Draw the currently loaded map layers.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch)
    {
        if (_map is null) return;

        int tileSize = _map.TileSize;

        // Draw all tiles — camera is locked, so startX/startY = 0
        int endX = _map.Width;
        int endY = _map.Height;

        // Draw ground layer
        DrawLayer(spriteBatch, _map.Layers.Ground, 0, 0, endX, endY, tileSize);

        // Draw decoration layer on top
        DrawLayer(spriteBatch, _map.Layers.Decoration, 0, 0, endX, endY, tileSize);
    }

    private void DrawLayer(SpriteBatch batch, List<List<int>> layer, int startX, int startY, int endX, int endY, int tileSize)
    {
        if (layer is null) return;

        for (int y = startY; y < endY && y < layer.Count; y++)
        {
            var row = layer[y];
            if (row is null) continue;

            for (int x = startX; x < endX && x < row.Count; x++)
            {
                int tileId = row[x];
                if (tileId <= 0) continue;

                var source = _atlas.GetSourceRect(tileId);
                if (source == Rectangle.Empty) continue;

                batch.Draw(
                    _atlas.Texture,
                    new Vector2(x * tileSize, y * tileSize),
                    source,
                    Color.White);
            }
        }
    }
}