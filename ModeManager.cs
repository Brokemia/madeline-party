using Celeste;
using Microsoft.Xna.Framework;

namespace MadelineParty {
    public class ModeManager {
        public static ModeManager Instance { get; private set; } = new ModeManager();

        public string Mode { get; set; } = "Board";

        public void AfterPlayerSelect(Level level) {
            level.OnEndOfFrame += delegate {
                GameData.Instance.currentPlayerSelection = null;
                level.Teleport(Mode.Equals("Minigame") ? "Game_MinigameHub" : "Game_SettingsConfig",
                    () => new Vector2(level.Bounds.Left, GameData.Instance.gnetHost ? level.Bounds.Top : level.Bounds.Bottom));
            };
        }
    }
}
