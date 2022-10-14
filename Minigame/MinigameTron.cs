using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BrokemiaHelper;
using Celeste;
using Celeste.Mod.Entities;
using MadelineParty.Multiplayer;
using MadelineParty.Multiplayer.General;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace MadelineParty {
    // Try to dodge opponents trails in a feather
    [CustomEntity("madelineparty/minigameTron")]
    public class MinigameTron : MinigameEntity {
        private static FieldInfo diedInGBJInfo = typeof(Player).GetField("diedInGBJ", BindingFlags.Static | BindingFlags.NonPublic);
        protected Vector2 deadRespawn;
        // Note: Brokemia Helper handles the trail for the real player
        private static List<Vector2>[] trails = new List<Vector2>[4];
        private Player player;
        
        public MinigameTron(EntityData data, Vector2 offset) : base(data, offset) {
            deadRespawn = data.NodesOffset(offset)[0];
        }

        public static new void Load() {
            TronState.OnAddTrailPt += OnAddTrailPt;
        }

        public static void Unload() {
            TronState.OnAddTrailPt -= OnAddTrailPt;
        }

        private static void OnAddTrailPt(Vector2 pt) {
            MultiplayerSingleton.Instance.Send(new MinigameVector2 { vec = pt, extra = GameData.Instance.realPlayerID });
        }

        public override void Added(Scene scene) {
            base.Added(scene);
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            // If we have just died
            if (level.Session.RespawnPoint == deadRespawn) {
                completed = true;
                MinigameTimeDisplay display = level.Entities.FindFirst<MinigameTimeDisplay>();
                if (display != null)
                    display.finalTime = level.RawTimeActive - startTime;
                float timeElapsed = (level.RawTimeActive - startTime) * 10000;
                startTime = -1;
                started = false;
                didRespawn = false;
                level.CanRetry = false;
                GameData.Instance.minigameResults.Add(new Tuple<int, uint>(GameData.Instance.realPlayerID, (uint)timeElapsed));
                MultiplayerSingleton.Instance.Send(new MinigameEnd { results = (uint)timeElapsed });
                
                Add(new Coroutine(EndMinigame(HIGHEST_WINS, () => { })));
            } else {
                // Don't prevent pausing if we're still on the ready screen
                if (started) {
                    level.PauseLock = true;
                    diedInGBJInfo.SetValue(null, 0);
                    for (int i = 0; i < 4; i++) {
                        if (i != GameData.Instance.realPlayerID && GameData.Instance.players[i] != null) {
                            trails[i] = new();
                        } else {
                            trails[i] = null;
                        }
                    }
                }
            }
        }

        protected override void AfterStart() {
            base.AfterStart();
            // Reset timer so it starts at 0 instead of 4.2
            startTime = level.RawTimeActive;
            player = level.Tracker.GetEntity<Player>();
            player.JustRespawned = false;
            // TODO fix this when I refactor characters
            var pData = DynamicData.For(player);
            pData.Set("BrokemiaHelperTronHairColor", PlayerToken.colors[PlayerToken.GetFullPath(BoardController.TokenPaths[GameData.Instance.realPlayerID])]);
            pData.Set("BrokemiaHelperTronTargetSpeed", (float?)130f);
            pData.Set("BrokemiaHelperTronMaxSpeed", (float?)160f);
            TronState.StartTron(player);
            level.Session.RespawnPoint = deadRespawn;
            level.Add(new MinigameTimeDisplay(this));
        }

        public override void Update() {
            base.Update();
            if (!started) return;
            player = level.Tracker.GetEntity<Player>();
            if (player != null) {
                for (int i = 0; i < trails.Length; i++) {
                    if (trails[i] != null) {
                        foreach (Vector2 pt in trails[i]) {
                            if ((pt - player.Center).LengthSquared() < TronState.pointSpacingSq * Engine.DeltaTime * Engine.DeltaTime) {
                                player.Die(Vector2.Zero);
                            }
                        }
                    }
                }
                if(GameData.Instance.playerNumber > 1 && GameData.Instance.minigameResults.Count == GameData.Instance.playerNumber - 1 && !GameData.Instance.minigameResults.Any(t => t.Item1 == GameData.Instance.realPlayerID)) {
                    float timeElapsed = (level.RawTimeActive - startTime + 2) * 10000; // Add 2 seconds just to be sure, since this player is obviously the winner
                    MultiplayerSingleton.Instance.Send(new MinigameEnd { results = (uint)timeElapsed });

                    Add(new Coroutine(EndMinigame(HIGHEST_WINS, () => { })));
                }
            } else {
                trails[GameData.Instance.realPlayerID] = TronState.trail;
            }
        }

        public override void MultiplayerReceiveVector2(Vector2 vec, int extra) {
            base.MultiplayerReceiveVector2(vec, extra);
            trails[extra].Add(vec);
        }

        public override void Render() {
            base.Render();
            if (!started) return;
            for (int i = 0; i < 4; i++) {
                if (trails[i] != null) {
                    Color color = PlayerToken.colors[PlayerToken.GetFullPath(BoardController.TokenPaths[i])];
                    for (int j = 0; j < trails[i].Count - 1; j++) {
                        Draw.Line(trails[i][j], trails[i][j + 1], color, 3);
                    }
                }
            }
        }
    }
}
