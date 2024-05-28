using Celeste.Mod.CelesteNet.DataTypes;
using Celeste.Mod.CelesteNet;
using MadelineParty.Multiplayer.General;

namespace MadelineParty.Multiplayer.CelesteNet.Data {
    public class SyncedKevinHitData : DataType<SyncedKevinHitData>, MultiplayerData {
        static SyncedKevinHitData() {
            DataID = "mPartySyncedKevinHit";
        }

        public DataPlayerInfo Player;

        private SyncedKevinHit data;

        public SyncedKevinHit Data {
            get {
                data.ID = Player.ID;
                data.DisplayName = Player.DisplayName;
                return data;
            }
        }

        public void Initialize(MPData args) => data = args as SyncedKevinHit;

        public override MetaType[] GenerateMeta(DataContext ctx)
        => new MetaType[] {
            new MetaPlayerUpdate(Player)
        };

        public override void FixupMeta(DataContext ctx) {
            Player = Get<MetaPlayerUpdate>(ctx);
        }

        protected override void Read(CelesteNetBinaryReader reader) {
            data = new();
            data.kevinID = reader.ReadString();
            data.dir = reader.ReadVector2();
        }

        protected override void Write(CelesteNetBinaryWriter writer) {
            writer.Write(data.kevinID);
            writer.Write(data.dir);
        }
    }
}
