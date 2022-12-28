using Celeste;
using Celeste.Mod.UI;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MadelineParty.SubHud {
    public interface SubHudPixelPerfectRendered {
        int RenderDepth { get; }
        void SubHudRender();
    }

    [Tracked]
    public class SubHudPixelPerfectRenderer : Entity {
        public static void Add(Scene scene, SubHudPixelPerfectRendered entity) {
            var sceneData = DynamicData.For(scene);
            if (sceneData.Get<SubHudPixelPerfectRenderer>("madelinePartySubHudPixelPerfectRenderer") is not { } renderer) {
                scene.Add(renderer = new SubHudPixelPerfectRenderer());
                sceneData.Set("madelinePartySubHudPixelPerfectRenderer", renderer);
            }
            renderer.Add(entity);
        }

        public static void Remove(Scene scene, SubHudPixelPerfectRendered entity) {
            var sceneData = DynamicData.For(scene);
            if (sceneData.Get<SubHudPixelPerfectRenderer>("madelinePartySubHudPixelPerfectRenderer") is { } renderer) {
                renderer.entities.Remove(entity);
            }

        }

        private List<SubHudPixelPerfectRendered> entities = new();

        private bool unsorted = true;

        public SubHudPixelPerfectRenderer() : base() {
            Depth = -9000;
            AddTag(TagsExt.SubHUD);
        }

        public void Add(SubHudPixelPerfectRendered entity) {
            entities.Add(entity);
            unsorted = true;
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            DynamicData.For(scene).Set("madelinePartySubHudPixelPerfectRenderer", null);
        }

        public override void Render() {
            base.Render();
            if (unsorted) {
                entities.Sort((a, b) => b.RenderDepth.CompareTo(a.RenderDepth));
                unsorted = false;
            }
            SubHudRenderer.EndRender();
            SubHudRenderer.BeginRender(sampler: SamplerState.PointClamp);
            foreach (var e in entities) {
                e.SubHudRender();
            }
            SubHudRenderer.EndRender();
            SubHudRenderer.BeginRender();
        }
    }
}
