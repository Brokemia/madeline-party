using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadelineParty {
    public class MinigameTimeDisplay : MinigameDisplay {
        protected MTexture scoreBg = GFX.Gui["madelineparty/theoCountBG"];

        private bool countDown;

        public MinigameTimeDisplay(MinigameEntity minigame, bool countDown = false) : base(minigame) {
            this.countDown = countDown;
        }

        public override void Render() {
            if(!countDown) {
                base.Render();
            } else if (DrawLerp > 0f) {
                float timerY = -56f * Ease.CubeIn(1f - DrawLerp);
                Level level = Scene as Level;
                Session session = level.Session;

                TimeSpan timeSpan = TimeSpan.FromTicks((long)(((minigame.completed || MinigameEntity.startTime < 0) ? 0 : 30 - (level.RawTimeActive - MinigameEntity.startTime)) * 10000000));
                string timeString = timeSpan.ToString("ss\\.fff");
                timerBg.Draw(new Vector2(816, timerY));
                DrawTime(new Vector2(816 + 64f, timerY + 52f), timeString, 1f + wiggler.Value * 0.15f, true, minigame.completed, false);
            }
        }
    }
}
