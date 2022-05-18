using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using MadelineParty.Multiplayer;
using MadelineParty.Multiplayer.General;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty {
    [Tracked(true)]
    public abstract class MinigameEntity : Trigger {
        protected static readonly Comparison<Tuple<int, uint>> HIGHEST_WINS = (x, y) => y.Item2.CompareTo(x.Item2);
        protected static readonly Comparison<Tuple<int, uint>> LOWEST_WINS = (x, y) => x.Item2.CompareTo(y.Item2);
        protected Level level;
        protected int displayNum = -1;
        protected List<MTexture> diceNumbers;
        public static bool didRespawn;
        public static bool started;
        public bool completed;
        public static float startTime = -1;

        protected MinigameEntity(EntityData data, Vector2 offset) : base(data, offset) {
            diceNumbers = GFX.Game.GetAtlasSubtextures("decals/madelineparty/dicenumbers/dice_");
            Visible = true;
            Depth = -99999;
            AddTag(Tags.PauseUpdate);
            AddTag(Tags.FrozenUpdate);
        }

        public static void Load() {
            MultiplayerSingleton.Instance.RegisterHandler<MinigameVector2>(HandleMinigameVector2);
        }

        public override void Render() {
            base.Render();
            if (displayNum > 0) {
                Player player = level.Tracker.GetEntity<Player>();
                if (player != null)
                    diceNumbers[displayNum - 1].Draw(player.Position + new Vector2(-24, -72));
            }
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            level = SceneAs<Level>();
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            // FIXME hackfix for invis
            if(!didRespawn) {
                didRespawn = true;
                startTime = level.RawTimeActive;
                Player player = level.Tracker.GetEntity<Player>();
                player.Die(Vector2.Zero, true, false);
                return;
            }
            if (!started) {
                Player player = level.Tracker.GetEntity<Player>();
                player.StateMachine.State = 11;
                // Stops the player from being moved by wind immediately
                // Probably saves you from Badeline too
                player.JustRespawned = true;
                startTime = level.RawTimeActive;
                started = true;
                Add(new Coroutine(Countdown()));
            }
        }

        private IEnumerator Countdown() {
            Player player = level.Tracker.GetEntity<Player>();
            player.StateMachine.State = 11;
            // Stops the player from being moved by wind immediately
            // Probably saves you from Badeline too
            player.JustRespawned = true;
            level.CanRetry = false;
            player.Speed = Vector2.Zero;
            yield return 1.2f;
            displayNum = 3;
            yield return 1f;
            displayNum = 2;
            yield return 1f;
            displayNum = 1;
            yield return 1f;
            displayNum = -1;
            player.StateMachine.State = 0;
            level.CanRetry = true;
            AfterStart();
        }

        protected virtual void AfterStart() {

        }

        public virtual void MultiplayerReceiveVector2(Vector2 vec, int extra) {

        }

        protected IEnumerator EndMinigame(Comparison<Tuple<int, uint>> placeOrderer, Action cleanup) {
            Player player = level.Tracker.GetEntity<Player>();
            // Wait until all players have finished
            while (GameData.minigameResults.Count < GameData.playerNumber) {
                if(player != null || (player = level.Tracker.GetEntity<Player>()) != null) {
                    // Freeze the player so they can't do anything else until everyone else is done
                    player.StateMachine.State = Player.StFrozen;
                    player.Speed = Vector2.Zero;
                }
                yield return null;
            }

            GameData.minigameResults.Sort(placeOrderer);

            List<int> winners = new() { GameData.minigameResults[0].Item1 };
            for (int i = 1; i < GameData.minigameResults.Count; i++) {
                if (GameData.minigameResults[i].Item2 == GameData.minigameResults[0].Item2) {
                    winners.Add(GameData.minigameResults[i].Item1);
                }
            }

            foreach (int winnerID in winners) {
                BoardController.QueueStrawberryChange(winnerID, 10);
            }

            // Winners share the top pedestal, everyone else is placed below that
            int realPlayerPlace = winners.Contains(GameData.realPlayerID) ? 0 : GameData.minigameResults.FindIndex((obj) => obj.Item1 == GameData.realPlayerID) - winners.Count + 2;
            // A check to stop the game from crashing when I hit one of these while testing
            if (winners[0] >= 0 && GameData.players[winners[0]] != null) {
                cleanup();
                level.OnEndOfFrame += delegate {
                    level.Remove(player);
                    level.UnloadLevel();

                    level.Session.Level = "Game_PlayerRanking";
                    List<Vector2> spawns = new List<Vector2>(level.Session.LevelData.Spawns.ToArray());
                    // Sort the spawns so the highest ones are first
                    spawns.Sort((x, y) => { return x.Y.CompareTo(y.Y); });
                    level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(spawns[realPlayerPlace].X, spawns[realPlayerPlace].Y));

                    level.LoadLevel(Player.IntroTypes.None);
                };
            }
        }

        private static void HandleMinigameVector2(MPData data) {
            if (data is not MinigameVector2 vector2) return;
            // If another player in our party is sending out minigame vector2 data
            if (GameData.celestenetIDs.Contains(vector2.ID) && vector2.ID != MultiplayerSingleton.Instance.GetPlayerID()) {
                MinigameEntity mge;
                if ((mge = Engine.Scene?.Tracker.GetEntity<MinigameEntity>()) != null) {
                    mge.MultiplayerReceiveVector2(vector2.vec, vector2.extra);
                }
            }
        }
    }
}
