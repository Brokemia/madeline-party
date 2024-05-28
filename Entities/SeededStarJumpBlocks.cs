using BrokemiaHelper;
using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty.Entities
{
    [CustomEntity("madelineparty/seededStarJumpBlock")]
    [TrackedAs(typeof(StarJumpBlock))]
    public class SeededStarJumpBlocks : StarJumpBlock
    {
        public string seed;

        public SeededStarJumpBlocks(EntityData data, Vector2 offset) : base(data, offset)
        {
            seed = data.Attr("seed");
        }

        public override void Awake(Scene scene)
        {
            if (!string.IsNullOrEmpty(seed))
            {
                Calc.PushRandom(seed.SimpleHash());
                base.Awake(scene);
                Calc.PopRandom();
            }
            else
            {
                base.Awake(scene);
            }
        }

    }
}
