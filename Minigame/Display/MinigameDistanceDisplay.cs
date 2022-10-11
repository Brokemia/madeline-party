using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System.Linq;

namespace MadelineParty {
    public class MinigameDistanceDisplay : MinigameScoreDisplay {
        public MinigameDistanceDisplay(MinigameEntity minigame) : base(minigame) {
            Y = 120;
        }

        public override void Render() {
            if (DrawLerp > 0f) {
                float lerpIn = -300f * Ease.CubeIn(1f - DrawLerp);

                int index = 0;
                for (int i = 0; i < GameData.Instance.players.Length; i++) {
                    if (GameData.Instance.players[i] != null) {
                        bg.Draw(new Vector2(lerpIn, Y + 44 * (index + 1)));

                        RenderScore(string.Format("{0:F1} M", (GameData.Instance.minigameResults.FirstOrDefault((t) => t.Item1 == i)?.Item2 ?? GameData.Instance.minigameStatus[i]) / 50.0),
                            i, index, lerpIn, 190);
                        index++;
                    }
                }
            }
        }
    }
}