using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;
using System.Collections.Generic;

namespace MadelineParty.Multiplayer.CelesteNet {
    public class RandomSeedData : DataType<RandomSeedData>, MultiplayerData {
        static RandomSeedData() {
            DataID = "mPartyRandomSeed";
        }

        public DataPlayerInfo Player;

        public uint turnOrderSeed;
        public uint tieBreakerSeed;

        public void Initialize(Dictionary<string, object> args) {
            turnOrderSeed = args.OrDefault("turnOrderSeed", turnOrderSeed);
            tieBreakerSeed = args.OrDefault("tieBreakerSeed", tieBreakerSeed);
        }

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
