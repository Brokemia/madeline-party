using Celeste;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty.Tools {

    class DebugCommands {
        [Command("rig_minigame", "set the next minigame that will be chosen (if you are the host)")]
        private static void CmdRigMinigame(int id = 1) {
            if (GameData.Instance.celesteNetHost) {
                BoardController.riggedMinigame = (Engine.Scene as Level).Session.MapData.Levels.Find((obj) => obj.Name.StartsWith(string.Format("z_Minigame{0, 0:D2}-", id)));
                Engine.Commands.Log("Playing minigame " + BoardController.riggedMinigame.Name + " next");
            } else {
                Engine.Commands.Log("You are not the host");
            }
        }

        [Command("gse", "run a green space event")]
        private static void CmdGreenSpaceEvent(string spaceEvent, int space = 2) {
            BoardController.Instance.DoGreenSpace(new BoardController.BoardSpace {
                greenSpaceEvent = spaceEvent,
                x = BoardController.Instance.boardSpaces[space].x,
                y = BoardController.Instance.boardSpaces[space].y
            }, null);
            Engine.Commands.ExecuteCommand("clear", new string[0]);
        }

        [Command("set_space", "move a player's token to a specific space")]
        private static void SetSpace(int playerID, int spaceID) {
            PlayerToken token = BoardController.Instance.playerTokens[playerID];
            var space = BoardController.Instance.boardSpaces[spaceID];
            token.currentSpace = space;
            token.Position = space.screenPosition;
        }

        [Command("set_berries", "set the strawberries of a player in madeline party")]
        private static void SetBerries(int playerID, int amount) {
        GameData.Instance.players[playerID].ChangeStrawberries(amount - GameData.Instance.players[playerID].Strawberries);
        }

        [Command("set_hearts", "set the hearts of a player in madeline party")]
        private static void SetHearts(int playerID, int amount) {
            while (GameData.Instance.players[playerID].Hearts < amount) {
                GameData.Instance.players[playerID].AddHeart();
            }
        }

        [Command("set_turn", "set the current turn in madeline party")]
        private static void SetTurn(int turn) {
            GameData.Instance.turn = turn;
        }

        [Command("rig_roll", "rig the next roll this player will get")]
        private static void SetNextRoll(int roll) {
            BoardController.riggedRoll = roll;
        }

        [Command("move_heart", "move the heart to the specified ID")]
        private static void MoveHeart(int id) {
            GameData.Instance.heartSpaceID = id;
        }
    }
}
