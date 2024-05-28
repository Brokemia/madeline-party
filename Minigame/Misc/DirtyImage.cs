using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MadelineParty.Minigame.Misc {
    [Tracked]
    [CustomEntity("madelineparty/dirtyImage")]
    public class DirtyImage : Entity {
        public bool CanClean { get; set; } = true;
        public bool IsContentLost => dirt.Target.IsContentLost;

        private MTexture baseImage, dirtImage, brush;
        private VirtualRenderTarget dirt;
        private bool firstRender = true;
        private List<Vector2> allPoints = new();
        private List<Vector2> cleanPointQueue = new();
        private BlendState customBlendState;
        private Point dirtOffset;
        public int PlayerID { get; private set; }

        public event Action<Vector2> OnAddPoint;
        private int imageWidth;
        private int imageHeight;

        public DirtyImage(EntityData data, Vector2 offset) : base(data.Position + offset) {
            PlayerID = data.Int("playerID", -1);
            baseImage = GFX.Game[data.Attr("baseImage", "madelineparty/minigame/cleanedImage")];
            dirtImage = GFX.Game[data.Attr("dirt", "madelineparty/minigame/dirt")];
            brush = GFX.Game[data.Attr("brush", "madelineparty/minigame/cleaningBrush")];
            var cropWidth = data.Int("cropWidth", -1);
            var cropHeight = data.Int("cropHeight", -1);
            imageWidth = cropWidth < 0 ? baseImage.Width : cropWidth;
            imageHeight = cropHeight < 0 ? baseImage.Height : cropHeight;
            Add(new BeforeRenderHook(BeforeRender));
            customBlendState = new() {
                ColorSourceBlend = Blend.Zero,
                AlphaSourceBlend = Blend.Zero,
                ColorDestinationBlend = Blend.SourceAlpha,
                AlphaDestinationBlend = Blend.SourceAlpha
            };
        }

        public void SetImage(MTexture texture) {
            baseImage = texture;
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            dirtOffset = new(Calc.Random.Next(dirtImage.Width), Calc.Random.Next(dirtImage.Height));
        }

        public override void Update() {
            base.Update();
            if (CanClean && Scene.Tracker.GetEntity<Player>() is Player player) {
                AddPoint(player.Center);
            }
        }

        public void AddPoint(Vector2 vec) {
            allPoints.Add(vec);
            cleanPointQueue.Add(vec);
            OnAddPoint?.Invoke(vec);
        }
        
        private void BeforeRender() {
            if (firstRender || dirt.Target.IsContentLost) {
                dirt ??= VirtualContent.CreateRenderTarget("madelineparty-dirt-target", imageWidth, imageHeight);
                Engine.Graphics.GraphicsDevice.SetRenderTarget(dirt);
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.Default, RasterizerState.CullNone);
                Draw.SpriteBatch.Draw(dirtImage.Texture.Texture_Safe, new Rectangle(0, 0, imageWidth, imageHeight), new Rectangle(dirtOffset.X, dirtOffset.Y, dirtImage.Width, dirtImage.Height), Color.White);
                Draw.SpriteBatch.End();

                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, customBlendState, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone);
                foreach (Vector2 v in allPoints) {
                    brush.DrawCentered(v - Position);
                }
                cleanPointQueue.Clear();
                Draw.SpriteBatch.End();

                firstRender = false;
            } else {
                Engine.Graphics.GraphicsDevice.SetRenderTarget(dirt);
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, customBlendState, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone);
                foreach (Vector2 v in cleanPointQueue) {
                    brush.DrawCentered(v - Position);
                }
                cleanPointQueue.Clear();
                Draw.SpriteBatch.End();
            }
        }

        public override void Render() {
            base.Render();
            baseImage.Draw(Position, Vector2.Zero, Color.White, Vector2.One, 0, new Rectangle((baseImage.Width - imageWidth) / 2, (baseImage.Height - imageHeight) / 2, imageWidth, imageHeight));
            if (dirt != null) {
                Draw.SpriteBatch.Draw(dirt, Position, Color.White);
            }
        }

        public int GetErasedCount() {
            var pixels = new Color[dirt.Width * dirt.Height];
            dirt.Target.GetData(pixels);
            return pixels.Count(c => c.A <= 0.1f);
        }

        public int TotalPixels => dirt.Width * dirt.Height;
    }
}
