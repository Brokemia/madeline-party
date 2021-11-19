using System;
using System.IO;
using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;

namespace MadelineParty.CelesteNet {
    public class MinigameEndData : DataType<MinigameEndData> {
        static MinigameEndData() {
            DataID = "mPartyMinigameEnd";
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

        public override void Read(DataContext ctx, BinaryReader reader) {
            results = reader.ReadUInt32();
        }

        public override void Write(DataContext ctx, BinaryWriter writer) {
            writer.Write(results);
        }
    }
}
