using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;
using MadelineParty.Multiplayer.General;

namespace MadelineParty.Multiplayer.CelesteNet {
    public class TiebreakerRolledData : DataType<TiebreakerRolledData>, MultiplayerData {
        static TiebreakerRolledData() {
            DataID = "mPartyTiebreakerRolled";
        }

        public DataPlayerInfo Player;

        private TiebreakerRolled data;

        public TiebreakerRolled Data {
            get {
                data.ID = Player.ID;
                data.DisplayName = Player.DisplayName;
                return data;
            }
        }

        public void Initialize(MPData args) => data = args as TiebreakerRolled;

        public override MetaType[] GenerateMeta(DataContext ctx)
        => new MetaType[] {
            new MetaPlayerUpdate(Player)
        };

        public override void FixupMeta(DataContext ctx) {
            Player = Get<MetaPlayerUpdate>(ctx);
        }

        protected override void Read(CelesteNetBinaryReader reader) {
            data = new();
            data.ButtonPosition = reader.ReadVector2();
        }

        protected override void Write(CelesteNetBinaryWriter writer) {
            writer.Write(data.ButtonPosition);
        }
    }
}
