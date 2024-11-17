using Celeste;
using MadelineParty.Board;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace MadelineParty {
    public class BoardModeManager : ModeManager {
        public const string MODE = "Board";

        public override string Mode => MODE;

        protected override void SendToPostPlayerRanking(Level level) {
            level.Teleport(GameData.Instance.board,
                    () => GameData.Instance.realPlayerID switch {
                            0 => level.GetSpawnPoint(new Vector2(level.Bounds.Left, level.Bounds.Top)),
                            1 => level.GetSpawnPoint(new Vector2(level.Bounds.Right, level.Bounds.Top)),
                            2 => level.GetSpawnPoint(new Vector2(level.Bounds.Left, level.Bounds.Bottom)),
                            _ => level.GetSpawnPoint(new Vector2(level.Bounds.Right, level.Bounds.Bottom))
                        });
        }

        protected override void SendToPostPlayerSelect(Level level) {
            level.OnEndOfFrame += delegate {
                GameData.Instance.currentPlayerSelection = null;
                level.Teleport("Game_SettingsConfig",
                    () => level.GetSpawnPoint(new Vector2(level.Bounds.Left, GameData.Instance.celesteNetHost ? level.Bounds.Top : level.Bounds.Bottom)));
            };
        }

        public override void DistributeMinigameRewards(List<int> winners) {
            foreach (int winnerID in winners) {
                BoardController.QueueStrawberryChange(winnerID, 10);
            }
        }

        public override void AfterMinigameChosen(Level level) {
            var selectUI = new MinigameSelectUI(GameData.Instance.minigame);
            level.Add(selectUI);
            selectUI.OnSelect = selection => {
                GameData.Instance.playedMinigames.Add(selection);
                Player player = level.Tracker.GetEntity<Player>();
                level.OnEndOfFrame += delegate {
                    player.Speed = Vector2.Zero;
                    Leader.StoreStrawberries(player.Leader);
                    level.Remove(player);
                    level.UnloadLevel();

                    level.Session.Level = selection;
                    level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(level.Bounds.Left, level.Bounds.Top));
                    level.LoadLevel(Player.IntroTypes.None);

                    Leader.RestoreStrawberries(player.Leader);
                };
            };
        }
    }
}
