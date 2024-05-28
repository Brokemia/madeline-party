using System;
using System.Collections;
using Celeste;
using Celeste.Mod;
using Celeste.Mod.Entities;
using MadelineParty.Minigame;
using MadelineParty.Multiplayer;
using MadelineParty.Multiplayer.General;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty {
    [CustomEntity("madelineparty/minigameInfinityTrigger")]
    public class MinigameInfinityTrigger : MinigameEntity {
        public class InfinityMinigamePersistentData : MinigamePersistentData {
            public uint Loops { get; set; }
            public uint Distance { get; set; }
        }
        public Coroutine endCoroutine;
        public Vector2 backwardsSpot;
        public float tpDist;
        private bool everyOtherFrame;
        private bool vertical;
        private InfinityMinigamePersistentData infinityData;

        public MinigameInfinityTrigger(EntityData data, Vector2 offset) : base(data, offset) {
            backwardsSpot = data.NodesOffset(offset)[0];
            vertical = data.Bool("vertical", false);
            tpDist = vertical ? Position.Y - backwardsSpot.Y : Position.X - backwardsSpot.X;
        }

        protected override MinigamePersistentData NewData() {
            return new InfinityMinigamePersistentData();
        }

        protected override void AfterStart() {
            base.AfterStart();
            infinityData = DataAs<InfinityMinigamePersistentData>();
            level.Add(new MinigameDistanceDisplay(this));
            level.Add(new MinigameTimeDisplay(this, true));
        }

        public override void Update() {
            base.Update();
            if (!Data.Started) return;
            if (level.RawTimeActive - Data.StartTime >= 30 && endCoroutine == null) {
                Add(endCoroutine = new Coroutine(FinishMinigame()));
            }

            Player player = level.Tracker.GetEntity<Player>();

            if (player != null && infinityData.Loops > 0 && (vertical ? player.Y > backwardsSpot.Y + Height + 3 * 8 : player.X < backwardsSpot.X - Width - 3 * 8)) {
                infinityData.Loops--;
                Teleport(player, tpDist * (vertical ? Vector2.UnitY : Vector2.UnitX));
            }

            if (everyOtherFrame) {
                MultiplayerSingleton.Instance.Send(new MinigameStatus { results = infinityData.Distance });
            }
            everyOtherFrame = !everyOtherFrame;

            if (player != null) {
                infinityData.Distance = calculateDist(infinityData.Loops, player.Position);
                GameData.Instance.minigameStatus[GameData.Instance.realPlayerID] = infinityData.Distance;
            }
        }

        private uint calculateDist(uint loops, Vector2 pos) {
            return (uint)Math.Max(loops * (vertical ? -tpDist : tpDist) + (vertical ? backwardsSpot.Y - pos.Y : pos.X - backwardsSpot.X) - 10, 0);
        }

        protected IEnumerator FinishMinigame() {
            Player player = level.Tracker.GetEntity<Player>();
            // This check is probably unnecessary, but I left it in for safety
            while (player == null) {
                yield return null;
                player = level.Tracker.GetEntity<Player>();
            }
            completed = true;
            // Freeze the player so they can't do any more moving until everyone else is done
            player.StateMachine.State = Player.StFrozen;
            player.Speed = Vector2.Zero;
            level.CanRetry = false;
            infinityData.Distance = calculateDist(infinityData.Loops, player.Position);
            Console.WriteLine("Minigame Distance: " + infinityData.Distance);
            GameData.Instance.minigameResults.Add(new Tuple<int, uint>(GameData.Instance.realPlayerID, infinityData.Distance));
            MultiplayerSingleton.Instance.Send(new MinigameEnd { results = infinityData.Distance });

            yield return new SwapImmediately(EndMinigame(HIGHEST_WINS, null));
        }

        public override void OnEnter(Player player) {
            infinityData.Loops++;
            Teleport(player, -tpDist * (vertical ? Vector2.UnitY : Vector2.UnitX));
        }

        private void Teleport(Player player, Vector2 diff) {
            Level level = SceneAs<Level>();
            foreach (TrailManager.Snapshot snapshot in level.Tracker.GetEntities<TrailManager.Snapshot>()) {
                snapshot.Position += diff;
            }
            foreach (SlashFx slash in level.Tracker.GetEntities<SlashFx>()) {
                slash.Position += diff;
            }
            foreach (SpeedRing ring in level.Tracker.GetEntities<SpeedRing>()) {
                ring.Position += diff;
            }
            player.Position += diff;
            player.Hair.MoveHairBy(diff);
            level.Camera.Position += diff;
            foreach (Backdrop backdrop in level.Background.Backdrops) {
                backdrop.Position += diff * backdrop.Scroll;
                if(backdrop is NorthernLights lights) {
                    for(int i = 0; i < lights.particles.Length; i++) {
                        lights.particles[i].Position += diff * 0.2f;
                    }
                }
            }
            foreach (Backdrop backdrop in level.Foreground.Backdrops) {
                backdrop.Position += diff * backdrop.Scroll;
                if (backdrop is NorthernLights lights) {
                    for (int i = 0; i < lights.particles.Length; i++) {
                        lights.particles[i].Position += diff * 0.2f;
                    }
                }
            }

            for(int i = 0; i < level.ParticlesFG.particles.Length; i++) {
                level.ParticlesFG.particles[i].Position += diff;
            }

            for (int i = 0; i < level.ParticlesBG.particles.Length; i++) {
                level.ParticlesBG.particles[i].Position += diff;
            }

            for (int i = 0; i < level.Particles.particles.Length; i++) {
                level.Particles.particles[i].Position += diff;
            }

            for(int i = 0; i < StarClimbGraphicsController.rays.Length; i++) {
                StarClimbGraphicsController.rays[i].X += diff.X * 0.9f;
                StarClimbGraphicsController.rays[i].Y += diff.Y * 0.7f;
            }

            foreach (DreamBlock db in level.Tracker.GetEntities<DreamBlock>()) {
                for (int i = 0; i < db.particles.Length; i++) {
                    //Console.WriteLine(db.Position + " " + (particleProxy.Position + (level.Camera.Position - asVector) * (0.3f + 0.25f * particleProxy.Layer)) + " " +
                        //(particleProxy.Position + (level.Camera.Position) * (0.3f + 0.25f * particleProxy.Layer)) + " " + asVector * (0.3f + 0.25f * particleProxy.Layer));
                    db.particles[i].Position -= diff * .5f * (0.3f + 0.25f * db.particles[i].Layer);
                }
            }
        }
    }
}
