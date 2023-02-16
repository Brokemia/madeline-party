using System;
using System.Collections.Generic;
using System.Reflection;
using Celeste;
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
        private List<Vector2> bossSpawns = new();
        private Random rand;
        private float spawnDecrease;
        private float minSpawnTime;
        private float nextSpawnTime = 6.2f;
        private float spawnTimer = 5.2f;
        protected Vector2 deadRespawn;

        private bool spawnSeekers;
        private bool spawnOshiro;
        private bool spawnFinalBoss;
        private enum SpawnOptions {
            Seeker, Oshiro, FinalBoss
        }
        private List<SpawnOptions> options = new();

        public MinigameSurvival(EntityData data, Vector2 offset) : base(data, offset) {
            deadRespawn = data.NodesOffset(offset)[0];
            spawnDecrease = data.Float("spawnDecrease", 0.5f);
            minSpawnTime = data.Float("minSpawnTime", 1.2f);
            if (spawnSeekers = data.Bool("spawnSeekers", true)) options.Add(SpawnOptions.Seeker);
            if (spawnOshiro = data.Bool("spawnOshiro", false)) options.Add(SpawnOptions.Oshiro);
            if (spawnFinalBoss = data.Bool("spawnBadelineBoss", false)) options.Add(SpawnOptions.FinalBoss);
        }

        public override void Added(Scene scene) {
            base.Added(scene);
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            // If we have just died
            if (level.Session.RespawnPoint == deadRespawn) {
                level.Tracker.GetEntities<Seeker>().ForEach(e => e.RemoveSelf());
                level.Tracker.GetEntities<FinalBoss>().ForEach(e => e.RemoveSelf());
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
                }
                if (spawnSeekers) {
                    List<Entity> seekers = level.Tracker.GetEntities<Seeker>();
                    seekerSpawns.AddRange(seekers.ConvertAll(e => e.Position));
                    foreach (Seeker seeker in seekers) {
                        seeker.RemoveSelf();
                    }
                }
                if (spawnFinalBoss) {
                    List<Entity> bosses = level.Tracker.GetEntities<FinalBoss>();
                    bossSpawns.AddRange(bosses.ConvertAll(e => e.Position));
                    foreach (FinalBoss boss in bosses) {
                        boss.RemoveSelf();
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
            level.Add(new MinigameTimeDisplay(this));
        }

        private AngryOshiro lastOshiro;
        private readonly List<int> validBossPatterns = new() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 11, 14, 15 };

        public override void Update() {
            base.Update();
            if (!started) return;
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
                var spawnChoice = options[rand.Next(options.Count)];
                switch (spawnChoice) {
                    case SpawnOptions.Seeker:
                        var data = new EntityData { Position = seekerSpawns[rand.Next(seekerSpawns.Count)] };
                        data.Values = new();
                        data.Values["SightDistance"] = float.MaxValue;
                        data.Values["SpottedLosePlayerTime"] = float.MaxValue;
                        data.Values["AlwaysSeePlayer"] = true;
                        data.Values["SpottedNoCameraLimit"] = true;
                        var seeker = new CustomSeeker(data, Vector2.Zero);
                        level.Add(seeker);
                        //level.Add(new Seeker(seekerSpawns[rand.Next(seekerSpawns.Count)], null));
                        break;
                    case SpawnOptions.Oshiro:
                        var oshiro = new AngryOshiro(new Vector2(-64, 0), false);
                        level.Add(oshiro);
                        lastOshiro = oshiro;
                        break;
                    case SpawnOptions.FinalBoss:
                        var boss = new FinalBoss(bossSpawns[rand.Next(bossSpawns.Count)], new[] { Vector2.Zero }, validBossPatterns[rand.Next(validBossPatterns.Count)], 120, false, false, false);
                        boss.playerHasMoved = true;
                        level.Add(boss);
                        boss.StartAttacking();
                        break;
                }
            }
        }
    }
}
