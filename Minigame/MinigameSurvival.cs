using System;
using System.Collections.Generic;
using System.Reflection;
using Celeste;
using Celeste.Mod.Core;
using Celeste.Mod.Entities;
using MadelineParty.Multiplayer;
using MadelineParty.Multiplayer.General;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using VivHelper.Entities;

namespace MadelineParty {
    // Survive an increasing number of seekers for as long as possible
    [CustomEntity("madelineparty/minigameSurvival")]
    public class MinigameSurvival : MinigameEntity {
        private static FieldInfo diedInGBJInfo = typeof(Player).GetField("diedInGBJ", BindingFlags.Static | BindingFlags.NonPublic);
        private List<Vector2> seekerSpawns = new();
        private Random rand;
        private float spawnDecrease;
        private float minSpawnTime;
        private float nextSpawnTime = 6.2f;
        private float spawnTimer = 5.2f;
        protected Vector2 deadRespawn;
        public MinigameTimeDisplay display;

        private bool spawnSeekers;
        private bool spawnOshiro;

        public MinigameSurvival(EntityData data, Vector2 offset) : base(data, offset) {
            deadRespawn = data.NodesOffset(offset)[0];
            spawnDecrease = data.Float("spawnDecrease", 0.5f);
            minSpawnTime = data.Float("minSpawnTime", 1.2f);
            spawnSeekers = data.Bool("spawnSeekers", true);
            spawnOshiro = data.Bool("spawnOshiro", false);
        }

        public override void Added(Scene scene) {
            base.Added(scene);
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            // If we have just died
            if (level.Session.RespawnPoint == deadRespawn) {
                level.Tracker.GetEntities<Seeker>().ForEach(e => e.RemoveSelf());
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
                level.PauseLock = true;
                diedInGBJInfo.SetValue(null, 0);
                if (spawnSeekers) {
                    List<Entity> seekers = level.Tracker.GetEntities<Seeker>();
                    seekerSpawns.AddRange(seekers.ConvertAll(e => e.Position));
                    foreach (Seeker seeker in seekers) {
                        seeker.RemoveSelf();
                    }
                }
                rand = new Random((int)GameData.Instance.turnOrderSeed + (int)Y);
            }
        }

        protected override void AfterStart() {
            base.AfterStart();
            // Reset timer so it starts at 0 instead of 4.2
            startTime = level.RawTimeActive;
            level.Tracker.GetEntity<Player>().JustRespawned = false;
            level.Session.RespawnPoint = deadRespawn;
            level.Add(display = new MinigameTimeDisplay(this));
        }

        private AngryOshiro lastOshiro;

        public override void Update() {
            base.Update();
            spawnTimer -= Engine.DeltaTime;
            if(lastOshiro != null) {
                DynamicData.For(lastOshiro).Get<StateMachine>("state").State = 0;
                lastOshiro = null;
            }
            if(spawnTimer < 0 && !completed) {
                spawnTimer = nextSpawnTime;
                // Reduce by a half second each time until it's only 1 second between spawns
                if(nextSpawnTime > minSpawnTime) {
                    nextSpawnTime -= spawnDecrease;
                }
                nextSpawnTime = Calc.Max(nextSpawnTime, minSpawnTime);
                if (spawnSeekers && (!spawnOshiro || rand.Next(2) == 0)) {
                    var data = new EntityData { Position = seekerSpawns[rand.Next(seekerSpawns.Count)] };
                    data.Values = new();
                    data.Values["SightDistance"] = 9999f;
                    data.Values["SpottedLosePlayerTime"] = 2.0f;
                    level.Add(new CustomSeeker(data, Vector2.Zero));
                    //level.Add(new Seeker(seekerSpawns[rand.Next(seekerSpawns.Count)], null));
                } else if(spawnOshiro) {
                    var oshiro = new AngryOshiro(new Vector2(-64, 0), false);
                    level.Add(oshiro);
                    lastOshiro = oshiro;
                }
            }
        }
    }
}
