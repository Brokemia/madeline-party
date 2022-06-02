using Celeste;
using MadelineParty.Multiplayer;
using MadelineParty.Multiplayer.General;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MadelineParty.GreenSpace {
    [GreenSpace("gondola")]
    class GS_Gondola : GreenSpaceEvent {
        private static MTexture back = GFX.Game["objects/gondola/back"],
                                front = GFX.Game["objects/gondola/front"],
                                left = GFX.Game["objects/madelineparty/gondola/cliffsideLeft"],
                                right = GFX.Game["objects/madelineparty/gondola/cliffsideRight"],
                                top = GFX.Game["objects/gondola/top"];

        private const int GONDOLA_COST = 3;

        private Dictionary<Vector2, BoardController.BoardSpace> destinations = new();
        private Dictionary<Vector2, float> progress = new();
        private static readonly Vector2 destinationOffset = new Vector2(300, -100);
        private const int spaceWidth = 8 * 6;
        private static readonly Vector2 gondolaStartOffset = new Vector2(spaceWidth / 2 + 3, -spaceWidth - 6);
        private static readonly Vector2 gondolaEndOffset = new Vector2(-spaceWidth / 2 + 3 - back.Width, -spaceWidth / 2 - 18);

        // These only exist so that HandlePlayerChoice can use them, DO NOT use them outside of that
        private BoardController lastBoard;
        private BoardController.BoardSpace lastSpace;
        private Action lastAfter;
        public override void RunGreenSpace(BoardController board, BoardController.BoardSpace space, Action after) {
            lastBoard = board;
            lastSpace = space;
            lastAfter = after;
            if (GameData.players[board.CurrentPlayerToken.id].strawberries >= GONDOLA_COST) {
                board.GetLeftButton(board.CurrentPlayerToken).OnPressButton += delegate {
                    MultiplayerSingleton.Instance.Send(new PlayerChoice { choiceType = "TAKEGONDOLA", choice = 1 });
                    GondolaChoiceMade(true, board, space, after);
                };
                board.SetLeftButtonStatus(board.CurrentPlayerToken, LeftButton.Modes.Confirm);
            }
            board.GetRightButton(board.CurrentPlayerToken).OnPressButton += delegate {
                MultiplayerSingleton.Instance.Send(new PlayerChoice { choiceType = "TAKEGONDOLA", choice = 0 });
                GondolaChoiceMade(false, board, space, after);
            };
            board.SetRightButtonStatus(board.CurrentPlayerToken, RightButton.Modes.Cancel);
            board.GetScoreboard(board.CurrentPlayerToken).BuyArbitrary(GFX.Game["decals/madelineparty/gondola"], GONDOLA_COST);
        }

        private void GondolaChoiceMade(bool taking, BoardController board, BoardController.BoardSpace space, Action after) {
            board.SetLeftButtonStatus(board.CurrentPlayerToken, LeftButton.Modes.Inactive);
            board.SetRightButtonStatus(board.CurrentPlayerToken, RightButton.Modes.Inactive);
            board.GetScoreboard(board.CurrentPlayerToken).SetCurrentMode(GameScoreboard.Modes.NORMAL);
            if(!taking) {
                after();
                return;
            }

            board.ChangeStrawberries(board.CurrentPlayerToken.id, -GONDOLA_COST);
            board.Add(new Coroutine(GondolaRoutine(board, space, after)));
        }

        private IEnumerator GondolaRoutine(BoardController board, BoardController.BoardSpace space, Action after) {
            Vector2 gondolaCenterOffset = new Vector2(back.Width / 2, 2 * back.Height / 3);
            PlayerToken token = board.CurrentPlayerToken;
            BoardController.BoardSpace dest = getDestination(space);
            Vector2 destPos = dest.screenPosition;
            Vector2 target = CalcGondolaPosition(space.screenPosition, destPos, 0) + gondolaCenterOffset;
            while (!token.Position.Equals(target)) {
                yield return null;
                token.Position = Calc.Approach(token.Position, target, BoardController.TOKEN_SPEED * Engine.DeltaTime);
            }
            yield return 0.3f;
            // Begin moving gondola
            progress[space.screenPosition] = 0;
            while (progress[space.screenPosition] < 1) {
                yield return null;
                progress[space.screenPosition] += Engine.DeltaTime / 3;
                token.Position = CalcGondolaPosition(space.screenPosition, destPos, progress[space.screenPosition]) + gondolaCenterOffset;
            }
            progress[space.screenPosition] = 1;
            yield return 0.3f;
            while (!token.Position.Equals(destPos)) {
                yield return null;
                token.Position = Calc.Approach(token.Position, destPos, BoardController.TOKEN_SPEED * Engine.DeltaTime);
            }
            board.CurrentPlayerToken.currentSpace = dest;
            yield return 0.2f;
            // Send gondola back
            progress[space.screenPosition] = 1;
            while (progress[space.screenPosition] > 0) {
                yield return null;
                progress[space.screenPosition] -= Engine.DeltaTime / 3;
            }
            progress[space.screenPosition] = 0;
            after();
        }

        private Vector2 CalcGondolaPosition(Vector2 start, Vector2 end, float progress) {
            return Vector2.Lerp(start + gondolaStartOffset, end + gondolaEndOffset, Ease.CubeInOut(progress));
        }

        public override void RenderSubHUD(BoardController.BoardSpace space) {
            base.RenderSubHUD(space);
            Vector2 destPos = getDestination(space).screenPosition;
            Vector2 leftPos = space.screenPosition + new Vector2(10, -spaceWidth / 2 - left.Height / 2 + 6);
            Vector2 rightPos = destPos + new Vector2(-8, -spaceWidth / 2 - left.Height / 2 + 1);
            Vector2 gondolaTopLeft = CalcGondolaPosition(space.screenPosition, destPos, progress.OrDefault(space.screenPosition, 0));
            RenderRope(leftPos + new Vector2(10, 3),
                rightPos + new Vector2(-10, -10),
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
        private BoardController.BoardSpace getDestination(BoardController.BoardSpace start) {
            if(destinations.TryGetValue(start.screenPosition, out BoardController.BoardSpace res)) {
                return res;
            } else {
                var result = BoardController.boardSpaces
                    .Where(s => s.position != start.position)
                    .OrderBy(s => (s.screenPosition - start.screenPosition - destinationOffset).LengthSquared())
                    .First();
                destinations[start.screenPosition] = result;
                return result;
            }
        }

        public override void LoadContent() {
            base.LoadContent();
            MultiplayerSingleton.Instance.RegisterHandler<PlayerChoice>(HandlePlayerChoice);
        }

        private void HandlePlayerChoice(MPData data) {
            if (data is not PlayerChoice playerChoice) return;
            // If another player in our party has made a gondola choice
            if (GameData.celestenetIDs.Contains(playerChoice.ID) && playerChoice.ID != MultiplayerSingleton.Instance.GetPlayerID() && playerChoice.choice.Equals("TAKEGONDOLA")) {
                GondolaChoiceMade(playerChoice.choice == 1, lastBoard, lastSpace, lastAfter);
            }
        }
    }
}
