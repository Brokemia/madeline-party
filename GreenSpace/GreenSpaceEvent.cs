using Microsoft.Xna.Framework;
using System;

namespace MadelineParty.GreenSpace {
    abstract class GreenSpaceEvent {

        public abstract void RunGreenSpace(BoardController board, BoardController.BoardSpace space, Action after);

        public virtual void Render(Vector2 center) {
            BoardController.spaceTextures['g'].DrawCentered(center);
        }

    }
}
