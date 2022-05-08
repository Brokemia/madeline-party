using Microsoft.Xna.Framework;
using System;

namespace MadelineParty.GreenSpace {
    abstract class GreenSpaceEvent {

        public abstract void RunGreenSpace(BoardController board, BoardController.BoardSpace space, Action after);

        public virtual void Render(BoardController.BoardSpace space) {
            BoardController.spaceTextures['g'].DrawCentered(BoardController.Instance.Position + space.position);
        }

        public virtual void RenderSubHUD(BoardController.BoardSpace space) { }

    }
}
