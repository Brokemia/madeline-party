using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;
using MadelineParty.Multiplayer.General;
using System.Collections.Generic;

namespace MadelineParty.Multiplayer.CelesteNet {
    public class MinigameReadyData : DataType<MinigameReadyData>, MultiplayerData {
        static MinigameReadyData() {
            DataID = "mPartyMinigameReady";
        }
        public DataPlayerInfo Player;

        private MinigameReady data;

        public MinigameReady Data {
            get {
                data.ID = Player.ID;
                data.DisplayName = Player.DisplayName;
                return data;
            }
        }

        public void Initialize(MPData args) => data = args as MinigameReady;

        public override MetaType[] GenerateMeta(DataContext ctx)
        => new MetaType[] {
            new MetaPlayerUpdate(Player)
        };

        public override void FixupMeta(DataContext ctx) {
            Player = Get<MetaPlayerUpdate>(ctx);
        }

        protected override void Read(CelesteNetBinaryReader reader) {
            data = new();
            data.player = reader.ReadInt32();
        }

        protected override void Write(CelesteNetBinaryWriter writer) {
            writer.Write(data.player);
        }
    }
}
