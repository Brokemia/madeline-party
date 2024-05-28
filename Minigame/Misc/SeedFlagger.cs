using BrokemiaHelper;
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
            level.Session.Flags.RemoveWhere((flag) => flag.StartsWith("madelinepartytempseed"));
            level.Session.SetFlag("madelinepartytempseed" + GameData.Instance.Random.Next(partitions), true);
        }
    }
}
