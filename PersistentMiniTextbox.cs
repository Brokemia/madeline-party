using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadelineParty {
    [Tracked(false)]
    public class PersistentMiniTextbox : MiniTextbox {
        private FancyText.Anchors anchor;
        private DynamicData selfData;
        private Level level;

        public PersistentMiniTextbox(string dialogId, FancyText.Anchors anchor = FancyText.Anchors.Top) : base(dialogId) {
            this.anchor = anchor;
            selfData = DynamicData.For(this);
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            level = SceneAs<Level>();
        }

        public override void Render() {
            float ease = selfData.Get<float>("ease");
            if (ease <= 0f) {
                return;
            }
            Level level = Scene as Level;
            if (!level.FrozenOrPaused && level.RetryPlayerCorpse == null && !level.SkippingCutscene) {
                MTexture box = selfData.Get<MTexture>("box");
                Sprite portrait = selfData.Get<Sprite>("portrait");
                FancyText.Text text = selfData.Get<FancyText.Text>("text");
                float portraitScale = selfData.Get<float>("portraitScale");
                float portraitSize = selfData.Get<float>("portraitSize");
                int index = selfData.Get<int>("index");

                Vector2 vector = new Vector2(Engine.Width / 2, 72f + (Engine.Width - 1688f) / 4f);
                if(anchor == FancyText.Anchors.Bottom) {
                    vector.Y = Engine.Height - vector.Y;
                } else if(anchor == FancyText.Anchors.Middle) {
                    vector.Y = Engine.Height / 2;
                }
                Vector2 value = vector + new Vector2(-828f, -56f);

                box.DrawCentered(vector, Color.White, new Vector2(1f, ease));
                if (portrait != null) {
                    portrait.Scale = new Vector2(1f, ease) * portraitScale;
                    portrait.RenderPosition = value + new Vector2(portraitSize / 2f, portraitSize / 2f);
                    portrait.Render();
                }
                text.Draw(new Vector2(value.X + portraitSize + 32f, vector.Y), new Vector2(0f, 0.5f), new Vector2(1f, ease) * 0.75f, 1f, 0, index);
            }
        }

        public static void Load() {
            On.Celeste.MiniTextbox.Routine += MiniTextbox_Routine;
        }

        public static void Unload() {
            On.Celeste.MiniTextbox.Routine -= MiniTextbox_Routine;
        }

        private static IEnumerator MiniTextbox_Routine(On.Celeste.MiniTextbox.orig_Routine orig, MiniTextbox self) {
            if (self is PersistentMiniTextbox) {
                IEnumerator res = orig(self);
                while (res.MoveNext()) {
                    if (res.Current is float f && f == 3f) {
                        yield break;
                    }
                    yield return res.Current;
                }
            } else {
                yield return new SwapImmediately(orig(self));
            }
        }
    }
}
