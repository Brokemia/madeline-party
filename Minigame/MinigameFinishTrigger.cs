using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty {
    public class MinigameFinishTrigger : MinigameEntity {

        public MinigameTimeDisplay timer;

        public MinigameFinishTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        }

        protected override void AfterStart() {
            base.AfterStart();
            // Reset timer so it starts at 0 instead of 4.2
            startTime = level.RawTimeActive;
            level.Add(timer = new MinigameTimeDisplay(this));
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);
            // Stop problems with the player entering the trigger multiple times
            if (GameData.minigameResults.Exists((obj) => obj.Item1 == GameData.realPlayerID))
                return;
            completed = true;
            level.Entities.FindFirst<MinigameTimeDisplay>().finalTime = level.RawTimeActive - startTime;
            float timeElapsed = (level.RawTimeActive - startTime) * 10000;
            startTime = -1;
            started = false;
            didRespawn = false;
            level.CanRetry = false;
            foreach(SyncedKevin kevin in level.Tracker.GetEntities<SyncedKevin>()) {
                kevin.deactivated = true;
            }
            GameData.minigameResults.Add(new Tuple<int, uint>(GameData.realPlayerID, (uint)timeElapsed));
            if (MadelinePartyModule.IsCelesteNetInstalled()) {
                CelesteNetSendMinigameResults((uint)timeElapsed);
            }

            Add(new Coroutine(WaitForMinigameFinish(player)));
        }

        private IEnumerator WaitForMinigameFinish(Player player) {
            // Wait until all players have finished
            while (GameData.minigameResults.Count < GameData.playerNumber) {
                yield return null;
            }

            GameData.minigameResults.Sort((x, y) => { return x.Item2.CompareTo(y.Item2); });

            int winnerID = GameData.minigameResults[0].Item1;
            int realPlayerPlace = GameData.minigameResults.FindIndex((obj) => obj.Item1 == GameData.realPlayerID);
            // A check to stop the game from crashing when I hit one of these while testing
            if (winnerID >= 0 && GameData.players[winnerID] != null) {
                // TODO animate this change in strawberries, maybe just move it so it happens immediately after the second teleport
                GameData.players[winnerID].ChangeStrawberries(10);
                level.OnEndOfFrame += delegate {
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
