using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;
using System.Collections.Generic;

namespace MadelineParty.Multiplayer.CelesteNet {
    public class MinigameStatusData : DataType<MinigameStatusData>, MultiplayerData {
        static MinigameStatusData() {
            DataID = "mPartyMinigameStatus";
        }
        public DataPlayerInfo Player;

        // Can represent time, item thingies collected, or other
        public uint results;

        public void Initialize(Dictionary<string, object> args) {
            results = args.OrDefault("results", results);
        }

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
