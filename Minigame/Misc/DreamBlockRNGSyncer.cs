using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadelineParty {
    [Tracked]
    [CustomEntity("madelineparty/dreamBlockRNGSyncer")]
    class DreamBlockRNGSyncer : Entity {

        public DreamBlockRNGSyncer(EntityData data, Vector2 offset) : base(data.Position + offset) {
            Depth = int.MaxValue;
        }

        public static void Load() {
            On.Celeste.DreamBlock.Added += DreamBlock_Added;
            On.Celeste.DreamBlock.Update += DreamBlock_Update;
        }

        private static void DreamBlock_Update(On.Celeste.DreamBlock.orig_Update orig, DreamBlock self) {
            Scene scene = self.Scene;
            if (MadelinePartyModule.IsSIDMadelineParty((scene as Level).Session.Area.GetSID()) && scene.Tracker.GetEntity<DreamBlockRNGSyncer>() != null) {
                DynData<DreamBlock> selfData = new DynData<DreamBlock>(self);

                Random oldRand = Calc.Random;
                Calc.Random = selfData.Get<Random>("madelinePartyRandom");

                orig(self);

                Calc.Random = oldRand;
            } else {
                orig(self);
            }
        }

        private static void DreamBlock_Added(On.Celeste.DreamBlock.orig_Added orig, DreamBlock self, Scene scene) {
            if (MadelinePartyModule.IsSIDMadelineParty((scene as Level).Session.Area.GetSID()) && (scene as Level).Session.LevelData.Entities.Any((data) => data.Name.Equals("madelineparty/dreamBlockRNGSyncer"))) {
                DynData<Scene> sceneData = new DynData<Scene>();
                var seed = sceneData.Get<int?>("madelinePartyRandomSeed");
                if(seed == null) {
                    seed = Calc.Random.Next();
                    sceneData["madelinePartyRandomSeed"] = seed;
                }
                DynData<DreamBlock> selfData = new DynData<DreamBlock>(self);
                selfData["madelinePartyRandom"] = new Random(seed.Value);

                Random oldRand = Calc.Random;
                Calc.Random = selfData.Get<Random>("madelinePartyRandom");

                orig(self, scene);

                Calc.Random = oldRand;
            } else {
                orig(self, scene);
            }
        }
    }
}
