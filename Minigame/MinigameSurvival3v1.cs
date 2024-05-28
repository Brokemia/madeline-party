using System;
using System.Collections;
using Celeste;
using Celeste.Mod;
using Celeste.Mod.Entities;
using MadelineParty.Multiplayer;
using MadelineParty.Multiplayer.General;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty.Minigame {
    [CustomEntity("madelineparty/minigameSurvival3v1")]
    public class MinigameSurvival3v1 : MinigameEntity {
        private const uint KILLER_WIN = 3;
        private const uint KILLER_LOSE = 0;
        private const uint SURVIVOR_WIN = 2;
        private const uint SURVIVOR_LOSE = 1;

        private const string SOLO_ROLE = "3v1 - Solo";
        private const string TEAM_ROLE = "3v1 - Team";

        protected Vector2 deadRespawn;
        public Coroutine endCoroutine;

        public MinigameSurvival3v1(EntityData data, Vector2 offset) : base(data, offset) {
            deadRespawn = data.NodesOffset(offset)[0];
        }

        public override void Added(Scene scene) {
            base.Added(scene);
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            // If we have just died
            if (level.Session.RespawnPoint == deadRespawn) {
                if (level.Entities.FindFirst<MinigameTimeDisplay>() is { } display) {
                    display.finalTime = level.RawTimeActive - Data.StartTime;
                }

                Add(endCoroutine = new Coroutine(FinishMinigame(false)));
            } else {
                // Don't prevent pausing if we're still on the ready screen
                if (Data.Started) {
                    level.PauseLock = true;
                    Player.diedInGBJ = 0;
                } else {
                    // Assign roles
                    
                    var killer = Calc.Random.Next(GameData.Instance.playerNumber);
                    // Account for gaps in players
                    for (int i = 0; i <= killer; i++) {
                        if (GameData.Instance.players[i] == null) {
                            killer++;
                        }
                    }
                    for (int i = 0; i < GameData.Instance.players.Length; i++) {
                        if (i == killer) {
                            Data.AssignRole(i, SOLO_ROLE);
                        } else {
                            Data.AssignRole(i, TEAM_ROLE);
                        }
                    }
                }
            }
        }

        protected override void AfterStart() {
            base.AfterStart();
            // Reset timer so it starts at 0 instead of 4.2
            level.Tracker.GetEntity<Player>().JustRespawned = false;
            level.Session.RespawnPoint = deadRespawn;
            level.Add(new MinigameTimeDisplay(this, true));
        }

        public override void Update() {
            base.Update();
            if (!Data.Started) return;
            if (endCoroutine != null) return;

            if (Data.HasRole(GameData.Instance.realPlayerID, SOLO_ROLE)) {
                var killedPlayersCount = 0;
                foreach (var results in GameData.Instance.minigameResults) {
                    if (results.Item2 == SURVIVOR_LOSE) {
                        killedPlayersCount++;
                    }
                }
                if (killedPlayersCount >= GameData.Instance.playerNumber - 1) {
                    Add(endCoroutine = new Coroutine(FinishMinigame(false)));
                    return;
                }
            }

            if (level.RawTimeActive - Data.StartTime >= 30) {
                Add(endCoroutine = new Coroutine(FinishMinigame(true)));
            }
        }

        protected IEnumerator FinishMinigame(bool timeout) {
            Player player = level.Tracker.GetEntity<Player>();
            // This check is probably unnecessary, but I left it in for safety
            while (player == null) {
                yield return null;
                player = level.Tracker.GetEntity<Player>();
            }
            completed = true;
            // Freeze the player so they can't do any more moving until everyone else is done
            // TODO make sure surviving players are not killed by stuff
            player.StateMachine.State = Player.StFrozen;
            player.Speed = Vector2.Zero;
            level.CanRetry = false;
            uint res;
            if (timeout) {
                res = Data.HasRole(GameData.Instance.realPlayerID, SOLO_ROLE) ? KILLER_LOSE : SURVIVOR_WIN;
            } else {
                res = Data.HasRole(GameData.Instance.realPlayerID, SOLO_ROLE) ? KILLER_WIN : SURVIVOR_LOSE;
            }
            GameData.Instance.minigameResults.Add(new Tuple<int, uint>(GameData.Instance.realPlayerID, res));
            MultiplayerSingleton.Instance.Send(new MinigameEnd { results = res });

            yield return new SwapImmediately(EndMinigame(HIGHEST_WINS, null));
        }
    }
}
