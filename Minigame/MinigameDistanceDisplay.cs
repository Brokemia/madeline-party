using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System.Linq;

namespace MadelineParty {
    public class MinigameDistanceDisplay : MinigameTimeDisplay {
        MinigameInfinityTrigger infinityMinigame;

        public MinigameDistanceDisplay(MinigameEntity minigame) : base(minigame) {
            Y = 120;
            infinityMinigame = minigame as MinigameInfinityTrigger;

        }

        public override void Render() {
            base.Render();
            if (DrawLerp > 0f) {
                float num = -300f * Ease.CubeIn(1f - DrawLerp);

                int index = 0;
                for (int i = 0; i < GameData.players.Length; i++) {
                    if (GameData.players[i] != null) {
                        bg.Draw(new Vector2(num, Y + 44 * (index + 1)));
                        PlayerToken token = GameData.players[i].token;
                        token.textures[(int)token.frame].DrawCentered(new Vector2(num + 60, Y - 8 + 44 * (index + 1.5f)), Color.White, .3f);

                        PixelFont font = Dialog.Languages["english"].Font;
                        float fontFaceSize = Dialog.Languages["english"].FontFaceSize;
                        font.DrawOutline(fontFaceSize, string.Format("{0:F1} M", (GameData.minigameResults.FirstOrDefault((t) => t.Item1 == i)?.Item2 ?? GameData.minigameStatus[i]) / 50.0), new Vector2(num + 200, Y + 44f * (index + 2)), new Vector2(0.5f, 1f), Vector2.One * (1f + wiggler.Value * 0.15f), Color.White, 2f, Color.Black);
                        index++;
                    }
                }
            }
        }
    }
}