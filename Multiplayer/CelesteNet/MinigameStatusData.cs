using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;
using MadelineParty.Multiplayer.General;
using System.Collections.Generic;

namespace MadelineParty.Multiplayer.CelesteNet {
    public class MinigameStatusData : DataType<MinigameStatusData>, MultiplayerData {
        static MinigameStatusData() {
            DataID = "mPartyMinigameStatus";
        }
        public DataPlayerInfo Player;

        private MinigameStatus data;

        public MinigameStatus Data {
            get {
                data.ID = Player.ID;
                data.DisplayName = Player.DisplayName;
                return data;
            }
        }

        public void Initialize(MPData args) => data = args as MinigameStatus;

        public override MetaType[] GenerateMeta(DataContext ctx)
        => new MetaType[] {
            new MetaPlayerUpdate(Player)
        };

        public override void FixupMeta(DataContext ctx) {
            Player = Get<MetaPlayerUpdate>(ctx);
        }

        protected override void Read(CelesteNetBinaryReader reader) {
            data = new();
            data.results = reader.ReadUInt32();
        }

        protected override void Write(CelesteNetBinaryWriter writer) {
            writer.Write(data.results);
        }
    }
}
