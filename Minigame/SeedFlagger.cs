using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace MadelineParty {
    [CustomEntity("madelineparty/seedFlagger")]
    class SeedFlagger : Entity {
        int partitions;

        public SeedFlagger(EntityData data, Vector2 offset) : base(data.Position + offset) {
            partitions = data.Int("partitions", 1);
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            Level level = SceneAs<Level>();
            Random rand = new Random((int)(GameData.Instance.turnOrderSeed / 2) + level.Session.Level.GetHashCode() / 2);
            level.Session.Flags.RemoveWhere((flag) => flag.StartsWith("madelinepartytempseed"));
            level.Session.SetFlag("madelinepartytempseed" + rand.Next() % partitions, true);
        }
    }
}
