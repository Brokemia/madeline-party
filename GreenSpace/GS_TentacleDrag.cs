using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MadelineParty.GreenSpace {
    [GreenSpace("tentacleDrag")]
    class GS_TentacleDrag : GreenSpaceEvent {
        private const float scaleFactor = 3;

        public override void RunGreenSpace(BoardController board, BoardController.BoardSpace space, Action after) {
            Sprite tentacleSprite = GFX.SpriteBank.Create("madelinePartyTentacle");
            SubHUDSprite tentacle = new SubHUDSprite(tentacleSprite);
            Vector2 targetPosition = space.screenPosition - new Vector2(tentacleSprite.Width * scaleFactor / 2, 27 * scaleFactor);
            tentacle.Depth = -40000;
            board.Scene.Add(tentacle);
            tentacle.Position = targetPosition;
            List<Sprite> tentacleExtensions = new List<Sprite>();

            float posOffset = tentacleSprite.Height;
            tentacleSprite.Scale = new Vector2(scaleFactor);
            while (tentacle.Position.Y + posOffset < Engine.ViewHeight) {
                Sprite extension = GFX.SpriteBank.Create("madelinePartyTentacle");
                extension.Play("long");
                tentacle.Add(extension);
                extension.Position = new Vector2(0, posOffset * scaleFactor);
                extension.Scale = new Vector2(scaleFactor);
                posOffset += extension.Height;
                tentacleExtensions.Add(extension);
            }
            tentacle.Position.Y = Engine.ViewHeight;

            Tween riseTween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeIn, 0.25f, true);
            riseTween.OnUpdate = t => tentacle.Position.Y = Calc.LerpClamp(Engine.ViewHeight, targetPosition.Y, t.Percent);
            riseTween.OnComplete = t => {
                tentacleSprite.Play("tentacle_grab");
                board.Add(new Coroutine(PullDown(board, after, tentacle, space)));
            };
            board.Add(riseTween);
        }

        private IEnumerator PullDown(BoardController board, Action after, SubHUDSprite tentacle, BoardController.BoardSpace startSpace) {
            yield return 1.5f;
            // The highest space with the exact same x
            // Or if there is no space with the same x
            // Then the closest space to the point at the bottom of the screen but with the same x
            BoardController.BoardSpace endSpace = BoardController.boardSpaces
                .FindAll(s => s.x == startSpace.x && !s.screenPosition.Equals(startSpace.screenPosition))
                .OrderBy(a => a.y)
                .DefaultIfEmpty(BoardController.boardSpaces
                    .OrderBy(a => (a.screenPosition - new Vector2(startSpace.screenPosition.X, Engine.ViewHeight)).LengthSquared())
                    .First(s => !s.screenPosition.Equals(startSpace.screenPosition)))
                .First();

            var grabbed = board.playerTokens.Where(t => t != null && t.currentSpace.screenPosition.Equals(startSpace.screenPosition));
            tentacle.sprite.Play("tentacle_pull");
            Vector2 offset = new Vector2(tentacle.sprite.Width * scaleFactor / 2, 27 * scaleFactor);
            Vector2 speed = (endSpace.screenPosition - startSpace.screenPosition).SafeNormalize() * 150 * Engine.DeltaTime;
            Vector2 pos = startSpace.screenPosition;
            while (tentacle.Position != endSpace.screenPosition - offset) {
                pos = Vector2.Clamp(pos + speed, startSpace.screenPosition, endSpace.screenPosition);
                speed += speed.SafeNormalize() * 150 * Engine.DeltaTime;
                dragToPos(tentacle, grabbed, pos, offset);
                yield return null;
            }
            yield return 0.5f;
            float tentacleStartY = tentacle.Position.Y;
            tentacle.sprite.Rate = 0.5f;
            tentacle.sprite.Play("tentacle_ungrab");
            
            tentacle.sprite.OnLastFrame = s => {
                tentacle.sprite.OnLastFrame = null;
                Tween fallTween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeInOut, 0.25f, true);
                fallTween.OnUpdate = t => tentacle.Position.Y = Calc.LerpClamp(tentacleStartY, Engine.ViewHeight + tentacle.sprite.Height * scaleFactor, t.Percent);
                fallTween.OnComplete = t => {
                    tentacle.RemoveSelf();
                    after();
                };
                board.Add(fallTween);
            };

            foreach(PlayerToken token in grabbed) {
                token.currentSpace = endSpace;
            }
        }

        private void dragToPos(SubHUDSprite tentacle, IEnumerable<PlayerToken> tokens, Vector2 position, Vector2 tentacleOffset) {
            tentacle.Position = position - tentacleOffset;
            foreach(PlayerToken token in tokens) {
                token.Position = position;
            }
        }
    }
}
