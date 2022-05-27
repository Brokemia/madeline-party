using Celeste;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadelineParty.GreenSpace {
    [GreenSpace("gondola")]
    class GS_Gondola : GreenSpaceEvent {
        private static MTexture back = GFX.Game["objects/gondola/back"],
                                front = GFX.Game["objects/gondola/front"],
                                left = GFX.Game["objects/madelineparty/gondola/cliffsideLeft"],
                                right = GFX.Game["objects/madelineparty/gondola/cliffsideRight"],
                                top = GFX.Game["objects/gondola/top"];

        private Dictionary<Vector2, Vector2> destinations = new Dictionary<Vector2, Vector2>();
        private Dictionary<Vector2, float> progress = new Dictionary<Vector2, float>();

        private static readonly Vector2 destinationOffset = new Vector2(300, -100);
        private const int spaceWidth = 8 * 6;
        private static readonly Vector2 gondolaStartOffset = new Vector2(spaceWidth / 2 + 3, -spaceWidth - 6);
        private static readonly Vector2 gondolaEndOffset = new Vector2(-spaceWidth / 2 + 3 - back.Width, -spaceWidth / 2 - 18);

        public override void RunGreenSpace(BoardController board, BoardController.BoardSpace space, Action after) {

            after();
        }

        public override void RenderSubHUD(BoardController.BoardSpace space) {
            base.RenderSubHUD(space);
            progress[space.screenPosition] = progress.OrDefault(space.screenPosition, 0) + Engine.DeltaTime / 4;
            if(progress[space.screenPosition] > 1) {
                progress[space.screenPosition] = 1;
            }
            Vector2 destPos = getDestination(space);
            Vector2 leftPos = space.screenPosition + new Vector2(10, -spaceWidth / 2 - left.Height / 2 + 6);
            Vector2 rightPos = destPos + new Vector2(-8, -spaceWidth / 2 - left.Height / 2 + 1);
            float p = progress.OrDefault(space.screenPosition, 0);
            Vector2 gondolaTopLeft = Vector2.Lerp(space.screenPosition + gondolaStartOffset, destPos + gondolaEndOffset, Ease.CubeInOut(p));
            RenderRope(leftPos + new Vector2(10, 3),
                rightPos + new Vector2(-10, -3),
                gondolaTopLeft + new Vector2(39, 9),
                gondolaTopLeft + new Vector2(40, 9));

            back.Draw(gondolaTopLeft);
            front.Draw(gondolaTopLeft);
            top.Draw(gondolaTopLeft + new Vector2(top.Width / 2f, 12f), new Vector2(top.Width / 2f, 12f), Color.White, 1, Calc.Angle(space.screenPosition, destPos));
            left.DrawCentered(leftPos);
            right.DrawCentered(rightPos, Color.White, 1, 0, SpriteEffects.FlipHorizontally);
        }

        private void RenderRope(Vector2 leftPos, Vector2 rightPos, Vector2 gondolaLeft, Vector2 gondolaRight) {
            Vector2 slope = (leftPos - rightPos).SafeNormalize();
            for (int i = 0; i < 2; i++) {
                Vector2 value5 = Vector2.UnitY * i;
                Draw.Line(leftPos + value5, gondolaLeft + slope * 6 + value5, Color.Black);
                Draw.Line(gondolaRight - slope * 6 + value5, rightPos + value5, Color.Black);
            }
        }

        // We cache the results of this
        private Vector2 getDestination(BoardController.BoardSpace start) {
            if(destinations.TryGetValue(start.screenPosition, out Vector2 res)) {
                return res;
            } else {
                Vector2 result = BoardController.boardSpaces
                    .Where(s => s.position != start.position)
                    .OrderBy(s => (s.screenPosition - start.screenPosition - destinationOffset).LengthSquared())
                    .First().screenPosition;
                destinations[start.screenPosition] = result;
                return result;
            }
        }
    }
}
