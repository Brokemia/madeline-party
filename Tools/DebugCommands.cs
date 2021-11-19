using Celeste;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadelineParty.Tools {
    class DebugCommands {
		[Command("rig_minigame", "set the next minigame that will be chosen (if you are the host)")]
		private static void CmdRigMinigame(int id = 1) {
			BoardController.riggedMinigame = (Engine.Scene as Level).Session.MapData.Levels.Find((obj) => obj.Name.StartsWith(string.Format("z_Minigame{0, 0:D2}-", id)));
			if (GameData.gnetHost) {
				Engine.Commands.Log("Playing minigame " + BoardController.riggedMinigame.Name + " next");
			} else {
				Engine.Commands.Log("You are not the host");
			}
		}

		[Command("board_editor", "editor for madeline party boards")]
		private static void CmdBoardEditor() {
			Engine.Scene = new BoardEditor();
			Engine.Commands.Open = false;
		}
	}
}
