﻿using Celeste;
using MadelineParty.Minigame;
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
            if (!MadelinePartyModule.SaveData.CharacterChoices.ContainsKey(GameData.Instance.realPlayerID)) {
                MadelinePartyModule.SaveData.CharacterChoices[GameData.Instance.realPlayerID] = 0;
            }
            MadelinePartyModule.SaveData.CharacterChoices[GameData.Instance.realPlayerID]++;

            level.OnEndOfFrame += delegate {
                GameData.Instance.currentPlayerSelection = null;
                level.Teleport(Mode.Equals(MINIGAME_MODE) ? "Game_MinigameHub" : "Game_SettingsConfig",
                    () => level.GetSpawnPoint(new Vector2(level.Bounds.Left, GameData.Instance.celesteNetHost ? level.Bounds.Top : level.Bounds.Bottom)));
            };
        }

        public void AfterPlayersRanked(Level level) {
            level.OnEndOfFrame += delegate {
                GameData.Instance.minigameResults.Clear();
                GameData.Instance.minigameStatus.Clear();
                level.Teleport(Mode.Equals(MINIGAME_MODE) ? "Game_MinigameHub" : GameData.Instance.board,
                    () => {
                        if(Mode.Equals(MINIGAME_MODE)) {
                            return level.GetSpawnPoint(new Vector2(level.Bounds.Left, GameData.Instance.celesteNetHost ? level.Bounds.Top : level.Bounds.Bottom));
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
            if (Engine.Scene is not Level level) return;
            if (Mode.Equals(MINIGAME_MODE)) {
                GameData.Instance.minigameStatus.Clear();
                level.Remove(level.Entities.FindAll<MinigameDisplay>());
                level.OnEndOfFrame += delegate {
                    level.Teleport(GameData.Instance.minigame, () => level.Session.LevelData.Spawns.Count >= 4 ? level.Session.LevelData.Spawns[GameData.Instance.realPlayerID] : level.GetSpawnPoint(new Vector2(level.Bounds.Left, level.Bounds.Top)));
                };
            } else if (Mode.Equals(BOARD_MODE)) {
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
}
