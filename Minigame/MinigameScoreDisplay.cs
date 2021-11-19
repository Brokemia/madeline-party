using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadelineParty {
    public class MinigameScoreDisplay : MinigameCountdownDisplay {

		public MinigameScoreDisplay(MinigameEntity minigame) : base(minigame) {
			Y = 120;
        }

		public override void Render() {
			base.Render();
			if (DrawLerp > 0f) {
				float num = -300f * Ease.CubeIn(1f - DrawLerp);

				int index = 0;
				for(int i = 0; i < GameData.players.Length; i++) {
					if (GameData.players[i] != null) {
						scoreBg.Draw(new Vector2(num, Y + 44 * (index + 1)));
						PlayerToken token = GameData.players[i].token;
						token.textures[(int)token.frame].DrawCentered(new Vector2(num + 60, Y - 8 + 44 * (index + 1.5f)), Color.White, .3f);

						PixelFont font = Dialog.Languages["english"].Font;
						float fontFaceSize = Dialog.Languages["english"].FontFaceSize;
						font.DrawOutline(fontFaceSize, GameData.minigameStatus.ContainsKey(i) ? GameData.minigameStatus[i].ToString() : "0", new Vector2(num + 120, Y + 44f * (index + 2)), new Vector2(0.5f, 1f), Vector2.One * (1f + wiggler.Value * 0.15f), Color.White, 2f, Color.Black);
						index++;
					}
				}
			}
		}

	}
}
