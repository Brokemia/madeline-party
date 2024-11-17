using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

namespace MadelineParty.Minigame {
    [CustomEntity("madelineparty/minigameSpawnpoint")]
    [Tracked(true)]
    public class MinigameSpawnpoint : Entity {
        public IReadOnlyCollection<string> Roles { get; private set; }

        public MinigameSpawnpoint(EntityData data, Vector2 offset) : base(data.Position + offset) {
            Roles = data.Attr("roles")
                .Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .AsReadOnly();
        }
    }
}
