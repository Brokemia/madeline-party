using Celeste;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;

namespace MadelineParty {
    public class SubHUDSprite : Entity {
        private static DynData<SpriteBatch> spriteBatchData;

        private Level level;

        public Sprite sprite;
        private bool cleanSampling;
        private bool respectScreenShake;
        public SubHUDSprite(Sprite sprite, bool cleanSampling = true, bool respectScreenShake = true) {
            if (spriteBatchData == null) {
                spriteBatchData = new(Draw.SpriteBatch);
            }
            this.sprite = sprite;
            this.cleanSampling = cleanSampling;
            this.respectScreenShake = respectScreenShake;
            Add(sprite);
            AddTag(TagsExt.SubHUD);
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            level = SceneAs<Level>();
        }

        public override void Render() {
            SamplerState before = null;
            Matrix beforeMatrix = default;
            if (cleanSampling || respectScreenShake) {
                Draw.SpriteBatch.End();
                before = spriteBatchData.Get<SamplerState>("samplerState");
                beforeMatrix = spriteBatchData.Get<Matrix>("transformMatrix");
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred,
                    spriteBatchData.Get<BlendState>("blendState"),
                    cleanSampling ? SamplerState.PointClamp : before,
                    spriteBatchData.Get<DepthStencilState>("depthStencilState"),
                    spriteBatchData.Get<RasterizerState>("rasterizerState"),
                    spriteBatchData.Get<Effect>("customEffect"),
                    beforeMatrix * (respectScreenShake ? Matrix.CreateTranslation(new Vector3(-level.ShakeVector.X, -level.ShakeVector.Y, 0) * 6) : Matrix.Identity));
            }
            base.Render();
            if (cleanSampling) {
                Draw.SpriteBatch.End();
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred,
                    spriteBatchData.Get<BlendState>("blendState"),
                    before,
                    spriteBatchData.Get<DepthStencilState>("depthStencilState"),
                    spriteBatchData.Get<RasterizerState>("rasterizerState"),
                    spriteBatchData.Get<Effect>("customEffect"),
                    beforeMatrix);
            }
        }
    }
}
