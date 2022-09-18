using Celeste;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty {
    public class MinigameScoreDisplay : MinigameDisplay {
        private int max;

        public MinigameScoreDisplay(MinigameEntity minigame, int max = -1) : base(minigame) {
            this.max = max;
            Y = 120;
        }

        public override void Render() {
            base.Render();
            if (DrawLerp > 0f) {
                float lerpIn = -300f * Ease.CubeIn(1f - DrawLerp);

                int index = 0;
                for (int i = 0; i < GameData.Instance.players.Length; i++) {
                    if (GameData.Instance.players[i] != null) {
                        scoreBg.Draw(new Vector2(lerpIn, Y + 44 * (index + 1)));
                        
                        RenderScore((GameData.Instance.minigameStatus.ContainsKey(i) ? GameData.Instance.minigameStatus[i].ToString() : "0") + (max > 0 ? "/" + max : ""),
                            i, index, lerpIn);
                        index++;
                    }
                }
            }
        }

        protected void RenderScore(string text, int player, int index, float lerpIn) {
            GFX.Gui[PlayerToken.GetFullPath(BoardController.TokenPaths[player]) + "00"].DrawCentered(new Vector2(lerpIn + 40, Y - 8 + 44 * (index + 1.5f)), Color.White, .3f);

            PixelFont font = Dialog.Languages["english"].Font;
            float fontFaceSize = Dialog.Languages["english"].FontFaceSize;
            font.DrawOutline(fontFaceSize, text, new Vector2(lerpIn + 120, Y + 44f * (index + 2)), new Vector2(0.5f, 1f), Vector2.One * (1f + wiggler.Value * 0.15f), Color.White, 2f, Color.Black);
        }

    }
}
