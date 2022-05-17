﻿using System;
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

namespace MadelineParty {
    // Survive an increasing number of seekers for as long as possible
    [CustomEntity("madelineparty/minigameSurvival")]
    public class MinigameSurvival : MinigameEntity {
        private static FieldInfo diedInGBJInfo = typeof(Player).GetField("diedInGBJ", BindingFlags.Static | BindingFlags.NonPublic);
        private List<Vector2> seekerSpawns = new();
        private Random rand;
        private float nextSpawnTime = 6;
        private float spawnTimer = 5.2f;
        protected Vector2 deadRespawn;
        public MinigameTimeDisplay display;

        public MinigameSurvival(EntityData data, Vector2 offset) : base(data, offset) {
            deadRespawn = data.Nodes[0];
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
                GameData.minigameResults.Add(new Tuple<int, uint>(GameData.realPlayerID, (uint)timeElapsed));
                MultiplayerSingleton.Instance.Send(new MinigameEnd { results = (uint)timeElapsed });

                Add(new Coroutine(EndMinigame(HIGHEST_WINS, () => { })));
            } else {
                level.PauseLock = true;
                diedInGBJInfo.SetValue(null, 0);
                List<Entity> seekers = level.Tracker.GetEntities<Seeker>();
                seekerSpawns.AddRange(seekers.ConvertAll(e => e.Position));
                foreach(Seeker seeker in seekers) {
                    seeker.RemoveSelf();
                }
                rand = new Random((int)GameData.turnOrderSeed + (int)Y);
            }
        }

        protected override void AfterStart() {
            base.AfterStart();
            // Reset timer so it starts at 0 instead of 4.2
            startTime = level.RawTimeActive;
            level.Session.RespawnPoint = deadRespawn;
            level.Add(display = new MinigameTimeDisplay(this));
        }

        public override void Update() {
            base.Update();
            spawnTimer -= Engine.DeltaTime;
            if(spawnTimer < 0 && !completed) {
                spawnTimer = nextSpawnTime;
                // Reduce by a half second each time until it's only 1 second between spawns
                if(nextSpawnTime > 1.2) {
                    nextSpawnTime -= 0.5f;
                }
                level.Add(new Seeker(seekerSpawns[rand.Next(seekerSpawns.Count)], null));
            }
        }
    }
}
