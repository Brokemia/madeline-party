using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadelineParty {
    public class MinigameCountdownDisplay : MinigameDisplay {
		protected MTexture scoreBg = GFX.Gui["madelineparty/theoCountBG"];
		Level level;

		public MinigameCountdownDisplay(MinigameEntity minigame) : base(minigame) {
		}

		public override void Added(Scene scene) {
			base.Added(scene);
			level = SceneAs<Level>();
		}

		public override void Render() {
			if (DrawLerp > 0f) {
				float timerY = -56f * Ease.CubeIn(1f - DrawLerp);
				Level level = Scene as Level;
				Session session = level.Session;

				TimeSpan timeSpan = TimeSpan.FromTicks((long)((minigame.completed || MinigameEntity.startTime < 0 ? 0 : 30 - (level.RawTimeActive - MinigameEntity.startTime)) * 10000000));
				string timeString = timeSpan.ToString("ss\\.fff");
				timerBg.Draw(new Vector2(816, timerY));
				DrawTime(new Vector2(816 + 64f, timerY + 52f), timeString, 1f + wiggler.Value * 0.15f, session.StartedFromBeginning, level.Completed, session.BeatBestTime);
			}
		}
	}
}
