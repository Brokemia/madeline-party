using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod;
using Celeste.Mod.Entities;
using MadelineParty.Minigame;
using MadelineParty.Multiplayer;
using MadelineParty.Multiplayer.General;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty {
    // Move an infinite number of Theos from one place to another
    [Tracked(true)]
    public class MinigameTheoMover : MinigameEntity {
        public class TheoMoverMinigamePersistentData : MinigamePersistentData {
            public uint TheoCount { get; set; }
        }
        
        private const int THEOS_NEEDED = 5;
        protected Vector2 theoRespawnPoint;
        public Coroutine endCoroutine;
        public string entityToUse;
        private TheoMoverMinigamePersistentData theoMoverData;

        public MinigameTheoMover(EntityData data, Vector2 offset) : base(data, offset) {
            theoRespawnPoint = data.Nodes[0];
            entityToUse = data.Attr("spawnEntity", "Celeste.TheoCrystal");
            Add(new HoldableCollider(OnHoldable));
        }

        protected override MinigamePersistentData NewData() {
            return new TheoMoverMinigamePersistentData();
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            theoMoverData = DataAs<TheoMoverMinigamePersistentData>();
            level.Add(createEntity(entityToUse));
        }

        // TODO maybe replace with reflection at some point
        private Entity createEntity(string entity) {
            return entity switch {
                "Celeste.TheoCrystal" => new TheoCrystal(theoRespawnPoint),
                "Celeste.Glider" => new Glider(theoRespawnPoint, false, false) { Speed = new(0, -270f) },
                _ => null,
            };
        }

        protected override void AfterStart() {
            base.AfterStart();
            level.Add(new MinigameScoreDisplay(this, "{0}/5"));
            level.Add(new MinigameTimeDisplay(this));
        }

        public override void Update() {
            base.Update();
            // If another player beat us to it
            if (Data.Started && endCoroutine == null && GameData.Instance.minigameResults.Count > 0) {
                Add(endCoroutine = new Coroutine(EndMinigame()));
            }
        }

        protected IEnumerator EndMinigame() {
            completed = true;
            MinigameTimeDisplay display = level.Entities.FindFirst<MinigameTimeDisplay>();
            if (display != null)
                display.finalTime = level.RawTimeActive - Data.StartTime;
            uint timeElapsed = theoMoverData.TheoCount < THEOS_NEEDED ? uint.MaxValue : (uint)((level.RawTimeActive - Data.StartTime) * 10000);
            level.CanRetry = false;
            Console.WriteLine("Theo Count: " + theoMoverData.TheoCount);
            GameData.Instance.minigameResults.Add(new Tuple<int, uint>(GameData.Instance.realPlayerID, timeElapsed));
            MultiplayerSingleton.Instance.Send(new MinigameEnd { results = timeElapsed });

            yield return new SwapImmediately(EndMinigame(LOWEST_WINS, null));
        }

        private void OnHoldable(Holdable h) {
            if (h.Entity.GetType().FullName == entityToUse) {
                h.Entity.RemoveSelf();
                theoMoverData.TheoCount++;
                
                GameData.Instance.minigameStatus[GameData.Instance.realPlayerID] = theoMoverData.TheoCount;
                MultiplayerSingleton.Instance.Send(new MinigameStatus { results = theoMoverData.TheoCount });
                if (theoMoverData.TheoCount >= THEOS_NEEDED && endCoroutine == null) {
                    Add(endCoroutine = new Coroutine(EndMinigame()));
                } else {
                    if (!justSpawned) {
                        SpawnNext();
                    }
                    justSpawned = false;
                }
            }
        }

        public void SpawnNext() {
            if (justSpawned) return;
            justSpawned = true;
            level.Add(createEntity(entityToUse));
        }

        private bool justSpawned;
    }

    [CustomEntity("madelineparty/theoMoverEarlySpawn")]
    public class EarlyTheoMoverSpawn : Entity {
        private MinigameTheoMover minigame;

        public EarlyTheoMoverSpawn(EntityData data, Vector2 offset) : base(data.Position + offset) {
            Collider = new Hitbox(data.Width, data.Height);
            Visible = false;
            Add(new HoldableCollider(OnHoldable));
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            minigame = scene.Tracker.GetEntity<MinigameTheoMover>();
            if(minigame == null) {
                RemoveSelf();
            }
        }

        private void OnHoldable(Holdable h) {
            minigame.SpawnNext();
        }
    }
}
