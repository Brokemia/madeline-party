using Celeste;
using MadelineParty.Board;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace MadelineParty
{
    public class MinigameScoreDisplay : MinigameDisplay {
        private static readonly Func<uint, object> defaultProcessor = s => s;
        private string format;
        private Func<uint, object> statusProcessor;

        public MinigameScoreDisplay(MinigameEntity minigame, string format = "{0}", Func<uint, object> processor = null) : base(minigame) {
            statusProcessor = processor ?? defaultProcessor;
            this.format = format;
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
                        
                        RenderScore(string.Format(format, statusProcessor(GameData.Instance.minigameStatus.ContainsKey(i) ? GameData.Instance.minigameStatus[i] : 0)),
                            i, index, lerpIn, 120);
                        index++;
                    }
                }
            }
        }

        protected void RenderScore(string text, int player, int index, float lerpIn, float xOffset) {
            GFX.Gui[PlayerToken.GetFullPath(BoardController.TokenPaths[player]) + "00"].DrawCentered(new Vector2(lerpIn + 40, Y - 8 + 44 * (index + 1.5f)), Color.White, .3f);

            PixelFont font = Dialog.Languages["english"].Font;
            float fontFaceSize = Dialog.Languages["english"].FontFaceSize;
            font.DrawOutline(fontFaceSize, text, new Vector2(lerpIn + xOffset, Y + 44f * (index + 2)), new Vector2(0.5f, 1f), Vector2.One * (1f + wiggler.Value * 0.15f), Color.White, 2f, Color.Black);
        }

    }
}
