using Celeste;
using Celeste.Mod.Entities;
using MadelineParty.Minigame;
using MadelineParty.Multiplayer;
using MadelineParty.Multiplayer.General;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace MadelineParty.Entities
{
    [CustomEntity("madelineparty/multiplayerSyncedKevin")]
    public class SyncedKevin : CrushBlock
    {
        public bool reversed;
        public EntityID id;
        public string requiredRole;

        private MinigameEntity minigame;

        public SyncedKevin(EntityData data, Vector2 offset, EntityID id) : base(data, offset)
        {
            this.id = id;
            reversed = data.Bool("reversed");
            OnDashCollide = OnDashed;
            requiredRole = data.Attr("requiredRole", null);
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            Add(new MultiplayerHandlerComponent<SyncedKevinHit>("syncedKevin " + id.Key, HandleSyncedKevinHit));
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            minigame = Scene.Tracker.GetEntity<MinigameEntity>();
        }

        private void HandleSyncedKevinHit(MPData data)
        {
            if (data is not SyncedKevinHit hit) return;
            // If another player in our party has hit a synced kevin
            if (GameData.Instance.celestenetIDs.Contains(hit.ID) && hit.ID != MultiplayerSingleton.Instance.CurrentPlayerID() && hit.kevinID == id.Key)
            {
                Attack(hit.dir);
            }
        }

        private new DashCollisionResults OnDashed(Player player, Vector2 direction)
        {
            if ((string.IsNullOrWhiteSpace(requiredRole) || minigame == null || minigame.Data.HasRole(GameData.Instance.realPlayerID, requiredRole))
                && CanActivate(-direction))
            {
                var dir = reversed ? direction : -direction;
                MultiplayerSingleton.Instance.Send(new SyncedKevinHit
                {
                    kevinID = id.Key,
                    dir = dir
                });
                Attack(dir);
                return DashCollisionResults.Rebound;
            }
            return DashCollisionResults.NormalCollision;
        }
    }
}
