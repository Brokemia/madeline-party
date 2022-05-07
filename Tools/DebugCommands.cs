using Celeste;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadelineParty.Tools {

    class DebugCommands {
		public static string greenSpaceEvent = "seeker";

		[Command("rig_minigame", "set the next minigame that will be chosen (if you are the host)")]
		private static void CmdRigMinigame(int id = 1) {
			if (GameData.gnetHost) {
				BoardController.riggedMinigame = (Engine.Scene as Level).Session.MapData.Levels.Find((obj) => obj.Name.StartsWith(string.Format("z_Minigame{0, 0:D2}-", id)));
				Engine.Commands.Log("Playing minigame " + BoardController.riggedMinigame.Name + " next");
			} else {
				Engine.Commands.Log("You are not the host");
			}
		}

		[Command("be", "editor for madeline party boards")]
		private static void CmdBoardEditor() {
			Engine.Scene = new BoardEditor();
			Engine.Commands.Open = false;
		}

		[Command("green_space", "set the specific event to be used for any green spaces set after this")]
		private static void CmdGreenSpace(string spaceEvent) {
			greenSpaceEvent = spaceEvent;
		}

		[Command("gse", "run a green space event")]
		private static void CmdGreenSpaceEvent(string spaceEvent, int space = 2) {
			BoardController.Instance.DoGreenSpace(new BoardController.BoardSpace {
				greenSpaceEvent = spaceEvent,
				x = BoardController.boardSpaces[space].x,
				y = BoardController.boardSpaces[space].y
			}, null);
			Engine.Commands.ExecuteCommand("clear", new string[0]);
		}

		[Command("set_space", "move a player's token to a specific space")]
		private static void SetSpace(int playerID, int spaceID) {
			PlayerToken token = BoardController.Instance.playerTokens[playerID];
			var space = BoardController.boardSpaces[spaceID];
			token.currentSpace = space;
			token.Position = space.screenPosition;
        }
	}
}
