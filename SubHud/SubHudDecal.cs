using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty.SubHud {
    [CustomEntity("madelineparty/subHudDecal")]
    public class SubHudDecal : Decal, SubHudPixelPerfectRendered {
        public int RenderDepth => Depth;

        private Vector2 subpixelOffset;

        public SubHudDecal(EntityData data, Vector2 offset) : base(data.Attr("texture"), data.Position + offset, new(data.Float("scaleX", 1), data.Float("scaleY", 1)), data.Int("depth", Depths.FGDecals), data.Float("rotation")) {
            subpixelOffset = new(data.Int("subpixelX"), data.Int("subpixelY"));
            AddTag(TagsExt.SubHUD);
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            SubHudPixelPerfectRenderer.Add(scene, this);
        }

        public void SubHudRender() {
            Vector2 position = Position;
            Position = (Position - SceneAs<Level>().Camera.Position) * 6 + subpixelOffset;
            base.Render();
            Position = position;
        }

        public override void Render() { }
    }
}
