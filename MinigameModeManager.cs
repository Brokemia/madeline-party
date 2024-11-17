using Celeste;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace MadelineParty {
    public class MinigameModeManager : ModeManager {
        public const string MODE = "Minigame";
        
        public override string Mode => MODE;

        // No rewards in minigame mode
        public override void DistributeMinigameRewards(List<int> winners) { }

        protected override void SendToPostPlayerRanking(Level level) {
            level.Teleport("Game_MinigameHub",
                    () => level.GetSpawnPoint(new Vector2(level.Bounds.Left, GameData.Instance.celesteNetHost ? level.Bounds.Top : level.Bounds.Bottom)));
        }

        protected override void SendToPostPlayerSelect(Level level) {
            level.OnEndOfFrame += delegate {
                GameData.Instance.currentPlayerSelection = null;
                level.Teleport("Game_MinigameHub",
                    () => level.GetSpawnPoint(new Vector2(level.Bounds.Left, GameData.Instance.celesteNetHost ? level.Bounds.Top : level.Bounds.Bottom)));
            };
        }

        public override void AfterMinigameChosen(Level level) {
            GameData.Instance.minigameStatus.Clear();
            level.Remove(level.Entities.FindAll<MinigameDisplay>());
            level.OnEndOfFrame += delegate {
                level.Teleport(GameData.Instance.minigame, () => level.Session.LevelData.Spawns.Count >= 4 ? level.Session.LevelData.Spawns[GameData.Instance.realPlayerID] : level.GetSpawnPoint(new Vector2(level.Bounds.Left, level.Bounds.Top)));
            };
        }
    }
}
