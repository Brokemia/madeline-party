using Celeste;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;

namespace MadelineParty.SubHud {
    public class SubHudSprite : Entity, SubHudPixelPerfectRendered {
        public int RenderDepth => Depth;

        private static DynData<SpriteBatch> spriteBatchData;

        private Level level;

        public Sprite sprite;
        private bool cleanSampling;
        private bool respectScreenShake;
        public SubHudSprite(Sprite sprite, bool cleanSampling = true, bool respectScreenShake = true) {
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
            SubHudPixelPerfectRenderer.Add(scene, this);
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            SubHudPixelPerfectRenderer.Remove(scene, this);
        }

        public override void Render() {}

        public void SubHudRender() {
            Vector2 pos = Position;
            if(respectScreenShake) {
                Position += SceneAs<Level>().ShakeVector;
            }
            base.Render();
            Position = pos;
        }
    }
}
