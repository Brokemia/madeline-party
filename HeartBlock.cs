using Celeste;
using Celeste.Mod.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;
using System;

namespace MadelineParty {
    public class HeartBlock : Entity {
        private const int SCALE = 2;

        private MTexture[,] nineSlice;

        private float startY;

        private float yLerp;

        private float renderLerp;

        private float width, height;

        public HeartBlock(Vector2 position, float width, float height) : base(position) {
            this.width = width;
            this.height = height;
            startY = Y;
            MTexture mTexture = GFX.Game["objects/madelineparty/heartblock"];
            nineSlice = new MTexture[3, 3];
            for (int i = 0; i < 3; i++) {
                for (int j = 0; j < 3; j++) {
                    nineSlice[i, j] = mTexture.GetSubtexture(new Rectangle(i * 8, j * 8, 8, 8));
                }
            }
            Depth = -10000;
            AddTag(TagsExt.SubHUD);
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            renderLerp = 1f;
        }

        private void DrawBlock(Vector2 offset, Color color) {
            float tileWidth = width / 8f - 1f;
            float tileHeight = height / 8f - 1f;
            for (int i = 0; i <= tileWidth; i++) {
                for (int j = 0; j <= tileHeight; j++) {
                    int num3 = ((i < tileWidth) ? Math.Min(i, 1) : 2);
                    int num4 = ((j < tileHeight) ? Math.Min(j, 1) : 2);
                    nineSlice[num3, num4].Draw(Position + offset * SCALE + new Vector2(i * 8, j * 8) * SCALE, Vector2.Zero, color, SCALE);
                }
            }
        }

        public override void Render() {
            SubHudRenderer.EndRender();
            SubHudRenderer.BeginRender(null, SamplerState.PointClamp);
            Vector2 oldPos = Position;
            var level = SceneAs<Level>();
            Position += new Vector2(0f, (level.Bounds.Bottom - startY + 32f) * Ease.CubeIn(renderLerp)) - level.ShakeVector * 6;
            DrawBlock(new Vector2(-1f, 0f), Color.Black);
            DrawBlock(new Vector2(1f, 0f), Color.Black);
            DrawBlock(new Vector2(0f, -1f), Color.Black);
            DrawBlock(new Vector2(0f, 1f), Color.Black);
            DrawBlock(Vector2.Zero, Color.White);
            Position = oldPos;
            SubHudRenderer.EndRender();
            SubHudRenderer.BeginRender();
        }

        public override void Update() {
            base.Update();
            if (Visible) {
                renderLerp = Calc.Approach(renderLerp, 0f, Engine.DeltaTime * 3f);
            }
            yLerp = Calc.Approach(yLerp, 0f, 1f * Engine.DeltaTime);
            Y = MathHelper.Lerp(startY, startY + 12f, Ease.SineInOut(yLerp));
        }
    }
}
