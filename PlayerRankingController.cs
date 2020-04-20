using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using MadelineParty.Ghostnet;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty
{
    public class PlayerRankingController : Entity, IPauseUpdateGhostnetChat
    {
        private Level level;

        public PlayerRankingController()
        {
            AddTag(Tags.PauseUpdate);
            AddTag(Tags.FrozenUpdate);
            Add(new Coroutine(SendBack()));
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            level = SceneAs<Level>();
        }

        private void EndGame()
        {
            List<PlayerData> playerList = new List<PlayerData>(Array.FindAll<PlayerData>(GameData.players, (x) => x != null));
            // TODO Tiebreaker die roll
            playerList.Sort((x, y) => { if (x.hearts == y.hearts)
                {
                    return y.strawberries.CompareTo(x.strawberries);
                }
                return y.hearts.CompareTo(x.hearts);
            });

            int winnerID = playerList[0].TokenSelected;
            int realPlayerPlace = playerList.FindIndex((obj) => obj.TokenSelected == GameData.realPlayerID);
            level.OnEndOfFrame += delegate
            {
                Player player = level.Tracker.GetEntity<Player>();
                Leader.StoreStrawberries(player.Leader);
                level.Remove(player);
                level.UnloadLevel();

                level.Session.Level = "Game_VictoryRoyale";

                List<Vector2> spawns = new List<Vector2>(level.Session.LevelData.Spawns.ToArray());
                // Sort the spawns so the highest one is first
                spawns.Sort((x, y) => { return x.Y.CompareTo(y.Y); });
                level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(spawns[realPlayerPlace].X, spawns[realPlayerPlace].Y));

                level.LoadLevel(Player.IntroTypes.Jump);

                Leader.RestoreStrawberries(player.Leader);
            };
        }

        private IEnumerator SendBack()
        {
            yield return 7f;
            GameData.minigame = null;
            if (GameData.turn > GameData.maxTurns)
            {
                EndGame();
            }
            else
            {
                level.OnEndOfFrame += delegate
                {
                    Player player = level.Tracker.GetEntity<Player>();
                    Leader.StoreStrawberries(player.Leader);
                    level.Remove(player);
                    level.UnloadLevel();

                    level.Session.Level = "Game_MainRoom";
                    switch (GameData.realPlayerID)
                    {
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
