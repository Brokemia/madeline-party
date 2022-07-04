using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

namespace MadelineParty {
    public class ModeManager {
        public const string BOARD_MODE = "Board";
        public const string MINIGAME_MODE = "Minigame";

        public static ModeManager Instance { get; private set; } = new ModeManager();

        public string Mode { get; set; } = BOARD_MODE;

        public void AfterPlayerSelect(Level level) {
            level.OnEndOfFrame += delegate {
                GameData.Instance.currentPlayerSelection = null;
                level.Teleport(Mode.Equals(MINIGAME_MODE) ? "Game_MinigameHub" : "Game_SettingsConfig",
                    () => level.GetSpawnPoint(new Vector2(level.Bounds.Left, GameData.Instance.gnetHost ? level.Bounds.Top : level.Bounds.Bottom)));
            };
        }

        public void AfterPlayersRanked(Level level) {
            level.OnEndOfFrame += delegate {
                GameData.Instance.minigameResults.Clear();
                GameData.Instance.minigameStatus.Clear();
                level.Teleport(Mode.Equals(MINIGAME_MODE) ? "Game_MinigameHub" : "Game_MainRoom",
                    () => {
                        if(Mode.Equals(MINIGAME_MODE)) {
                            return level.GetSpawnPoint(new Vector2(level.Bounds.Left, GameData.Instance.gnetHost ? level.Bounds.Top : level.Bounds.Bottom));
                        }
                        return GameData.Instance.realPlayerID switch {
                            0 => level.GetSpawnPoint(new Vector2(level.Bounds.Left, level.Bounds.Top)),
                            1 => level.GetSpawnPoint(new Vector2(level.Bounds.Right, level.Bounds.Top)),
                            2 => level.GetSpawnPoint(new Vector2(level.Bounds.Left, level.Bounds.Bottom)),
                            _ => level.GetSpawnPoint(new Vector2(level.Bounds.Right, level.Bounds.Bottom))
                        };
                    });
            };
        }

        public void DistributeMinigameRewards(List<int> winners) {
            if(Mode.Equals(BOARD_MODE)) {
                foreach (int winnerID in winners) {
                    BoardController.QueueStrawberryChange(winnerID, 10);
                }
            }
        }

        public void AfterMinigameChosen() {
            Level level = Engine.Scene as Level;
            if (Mode.Equals(MINIGAME_MODE) && level != null) {
                level.OnEndOfFrame += delegate {
                    level.Teleport(GameData.Instance.minigame);
                    level.Session.Audio.Music.Event = GameData.GetMinigameMusic(GameData.Instance.minigame);
                };
            }
        }
    }
}
