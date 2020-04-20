using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod.Ghost.Net;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty
{
    public class MinigameFinishTrigger : MinigameEntity
    {

        public MinigameFinishTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
        }

        public override void OnEnter(Player player)
        {
            base.OnEnter(player);
            // Stop problems with the player entering the trigger multiple times
            if (GameData.minigameResults.Exists((obj) => obj.Item1 == GameData.realPlayerID))
                return;
            float timeElapsed = (level.RawTimeActive - startTime) * 10000;
            startTime = -1;
            started = false;
            level.CanRetry = false;
            GameData.minigameResults.Add(new Tuple<int, uint>(GameData.realPlayerID, (uint)timeElapsed));
            if (MadelinePartyModule.ghostnetConnected)
            {
                GhostNetSendMinigameResults((uint)timeElapsed);
            }

            Add(new Coroutine(WaitForMinigameFinish(player)));
        }

        private IEnumerator WaitForMinigameFinish(Player player)
        {
            // Wait until all players have finished
            while (GameData.minigameResults.Count < GameData.playerNumber)
            {
                yield return null;
            }

            GameData.minigameResults.Sort((x, y) => { return x.Item2.CompareTo(y.Item2); });

            int winnerID = GameData.minigameResults[0].Item1;
            int realPlayerPlace = GameData.minigameResults.FindIndex((obj) => obj.Item1 == GameData.realPlayerID);
            // A check to stop the game from crashing when I hit one of these while testing
            if (winnerID >= 0 && GameData.players[winnerID] != null)
            {

                GameData.players[winnerID].ChangeStrawberries(10);
                level.OnEndOfFrame += delegate
                {
                    Leader.StoreStrawberries(player.Leader);
                    level.Remove(player);
                    level.UnloadLevel();

                    level.Session.Level = "Game_PlayerRanking";
                    GameData.minigameResults.Clear();
                    List<Vector2> spawns = new List<Vector2>(level.Session.LevelData.Spawns.ToArray());
                    // Sort the spawns so the highest ones are first
                    spawns.Sort((x, y) => { return x.Y.CompareTo(y.Y); });
                    level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(spawns[realPlayerPlace].X, spawns[realPlayerPlace].Y));

                    level.LoadLevel(Player.IntroTypes.None);

                    Leader.RestoreStrawberries(player.Leader);
                };
            }
        }
    }
}
