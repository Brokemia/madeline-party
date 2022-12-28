using Celeste;
using MadelineParty.SubHud;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;

namespace MadelineParty.GreenSpace
{
    [GreenSpace("seeker")]
    class GS_Seeker : GreenSpaceEvent {
        private static Vector2 startOffset = new Vector2(120, -36);

        public override void RunGreenSpace(BoardController board, BoardController.BoardSpace space, Action after) {
            Sprite seekerSprite = GFX.SpriteBank.Create("seeker");
            SubHudSprite seeker = new SubHudSprite(seekerSprite);
            seeker.Collider = new Hitbox(50, 50, -25, -25);
            seeker.Depth = -40000;
            board.Scene.Add(seeker);
            seeker.Position = space.screenPosition + startOffset;
            seekerSprite.FlipX = true;
            seekerSprite.Play("recover");
            seekerSprite.OnFinish += s => seekerSprite.Play("windUp");
            seekerSprite.Scale = new Vector2(2, 2);
            board.Add(new Coroutine(SeekerCharge(board, after, seeker, seeker.Position, space.screenPosition - startOffset / 2)));
        }

        private IEnumerator SeekerCharge(BoardController board, Action after, SubHudSprite seeker, Vector2 start, Vector2 end) {
            yield return 0.6f;
            float speed = -6;
            while ((start - end).Sign().Equals((seeker.Position - end).Sign())) {
                CheckSeekerCollision(board, seeker);
                speed = Calc.Approach(speed, 26f, 30f * Engine.DeltaTime);
                seeker.Position += speed * (end - start).SafeNormalize();
                yield return null;
            }
            while (speed > 0.5) {
                CheckSeekerCollision(board, seeker);
                speed = Calc.Approach(speed, 0, 40f * Engine.DeltaTime);
                seeker.Position += speed * (end - start).SafeNormalize();
                yield return null;
            }
            seeker.sprite.Play("takeHit");
            seeker.sprite.OnLoop = s => { seeker.RemoveSelf(); after(); };
        }

        private void CheckSeekerCollision(BoardController board, SubHudSprite seeker) {
            PlayerToken token;
            if ((token = seeker.CollideFirst<PlayerToken>()) != null) {
                token.Add(new Coroutine(token.Respawn()));
                token.OnRespawn = (t) => board.ChangeStrawberries(t.id, -5);
            }

        }
    }
}
