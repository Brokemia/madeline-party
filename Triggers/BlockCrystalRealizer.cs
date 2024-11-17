using Celeste;
using Celeste.Mod.Entities;
using MadelineParty.Entities;
using MadelineParty.Multiplayer;
using MadelineParty.Multiplayer.General;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;

namespace MadelineParty.Triggers {
    [CustomEntity("madelineparty/blockCrystalRealizer")]
    [Tracked]
    public class BlockCrystalRealizer : Trigger {
        private Vector2 blockSpawnPos;

        private Queue<BlockCrystal> queuedCrystals = new();

        private MinigameEntity minigame;

        public BlockCrystalRealizer(EntityData data, Vector2 offset) : base(data, offset) {
            blockSpawnPos = data.NodesOffset(offset)[0];
            Add(new HoldableCollider(OnCollide));
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            minigame = Scene.Tracker.GetEntity<MinigameEntity>();
        }

        public override void Update() {
            base.Update();
            if (queuedCrystals.TryPeek(out var crystal) && crystal.CanSpawn(blockSpawnPos)) {
                queuedCrystals.Dequeue();
                crystal.SpawnBlock(this, blockSpawnPos);
            }
        }

        private void OnCollide(Holdable holdable) {
            if (holdable.Entity is not BlockCrystal crystal) return;

            if (string.IsNullOrWhiteSpace(crystal.MinigameOwnerRole) || (minigame?.Data.HasRole(GameData.Instance.realPlayerID, crystal.MinigameOwnerRole) ?? true)) {
                MultiplayerSingleton.Instance.Send(new BlockCrystalUpdate {
                    crystalId = crystal.Id.ID,
                    position = crystal.Position,
                    spawning = true
                });
            }
            OnBlockCrystalCollide(crystal);
        }

        public void OnBlockCrystalCollide(BlockCrystal crystal) {
            // Prevent OnCollide being called repeatedly
            crystal.Active = false;
            crystal.Collidable = false;
            crystal.Disappear();
            // Force the player to drop the crystal
            var player = crystal.Hold.Holder;
            if (player != null) {
                crystal.Hold.Release(Vector2.Zero);
                player.Holding = null;
            }

            queuedCrystals.Enqueue(crystal);
        }
    }
}
