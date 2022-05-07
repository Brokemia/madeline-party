using Celeste;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;

namespace MadelineParty {
    public class SubHUDSprite : Entity {
        private static DynData<SpriteBatch> spriteBatchData;

        public Sprite sprite;
        private bool cleanSampling;
        public SubHUDSprite(Sprite sprite, bool cleanSampling = true) {
            if (spriteBatchData == null) {
                spriteBatchData = new DynData<SpriteBatch>(Draw.SpriteBatch);
            }
            this.sprite = sprite;
            this.cleanSampling = cleanSampling;
            Add(sprite);
            AddTag(TagsExt.SubHUD);
        }

        public override void Render() {
            SamplerState before = null;
            if (cleanSampling) {
                Draw.SpriteBatch.End();
                before = spriteBatchData.Get<SamplerState>("samplerState");
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred,
                    spriteBatchData.Get<BlendState>("blendState"),
                    SamplerState.PointClamp,
                    spriteBatchData.Get<DepthStencilState>("depthStencilState"),
                    spriteBatchData.Get<RasterizerState>("rasterizerState"),
                    spriteBatchData.Get<Effect>("customEffect"),
                    spriteBatchData.Get<Matrix>("transformMatrix"));
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
                    spriteBatchData.Get<Matrix>("transformMatrix"));
            }
        }
    }
}
