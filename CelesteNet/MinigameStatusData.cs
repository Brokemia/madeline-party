using System;
using System.IO;
using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;

namespace MadelineParty.CelesteNet {
    public class MinigameStatusData : DataType<MinigameStatusData> {
        static MinigameStatusData() {
            DataID = "mPartyMinigameStatus";
        }
        public DataPlayerInfo Player;

        // Can represent time, item thingies collected, or other
        public uint results;

        public override MetaType[] GenerateMeta(DataContext ctx)
        => new MetaType[] {
            new MetaPlayerUpdate(Player)
        };

        public override void FixupMeta(DataContext ctx) {
            Player = Get<MetaPlayerUpdate>(ctx);
        }

        protected override void Read(CelesteNetBinaryReader reader) {
            results = reader.ReadUInt32();
        }

        protected override void Write(CelesteNetBinaryWriter writer) {
            writer.Write(results);
        }
    }
}
