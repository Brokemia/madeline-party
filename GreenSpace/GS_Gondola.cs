using Celeste;
using Microsoft.Xna.Framework;
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
                                left = GFX.Game["objects/gondola/cliffsideLeft"],
                                right = GFX.Game["objects/gondola/cliffsideRight"];

        private static Dictionary<Vector2, Vector2> destinations = new Dictionary<Vector2, Vector2>();

        private static Vector2 destinationOffset = new Vector2(150, -40);
        private static int spaceWidth = 8 * 6;

        public override void RunGreenSpace(BoardController board, BoardController.BoardSpace space, Action after) {
            after();
        }

        public override void RenderSubHUD(BoardController.BoardSpace space) {
            base.RenderSubHUD(space);
            back.Draw(space.screenPosition + new Vector2(spaceWidth / 2, -spaceWidth));
            front.Draw(space.screenPosition + new Vector2(spaceWidth / 2, -spaceWidth));
            left.DrawCentered(space.screenPosition + new Vector2(0, spaceWidth / 4));
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
