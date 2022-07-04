﻿using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace MadelineParty {
    [Tracked(true)]
    public class TextMenuPlus : TextMenu {

        public enum TextAlignment { Left, Center, Right }

        public class BetterHeader : Item {
            public float Scale = 1;

            public TextAlignment Alignment = TextAlignment.Center;

            public string Title;

            public BetterHeader(string title) {
                Title = title;
                Selectable = false;
                IncludeWidthInMeasurement = false;
            }

            public override float LeftWidth() {
                return ActiveFont.Measure(Title).X * 2f * Scale;
            }

            public override float Height() {
                return ActiveFont.LineHeight * 2f * Scale;
            }

            public override void Render(Vector2 position, bool highlighted) {
                float alpha = Container.Alpha;
                Color strokeColor = Color.Black * (alpha * alpha * alpha);
                Vector2 pos = position + Alignment switch {
                    TextAlignment.Center => new Vector2(Container.Width * 0.5f, 0f),
                    TextAlignment.Right => new Vector2(Container.Width, 0f),
                    _ => Vector2.Zero
                };
                Vector2 justification = Alignment switch {
                    TextAlignment.Left => new Vector2(0, 0.5f),
                    TextAlignment.Center => new Vector2(0.5f, 0.5f),
                    _ => new Vector2(1, 0.5f)
                };
                ActiveFont.DrawEdgeOutline(Title, pos, justification, Vector2.One * 2f * Scale, Color.Gray * alpha, 4f, Color.DarkSlateBlue * alpha, 2f, strokeColor);
            }
        }

        public bool DoCrop = false;

        public Rectangle Crop;

        private VirtualRenderTarget renderTarget;

        public override void Awake(Scene scene) {
            base.Awake(scene);
            renderTarget = VirtualContent.CreateRenderTarget("madelineparty-better-textmenu", Crop.Width, Crop.Height);
        }

        public static void Load() {
            On.Celeste.Mod.UI.SubHudRenderer.BeforeRender += SubHudRenderer_BeforeRender;
        }

        private static void SubHudRenderer_BeforeRender(On.Celeste.Mod.UI.SubHudRenderer.orig_BeforeRender orig, Celeste.Mod.UI.SubHudRenderer self, Scene scene) {
            foreach(TextMenuPlus menu in scene.Tracker.GetEntities<TextMenuPlus>()) {
                if (menu.DoCrop) {
                    Engine.Graphics.GraphicsDevice.SetRenderTarget(menu.renderTarget);
                    Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);
                    Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, Matrix.CreateTranslation(new Vector3(-menu.Crop.X, -menu.Crop.Y, 0)));
                    menu.RenderContent();
                    Draw.SpriteBatch.End();
                }
            }
            orig(self, scene);
        }

        private void RenderContent() {
            base.Render();
        }

        public override void Render() {
            if (DoCrop) {
                Draw.SpriteBatch.Draw(renderTarget, Crop.Location.ToVector2(), Color.White);
            } else {
                RenderContent();
            }
        }

    }
}
