using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace MadelineParty {
    public abstract class MinigameDisplay : Entity {
        public float CompleteTimer;

        public const int GuiChapterHeight = 58;

        public const int GuiFileHeight = 78;

        protected MTexture bg = GFX.Gui["strawberryCountBG"];

        protected MTexture timerBg = GFX.Gui["madelineparty/timerBG"];

        protected MTexture scoreBg = GFX.Gui["madelineparty/theoCountBG"];

        public float DrawLerp;

        protected Wiggler wiggler;

        protected MinigameEntity minigame;

        public float finalTime = -1;

        public MinigameDisplay(MinigameEntity minigame) {
            Tag = Tags.HUD | Tags.Global | Tags.PauseUpdate | Tags.TransitionUpdate;
            Depth = -100;
            Y = 60f;
            Add(wiggler = Wiggler.Create(0.5f, 4f));
            this.minigame = minigame;
        }

        public override void Update() {
            if (minigame.completed) {
                if (CompleteTimer == 0f) {
                    wiggler.Start();
                }
                CompleteTimer += Engine.DeltaTime;
            }
            DrawLerp = Calc.Approach(DrawLerp, 1, Engine.DeltaTime * 4f);
            base.Update();
        }
    }
}
