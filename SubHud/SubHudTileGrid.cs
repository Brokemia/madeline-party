using Celeste;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;

namespace MadelineParty.SubHud {
    public class SubHudTileGrid : Component {
        public Vector2 Position;

        public Color Color = Color.White;

        public int VisualExtend;

        public VirtualMap<MTexture> Tiles;

        public float Alpha = 1f;

        public int TileWidth { get; private set; }

        public int TileHeight { get; private set; }

        public int TilesX => Tiles.Columns;

        public int TilesY => Tiles.Rows;

        public float Scale { get; set; }

        public SubHudTileGrid(int tileWidth, int tileHeight, int tilesX, int tilesY, float scale)
            : base(active: false, visible: true) {
            TileWidth = tileWidth;
            TileHeight = tileHeight;
            Tiles = new VirtualMap<MTexture>(tilesX, tilesY);
            Scale = scale;
        }

        public void Clear() {
            for (int i = 0; i < TilesX; i++) {
                for (int j = 0; j < TilesY; j++) {
                    Tiles[i, j] = null;
                }
            }
        }

        public Rectangle GetClippedRenderTiles() {
            int val = Math.Max(-VisualExtend, 0);
            int val2 = Math.Max(-VisualExtend, 0);
            int val3 = Math.Min(TilesX + VisualExtend, TilesX);
            int val4 = Math.Min(TilesY + VisualExtend, TilesY);
            return new Rectangle(val, val2, val3 - val, val4 - val2);
        }

        public override void Render() {
            RenderAt(Position * Scale + (Entity.Position - SceneAs<Level>().Camera.Position) * 6);
        }

        public void RenderAt(Vector2 position) {
            if (Alpha <= 0f) {
                return;
            }
            Rectangle clippedRenderTiles = GetClippedRenderTiles();
            int tileWidth = TileWidth;
            int tileHeight = TileHeight;
            Color color = Color * Alpha;
            Vector2 position2 = new Vector2(position.X + clippedRenderTiles.Left * tileWidth, position.Y + clippedRenderTiles.Top * tileHeight);
            for (int i = clippedRenderTiles.Left; i < clippedRenderTiles.Right; i++) {
                for (int j = clippedRenderTiles.Top; j < clippedRenderTiles.Bottom; j++) {
                    MTexture mTexture = Tiles[i, j];
                    if (mTexture != null) {
                        Draw.SpriteBatch.Draw(mTexture.Texture.Texture_Safe, position2, mTexture.ClipRect, color, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
                    }
                    position2.Y += tileHeight * Scale;
                }
                position2.X += tileWidth * Scale;
                position2.Y = position.Y + clippedRenderTiles.Top * tileHeight;
            }
        }
    }
}
