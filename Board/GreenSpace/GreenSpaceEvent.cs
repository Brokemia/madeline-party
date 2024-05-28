using System;
using System.Collections.Generic;
using MadelineParty.Board;

namespace MadelineParty.Board.GreenSpace
{
    public abstract class GreenSpaceEvent
    {

        public virtual void LoadContent() { }

        public abstract void RunGreenSpace(BoardController board, BoardController.BoardSpace space, Action after);

        public virtual void Render(BoardController.BoardSpace space, List<BoardController.BoardSpace> spaces)
        {
            BoardController.spaceTextures[space.type].DrawCentered(BoardController.Instance.Position + space.position);
        }

        public virtual void RenderSubHUD(BoardController.BoardSpace space, List<BoardController.BoardSpace> spaces) { }

    }
}
