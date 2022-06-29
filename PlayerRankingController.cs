﻿using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty {
    public class PlayerRankingController : Entity {
        private Level level;

        public PlayerRankingController() {
            AddTag(Tags.PauseUpdate);
            AddTag(Tags.FrozenUpdate);
            Add(new Coroutine(SendBack()));
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            level = SceneAs<Level>();
        }

        private void EndGame() {
            List<PlayerData> playerList = new List<PlayerData>(Array.FindAll(GameData.Instance.players, (x) => x != null));
            // TODO Tiebreaker die roll
            playerList.Sort((x, y) => {
                if (x.hearts == y.hearts) {
                    return y.strawberries.CompareTo(x.strawberries);
                }
                return y.hearts.CompareTo(x.hearts);
            });

            int tieCount = 1;
            for(int i = 1; i < playerList.Count; i++) {
                if(playerList[i].hearts == playerList[0].hearts && playerList[i].strawberries == playerList[0].strawberries) {
                    tieCount++;
                }
            }

            if (tieCount > 1) {
                int realPlayerPlace = playerList.FindIndex((obj) => obj.TokenSelected == GameData.Instance.realPlayerID);
                level.OnEndOfFrame += delegate {
                    Player player = level.Tracker.GetEntity<Player>();
                    Leader.StoreStrawberries(player.Leader);
                    level.Remove(player);
                    level.UnloadLevel();

                    level.Session.Level = "Game_Tiebreaker";
                    GameData.Instance.minigameResults.Clear();
                    GameData.Instance.minigameStatus.Clear();

                    List<Vector2> spawns = new List<Vector2>(level.Session.LevelData.Spawns.ToArray());
                    // Sort the spawns so the highest one is first
                    spawns.Sort((x, y) => { return x.Y.CompareTo(y.Y); });
                    if (realPlayerPlace < tieCount) {
                        level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(spawns[realPlayerPlace].X, spawns[realPlayerPlace].Y));
                    } else {
                        level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(spawns[spawns.Count - 1].X, spawns[spawns.Count - 1].Y));
                    }

                    level.LoadLevel(Player.IntroTypes.None);
                    playerList.Add(playerList[0]);
                    level.Entities.FindFirst<TiebreakerController>().Initialize(tieCount, playerList);

                    Leader.RestoreStrawberries(player.Leader);
                };
            } else {
                int winnerID = playerList[0].TokenSelected;
                int realPlayerPlace = playerList.FindIndex((obj) => obj.TokenSelected == GameData.Instance.realPlayerID);
                level.OnEndOfFrame += delegate {
                    Player player = level.Tracker.GetEntity<Player>();
                    Leader.StoreStrawberries(player.Leader);
                    level.Remove(player);
                    level.UnloadLevel();

                    level.Session.Level = "Game_VictoryRoyale";
                    GameData.Instance.minigameResults.Clear();
                    GameData.Instance.minigameStatus.Clear();

                    List<Vector2> spawns = new List<Vector2>(level.Session.LevelData.Spawns.ToArray());
                    // Sort the spawns so the highest one is first
                    spawns.Sort((x, y) => { return x.Y.CompareTo(y.Y); });
                    level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(spawns[realPlayerPlace].X, spawns[realPlayerPlace].Y));

                    level.LoadLevel(Player.IntroTypes.Jump);

                    level.Tracker.GetEntity<GameEndController>().winnerID = winnerID;

                    Leader.RestoreStrawberries(player.Leader);
                };
            }
        }

        private IEnumerator SendBack() {
            yield return 7f;
            level.Remove(level.Entities.FindAll<MinigameDisplay>());
            GameData.Instance.minigame = null;
            if (GameData.Instance.turn > GameData.Instance.maxTurns) {
                EndGame();
            } else {
                level.OnEndOfFrame += delegate {
                    Player player = level.Tracker.GetEntity<Player>();
                    Leader.StoreStrawberries(player.Leader);
                    level.Remove(player);
                    level.UnloadLevel();

                    level.Session.Level = "Game_MainRoom";
                    GameData.Instance.minigameResults.Clear();
                    GameData.Instance.minigameStatus.Clear();
                    switch (GameData.Instance.realPlayerID) {
                        case 0:
                            level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(level.Bounds.Left, level.Bounds.Top));
                            break;
                        case 1:
                            level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(level.Bounds.Right, level.Bounds.Top));
                            break;
                        case 2:
                            level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(level.Bounds.Left, level.Bounds.Bottom));
                            break;
                        case 3:
                            level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(level.Bounds.Right, level.Bounds.Bottom));
                            break;
                    }
                    level.LoadLevel(Player.IntroTypes.None);

                    Leader.RestoreStrawberries(player.Leader);
                };
            }

        }
    }

}
