using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BrokemiaHelper;
using Celeste;
using Celeste.Mod.Entities;
using MadelineParty.Board;
using MadelineParty.Minigame;
using MadelineParty.Multiplayer;
using MadelineParty.Multiplayer.General;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace MadelineParty
{
    // Try to dodge opponents trails in a feather
    [CustomEntity("madelineparty/minigameTron")]
    public class MinigameTron : MinigameEntity {
        public class TronMinigamePersistentData : MinigamePersistentData {
            // Note: Brokemia Helper handles the trail for the real player
            public List<Vector2>[] Trails { get; private set; } = new List<Vector2>[4];
        }


        protected Vector2 deadRespawn;
        private Player player;
        private TronMinigamePersistentData tronData;
        
        public MinigameTron(EntityData data, Vector2 offset) : base(data, offset) {
            deadRespawn = data.NodesOffset(offset)[0];
        }

        protected override MinigamePersistentData NewData() {
            return new TronMinigamePersistentData();
        }

        private static void OnAddTrailPt(Vector2 pt) {
            MultiplayerSingleton.Instance.Send(new MinigameVector2 { vec = pt, extra = GameData.Instance.realPlayerID });
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            tronData = DataAs<TronMinigamePersistentData>();
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            // If we have just died
            if (level.Session.RespawnPoint == deadRespawn) {
                completed = true;
                MinigameTimeDisplay display = level.Entities.FindFirst<MinigameTimeDisplay>();
                if (display != null)
                    display.finalTime = level.RawTimeActive - Data.StartTime;
                float timeElapsed = (level.RawTimeActive - Data.StartTime) * 10000;
                level.CanRetry = false;
                GameData.Instance.minigameResults.Add(new Tuple<int, uint>(GameData.Instance.realPlayerID, (uint)timeElapsed));
                MultiplayerSingleton.Instance.Send(new MinigameEnd { results = (uint)timeElapsed });
                
                Add(new Coroutine(EndMinigame(HIGHEST_WINS, () => { })));
            } else {
                // Don't prevent pausing if we're still on the ready screen
                //if (started) {
                level.PauseLock = true;
                Player.diedInGBJ = 0;
                lock (tronData.Trails) {
                    for (int i = 0; i < 4; i++) {
                        if (i != GameData.Instance.realPlayerID && GameData.Instance.players[i] != null) {
                            tronData.Trails[i] = new();
                        } else {
                            tronData.Trails[i] = null;
                        }
                    }
                }
                //}
            }
        }

        protected override void AfterStart() {
            base.AfterStart();
            player = level.Tracker.GetEntity<Player>();
            player.JustRespawned = false;
            
            var state = player.Get<TronState>();
            // TODO fix this when I refactor characters
            state.HairColor = PlayerToken.colors[PlayerToken.GetFullPath(BoardController.TokenPaths[GameData.Instance.realPlayerID])];
            state.TargetSpeed = 110;
            state.MaxSpeed = 140;
            state.OnAddTrailPt += OnAddTrailPt;
            state.StartTron();
            
            level.Session.RespawnPoint = deadRespawn;
            level.Add(new MinigameTimeDisplay(this));
        }

        public override void Update() {
            base.Update();
            if (!Data.Started) return;
            player = level.Tracker.GetEntity<Player>();
            if (player != null) {
                lock (tronData.Trails) {
                    for (int i = 0; i < tronData.Trails.Length; i++) {
                        if (tronData.Trails[i] != null) {
                            foreach (Vector2 pt in tronData.Trails[i]) {
                                if ((pt - player.Center).LengthSquared() < TronState.pointSpacingSq * Engine.DeltaTime * Engine.DeltaTime) {
                                    player.Die(Vector2.Zero);
                                }
                            }
                        }
                    }
                }
                if(GameData.Instance.playerNumber > 1 && GameData.Instance.minigameResults.Count == GameData.Instance.playerNumber - 1 && !GameData.Instance.minigameResults.Any(t => t.Item1 == GameData.Instance.realPlayerID)) {
                    float timeElapsed = (level.RawTimeActive - Data.StartTime + 2) * 10000; // Add 2 seconds just to be sure, since this player is obviously the winner
                    MultiplayerSingleton.Instance.Send(new MinigameEnd { results = (uint)timeElapsed });

                    Add(new Coroutine(EndMinigame(HIGHEST_WINS, null)));
                }
            } else {
                lock (tronData.Trails) {
                    // TODO don't break with quick respawn mods?
                    tronData.Trails[GameData.Instance.realPlayerID] = level.Entities.FindFirst<PlayerDeadBody>().Get<TronState>().trail;
                }
            }
        }

        public override void MultiplayerReceiveVector2(Vector2 vec, int extra) {
            base.MultiplayerReceiveVector2(vec, extra);
            lock (tronData.Trails) {
                tronData.Trails[extra].Add(vec);
            }
        }

        public override void Render() {
            base.Render();
            //if (!started) return;
            lock (tronData.Trails) {
                for (int i = 0; i < 4; i++) {
                    if (tronData.Trails[i] != null) {
                        Color color = PlayerToken.colors[PlayerToken.GetFullPath(BoardController.TokenPaths[i])];
                        for (int j = 0; j < tronData.Trails[i].Count - 1; j++) {
                            Draw.Line(tronData.Trails[i][j], tronData.Trails[i][j + 1], color, 3);
                        }
                    }
                }
            }
        }
    }
}
