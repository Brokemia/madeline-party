using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BrokemiaHelper;
using Celeste;
using FactoryHelper.Components;
using MadelineParty.Minigame;
using MadelineParty.Minigame.Display;
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
        public bool completed;
        public MinigamePersistentData Data { get; private set; }

        protected T DataAs<T>() where T : MinigamePersistentData {
            return Data as T;
        }

        protected virtual MinigamePersistentData NewData() {
            return new MinigamePersistentData();
        }

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
            if ((Data = level.Tracker.GetEntity<MinigamePersistentData>()) == null) {
                level.Add(Data = NewData());
            }
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            if (!Data.Started) {
                level.Add(new MinigameReadyPrompt(this));
                Player player = level.Tracker.GetEntity<Player>();
                player.Position = level.GetMinigameSpawnPoint(Data, GameData.Instance.RealPlayer.TokenSelected);
                player.JustRespawned = true;
                player.StateMachine.State = Player.StFrozen;
                player.Get<ConveyorMover>().Active = false;
            }
            //// FIXME hackfix for invis
            //if (!didRespawn) {
            //    didRespawn = true;
            //    //startTime = level.RawTimeActive;
            //    //Player player = level.Tracker.GetEntity<Player>();
            //    //player.Die(Vector2.Zero, true, false);
            //    level.Add(new MinigameReadyPrompt());
            //    Player player = level.Tracker.GetEntity<Player>();
            //    player.JustRespawned = true;
            //    player.StateMachine.State = Player.StFrozen;
            //    return;
            //}
            //if (!started) {
            //    Player player = level.Tracker.GetEntity<Player>();
            //    player.StateMachine.State = Player.StFrozen;
            //    // Stops the player from being moved by wind immediately
            //    // Probably saves you from Badeline too
            //    player.JustRespawned = true;
            //    startTime = level.RawTimeActive;
            //    started = true;
            //    Add(new Coroutine(Countdown()));
            //}
        }

        public IEnumerator Countdown() {
            Player player = level.Tracker.GetEntity<Player>();
            level.Session.Audio.Music.Event = GameData.GetMinigameMusic(GameData.Instance.minigame);
            level.Session.Audio.Apply();
            player.StateMachine.State = Player.StFrozen;
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
            // Reset timer so it doesn't include the countdown
            Data.StartTime = level.RawTimeActive;
            Data.Started = true;
            player.Get<ConveyorMover>().Active = true;
            AfterStart();
        }

        protected virtual void AfterStart() {

        }

        public virtual void MultiplayerReceiveVector2(Vector2 vec, int extra) {

        }

        protected IEnumerator EndMinigame(Comparison<Tuple<int, uint>> placeOrderer, Action cleanup) {
            Player player = level.Tracker.GetEntity<Player>();
            Data.RemoveSelf();
            Data.Started = false;
            // A little extra gap so you aren't teleported quite as suddenly
            float extraTime = 1;
            while (extraTime > 0) {
                if(player != null || (player = level.Tracker.GetEntity<Player>()) != null) {
                    // Freeze the player so they can't do anything else until everyone else is done
                    player.StateMachine.State = Player.StFrozen;
                    player.ForceCameraUpdate = true;
                    player.Speed = Vector2.Zero;
                }
                // Wait until all players have finished
                if (GameData.Instance.minigameResults.Count >= GameData.Instance.playerNumber) {
                    extraTime -= Engine.DeltaTime;
                }
                yield return null;
            }

            GameData.Instance.minigameResults.Sort(placeOrderer);

            List<int> winners = [ GameData.Instance.minigameResults[0].Item1 ];
            for (int i = 1; i < GameData.Instance.minigameResults.Count; i++) {
                if (GameData.Instance.minigameResults[i].Item2 == GameData.Instance.minigameResults[0].Item2) {
                    winners.Add(GameData.Instance.minigameResults[i].Item1);
                }
            }

            MadelinePartyModule.SaveData.MinigamesPlayed++;

            if(winners.Contains(GameData.Instance.realPlayerID)) {
                MadelinePartyModule.SaveData.MinigamesWon++;
            }

            ModeManager.Instance.DistributeMinigameRewards(winners);

            // Winners share the top pedestal, everyone else is placed below that
            int realPlayerPlace = winners.Contains(GameData.Instance.realPlayerID) ? 0 : GameData.Instance.minigameResults.FindIndex((obj) => obj.Item1 == GameData.Instance.realPlayerID) - winners.Count + 1;
            // A check to stop the game from crashing when I hit one of these while testing
            if (winners[0] >= 0 && GameData.Instance.players[winners[0]] != null) {
                cleanup?.Invoke();
                // TODO this is a reward and can go in mode manager DistributeMinigameRewards
                if (!GameData.Instance.minigameWins.ContainsKey(winners[0])) {
                    GameData.Instance.minigameWins[winners[0]] = 1;
                } else {
                    GameData.Instance.minigameWins[winners[0]]++;
                }
                level.OnEndOfFrame += delegate {
                    level.Remove(player);
                    level.UnloadLevel();

                    var rankingLevel = "Game_PlayerRanking";
                    level.Session.Level = rankingLevel;
                    List<Vector2> spawns = new List<Vector2>(level.Session.LevelData.Spawns);
                    // Sort the spawns so the highest ones are first
                    spawns.Sort((x, y) => { return x.Y.CompareTo(y.Y); });
                    //Console.WriteLine(spawns + " " + realPlayerPlace + " " + GameData.Instance.minigameResults.FindIndex((obj) => obj.Item1 == GameData.Instance.realPlayerID));
                    level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(spawns[realPlayerPlace].X, spawns[realPlayerPlace].Y));

                    level.LoadLevel(Player.IntroTypes.None);

                    if(level.Wipe != null) {
                        Action onComplete = level.Wipe.OnComplete;
                        level.Wipe.OnComplete = delegate {
                            // If the level was paused enough that the wipe didn't finish in the ranking room, don't show the textbox
                            if (level.Session.Level == rankingLevel) {
                                level.Add(new PersistentMiniTextbox(GetWinnerText(winners), pauseUpdate: true));
                            }
                            onComplete?.Invoke();
                        };
                    } else {
                        level.Add(new PersistentMiniTextbox(GetWinnerText(winners), pauseUpdate: true));
                    }
                };
            }
        }

        // Turn several names into a readable list
        private string CombineNames(List<string> names) {
            if (names.Count == 1) {
                return names[0];
            } else if (names.Count == 2) {
                return $"{names[0]} and {names[1]}";
            }

            string res = "";
            for(int i = 0; i < names.Count - 1; i++) {
                res += names[i];
                if(i == names.Count - 2) {
                    res += ", and ";
                } else {
                    res += ", ";
                }
            }
            res += names[names.Count - 1];
            return res;
        }

        // TODO add special text for a win streak
        // X is dominating, X is unstoppable, etc
        private string GetWinnerText(List<int> winners) {
            // First, set the name to use as a dialog entry
            Dialog.Language.Dialog["MadelineParty_Winner_ID_Name"] = CombineNames(winners.ConvertAll(i => GameData.Instance.GetPlayerName(i)));
            return GameData.Instance.GetRandomDialogID(winners.Count > 1 ? "MadelineParty_Minigame_Winners_List" : "MadelineParty_Minigame_Winner_List");
        }

        private static void HandleMinigameVector2(MPData data) {
            if (data is not MinigameVector2 vector2) return;
            // If another player in our party is sending out minigame vector2 data
            if (GameData.Instance.celestenetIDs.Contains(vector2.ID) && vector2.ID != MultiplayerSingleton.Instance.CurrentPlayerID()) {
                MinigameEntity mge;
                if ((mge = Engine.Scene?.Tracker.GetEntity<MinigameEntity>()) != null) {
                    mge.MultiplayerReceiveVector2(vector2.vec, vector2.extra);
                }
            }
        }
    }
}
