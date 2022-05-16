using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;
using MadelineParty.Multiplayer.General;

namespace MadelineParty.Multiplayer.CelesteNet {
    public class MinigameVector2Data : DataType<MinigameVector2Data>, MultiplayerData {
        static MinigameVector2Data() {
            DataID = "mPartyMinigameVector2";
        }
        public DataPlayerInfo Player;

        private MinigameVector2 data;

        public MinigameVector2 Data {
            get {
                data.ID = Player.ID;
                return data;
            }
        }

        public void Initialize(MPData args) => data = args as MinigameVector2;

        public override MetaType[] GenerateMeta(DataContext ctx)
        => new MetaType[] {
            new MetaPlayerUpdate(Player)
        };

        public override void FixupMeta(DataContext ctx) {
            Player = Get<MetaPlayerUpdate>(ctx);
        }

        protected override void Read(CelesteNetBinaryReader reader) {
            data.vec = reader.ReadVector2();
            data.extra = reader.ReadInt32();
        }

        protected override void Write(CelesteNetBinaryWriter writer) {
            writer.Write(data.vec);
            writer.Write(data.extra);
        }
    }
}
