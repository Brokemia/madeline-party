﻿using Celeste;
using MadelineParty.Multiplayer.General;
using MadelineParty.Multiplayer;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Monocle;
using Celeste.Mod.Entities;
using MadelineParty.Entities;

namespace MadelineParty.Minigame
{
    [CustomEntity("madelineparty/minigameLaserDodge")]
    public class MinigameLaserDodge : MinigameEntity {
        private float spawnDecrease;
        private float minSpawnTime;
        private float nextSpawnTime = 6.2f;
        private float spawnTimer = 5.2f;

        private Random rand;

        protected Vector2 deadRespawn;

        private string patternStr;
        private List<LaserSource>[] patterns;
        
        public MinigameLaserDodge(EntityData data, Vector2 offset) : base(data, offset) {
            deadRespawn = data.NodesOffset(offset)[0];
            spawnDecrease = data.Float("spawnDecrease", 0.5f);
            minSpawnTime = data.Float("minSpawnTime", 1.5f);
            patternStr = data.Attr("patterns");
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            var lasers = new Dictionary<int, List<LaserSource>>();
            foreach (LaserSource laser in level.Tracker.GetEntities<LaserSource>()) {
                if(!lasers.ContainsKey(laser.laserID)) {
                    lasers[laser.laserID] = new();
                }
                lasers[laser.laserID].Add(laser);
            }
            var split = patternStr.Split(';');
            patterns = new List<LaserSource>[split.Length];
            for (int i = 0; i < split.Length; i++) {
                patterns[i] = new List<LaserSource>();
                foreach (var patternPart in split[i].Split(',')) {
                    // Allow ranges of numbers
                    if (patternPart.Contains('-')) {
                        var rangeSplit = patternPart.Split('-');
                        for (int j = int.Parse(rangeSplit[0]); j <= int.Parse(rangeSplit[1]); j++) {
                            patterns[i].AddRange(lasers[j]);
                        }
                    } else {
                        patterns[i].AddRange(lasers[int.Parse(patternPart)]);
                    }
                }
            }

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
            }

            rand = new Random(GameData.Instance.Random.Next());
        }

        protected override void AfterStart() {
            base.AfterStart();
            level.Tracker.GetEntity<Player>().JustRespawned = false;
            level.Session.RespawnPoint = deadRespawn;
            level.Add(new MinigameTimeDisplay(this));
        }

        public override void Update() {
            base.Update();
            if (!Data.Started) return;
            spawnTimer -= Engine.DeltaTime;
            if (spawnTimer < 0 && !completed) {
                spawnTimer = nextSpawnTime;
                // Reduce by a half second each time until it's only 1 second between spawns
                if (nextSpawnTime > minSpawnTime) {
                    nextSpawnTime -= spawnDecrease;
                }
                nextSpawnTime = Calc.Max(nextSpawnTime, minSpawnTime);

                // Do the spawn
                var pattern = patterns[rand.Next(patterns.Length)];
                foreach (var laser in pattern) {
                    laser.Lase(spawnTimer / 2);
                }
            }
        }
    }
}
