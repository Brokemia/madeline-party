using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty
{
    // Move an infinite number of Theos from one place to another
    public class MinigameTheoMover : MinigameEntity
    {
        protected Vector2 theoRespawnPoint;
        public static uint theoCount;
        public Coroutine endCoroutine;
        public MinigameTheoMover(EntityData data, Vector2 offset) : base(data, offset)
        {
            theoRespawnPoint = data.Nodes[0];
            Add(new HoldableCollider(new Action<Holdable>(OnHoldable), null));
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            level.Add(new TheoCrystal(theoRespawnPoint));
        }

        protected override void AfterStart()
        {
            base.AfterStart();
            // Reset timer so it starts at 30 instead of (30 - the time it takes to count down)
            startTime = level.RawTimeActive;
        }

        public override void Update()
        {
            base.Update();
            if (level.RawTimeActive - startTime >= 30 && endCoroutine == null)
            {
                Add(endCoroutine = new Coroutine(EndMinigame()));
            }
        }

        protected IEnumerator EndMinigame()
        {
            Player player = level.Tracker.GetEntity<Player>();
            // This check is probably unnecessary, but I left it in for safety
            while (player == null)
            {
                yield return null;
                player = level.Tracker.GetEntity<Player>();
            }
            // Freeze the player so they can't do any more Theo throwing until everyone else is done
            player.StateMachine.State = Player.StFrozen;
            player.Speed = Vector2.Zero;
            startTime = -1;
            started = false;
            level.CanRetry = false;
            Console.WriteLine("Theo Count: " + theoCount);
            GameData.minigameResults.Add(new Tuple<int, uint>(GameData.realPlayerID, theoCount));
            if (MadelinePartyModule.ghostnetConnected)
            {
                GhostNetSendMinigameResults(theoCount);
            }

            // Wait until all players have finished
            while (GameData.minigameResults.Count < GameData.playerNumber)
            {
                yield return null;
            }

            GameData.minigameResults.Sort((x, y) => { return y.Item2.CompareTo(x.Item2); });

            int winnerID = GameData.minigameResults[0].Item1;
            int realPlayerPlace = GameData.minigameResults.FindIndex((obj) => obj.Item1 == GameData.realPlayerID);
            // A check to stop the game from crashing when I hit one of these while testing
            if (winnerID >= 0 && GameData.players[winnerID] != null)
            {
                theoCount = 0;
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

        private void OnHoldable(Holdable h)
        {
            if (h.Entity is TheoCrystal)
            {
                TheoCrystal theoCrystal = h.Entity as TheoCrystal;
                theoCrystal.RemoveSelf();
                theoCount++;
                level.Add(new TheoCrystal(theoRespawnPoint));
            }
        }
    }
}
