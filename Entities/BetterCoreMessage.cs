using Celeste;
using Celeste.Mod.Entities;
using Celeste.Mod.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;

namespace MadelineParty.Entities
{
    [CustomEntity("madelineparty/betterCoreMessage")]
    public class BetterCoreMessage : Entity
    {
        public enum FadeMode { NoFade, FadeIn, FadeInAndOut }

        private string text;

        private float alpha;

        private bool outline;

        private float parallax;

        private FadeMode fade;

        private float scale;

        private bool renderBehindPlayer;


        public BetterCoreMessage(EntityData data, Vector2 offset) : base(data.Position + offset)
        {
            Tag = TagsExt.SubHUD;
            text = Dialog.Clean(data.Attr("dialog", "app_ending")).Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)[data.Int("line", 0)];
            outline = data.Bool("outline");
            parallax = data.Float("parallax", 0.2f);
            fade = data.Enum("fade", FadeMode.FadeInAndOut);
            scale = data.Float("scale", 1.25f);
            renderBehindPlayer = data.Bool("renderBehindPlayer");
            if (renderBehindPlayer)
            {
                Add(new RenderPlayerToBuffer(AfterGameplay));
            }
        }

        public override void Update()
        {
            if (fade == FadeMode.NoFade)
            {
                alpha = 1;
            }
            else
            {
                Player entity = Scene.Tracker.GetEntity<Player>();
                if (entity != null)
                {
                    float alphaTmp = Ease.CubeInOut(Calc.ClampedMap(Math.Abs(X - entity.X), 0f, 128f, 1f, 0f));
                    if (fade == FadeMode.FadeIn)
                    {
                        alphaTmp = Math.Max(alpha, alphaTmp);
                    }
                    alpha = alphaTmp;
                }
            }
            base.Update();
        }

        private void AfterGameplay()
        {
            if (SubHudRenderer.DrawToBuffer)
            {
                var oldUsage = Engine.Graphics.GraphicsDevice.PresentationParameters.RenderTargetUsage;
                Engine.Graphics.GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
                BoardSelect.BeginRender(true);

                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, BoardSelect.renderBehindPlayerShader, Matrix.Identity);

                RenderEntity();

                SubHudRenderer.EndRender();

                Engine.Graphics.GraphicsDevice.SetRenderTarget(GameplayBuffers.Gameplay);
                Engine.Graphics.GraphicsDevice.PresentationParameters.RenderTargetUsage = oldUsage;
            }
        }

        public override void Render()
        {
            if (!renderBehindPlayer)
            {
                RenderEntity();
            }
            else if (!SubHudRenderer.DrawToBuffer)
            {
                SubHudRenderer.EndRender();

                BoardSelect.BeginRender(false);
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, BoardSelect.renderBehindPlayerShader, Matrix.Identity);

                RenderEntity();

                SubHudRenderer.EndRender();
                SubHudRenderer.BeginRender();
            }
        }

        private void RenderEntity()
        {
            Vector2 cam = SceneAs<Level>().Camera.Position;
            Vector2 posTmp = cam + new Vector2(Celeste.Celeste.GameWidth, Celeste.Celeste.GameHeight) / 2;
            Vector2 pos = (Position - cam + (Position - posTmp) * parallax) * 6f;
            if (SaveData.Instance != null && SaveData.Instance.Assists.MirrorMode)
            {
                pos.X = Celeste.Celeste.TargetWidth - pos.X;
            }
            if (outline)
            {
                ActiveFont.DrawOutline(text, pos, new Vector2(0.5f, 0.5f), Vector2.One * scale, Color.White * alpha, 2f, Color.Black * alpha);
            }
            else
            {
                ActiveFont.Draw(text, pos, new Vector2(0.5f, 0.5f), Vector2.One * scale, Color.White * alpha);
            }
        }
    }
}
