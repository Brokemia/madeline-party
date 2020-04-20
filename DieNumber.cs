using System;
using System.Collections;
using Celeste;
using MadelineParty.Ghostnet;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty
{
    public class DieNumber : Entity, IPauseUpdateGhostnetChat
    {
        public int number;
        public int posIndex;
        private BoardController board;
        private Level level;

        public DieNumber(BoardController board)
        {
            this.board = board;
            AddTag(TagsExt.SubHUD);
            AddTag(Tags.PauseUpdate);
            AddTag(Tags.FrozenUpdate);
        }

        public DieNumber(BoardController board, int number)
        {
            this.board = board;
            this.number = number;
            AddTag(TagsExt.SubHUD);
            AddTag(Tags.PauseUpdate);
            AddTag(Tags.FrozenUpdate);
        }

        public DieNumber(BoardController board, int number, int posIndex)
        {
            this.board = board;
            this.number = number;
            this.posIndex = posIndex;
            AddTag(TagsExt.SubHUD);
            AddTag(Tags.PauseUpdate);
            AddTag(Tags.FrozenUpdate);
        }

        public void MoveNumber(Vector2 start, Vector2 end)
        {
            Add(new Coroutine(MoveNumberCoroutine(start, end)));
        }

        private IEnumerator MoveNumberCoroutine(Vector2 start, Vector2 end)
        {
            yield return .4f;
            SimpleCurve curve = new SimpleCurve(start, end, (start + end) / 2f + new Vector2(0f, 48f));
            for (float t = 0f; t < 1f; t += Engine.DeltaTime)
            {
                yield return null;
                Position = curve.GetPoint(Ease.CubeInOut(t));
            }
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            level = SceneAs<Level>();
        }

        public override void Render()
        {
            base.Render();
            //board.diceNumbers[number].Draw((Position - level.LevelOffset) * 6, Vector2.Zero, Color.White, new Vector2(1.5f, 1.5f));
            ActiveFont.DrawOutline(number + 1 + "", (Position - level.LevelOffset) * 6, new Vector2(.5f, .5f), new Vector2(2f, 2f), Color.Blue, 1f, Color.Black);
        }
    }
}
