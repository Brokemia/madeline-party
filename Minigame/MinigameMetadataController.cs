using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadelineParty.Minigame {
    [CustomEntity(EntityName)]
    [Tracked(true)]
    public class MinigameMetadataController : Entity {
        public const string EntityName = "madelineparty/minigameMetadataController";

        public static MinigameMetadata LoadMetadata(EntityData data) {
            var meta = new MinigameMetadata() {
                MinPlayers = data.Int("minPlayers", 1),
                MaxPlayers = data.Int("maxPlayers", 4)
            };
            foreach (string tag in data.Attr("tags", "").Split(',').Select(tag => tag.Trim()).Where(tag => !string.IsNullOrEmpty(tag))) {
                meta.MinigameTags.Add(tag);
            }

            return meta;
        }

        public class MinigameMetadata {
            public int MinPlayers { get; set; }
            public int MaxPlayers { get; set; }
            public HashSet<string> MinigameTags { get; } = new();
        }

        public MinigameMetadata Metadata;

        public MinigameMetadataController(EntityData data, Vector2 offset) : base(data.Position + offset) {
            Metadata = LoadMetadata(data);
        }
    }
}
