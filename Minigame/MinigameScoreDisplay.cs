using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadelineParty {
    public class MinigameScoreDisplay : MinigameTimeDisplay {
        private int max;

        public MinigameScoreDisplay(MinigameEntity minigame, int max = -1, bool countDown = true) : base(minigame, countDown) {
            this.max = max;
            Y = 120;
        }

        public override void Render() {
            base.Render();
            if (DrawLerp > 0f) {
                float num = -300f * Ease.CubeIn(1f - DrawLerp);

                int index = 0;
                for(int i = 0; i < GameData.Instance.players.Length; i++) {
                    if (GameData.Instance.players[i] != null) {
                        scoreBg.Draw(new Vector2(num, Y + 44 * (index + 1)));
                        PlayerToken token = GameData.Instance.players[i].token;
                        token.textures[(int)token.frame].DrawCentered(new Vector2(num + 40, Y - 8 + 44 * (index + 1.5f)), Color.White, .3f);

                        PixelFont font = Dialog.Languages["english"].Font;
                        float fontFaceSize = Dialog.Languages["english"].FontFaceSize;
                        string text = (GameData.Instance.minigameStatus.ContainsKey(i) ? GameData.Instance.minigameStatus[i].ToString() : "0") + (max > 0 ? "/" + max : "");
                        font.DrawOutline(fontFaceSize, text, new Vector2(num + 120, Y + 44f * (index + 2)), new Vector2(0.5f, 1f), Vector2.One * (1f + wiggler.Value * 0.15f), Color.White, 2f, Color.Black);
                        index++;
                    }
                }
            }
        }

    }
}
