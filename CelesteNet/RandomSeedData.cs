using System;
using System.IO;
using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;

namespace MadelineParty.CelesteNet {
    public class RandomSeedData : DataType<RandomSeedData> {
        static RandomSeedData() {
            DataID = "mPartyRandomSeed";
        }

        public DataPlayerInfo Player;

        public uint turnOrderSeed;
        public uint tieBreakerSeed;

        public override MetaType[] GenerateMeta(DataContext ctx)
        => new MetaType[] {
            new MetaPlayerUpdate(Player)
        };

        public override void FixupMeta(DataContext ctx) {
            Player = Get<MetaPlayerUpdate>(ctx);
        }

        protected override void Read(CelesteNetBinaryReader reader) {
            turnOrderSeed = reader.ReadUInt32();
            tieBreakerSeed = reader.ReadUInt32();
        }

        protected override void Write(CelesteNetBinaryWriter writer) {
            writer.Write(turnOrderSeed);
            writer.Write(tieBreakerSeed);
        }
    }
}
