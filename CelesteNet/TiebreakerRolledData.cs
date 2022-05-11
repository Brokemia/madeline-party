using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;
using Microsoft.Xna.Framework;

namespace MadelineParty.CelesteNet {
    public class TiebreakerRolledData : DataType<TiebreakerRolledData> {
        static TiebreakerRolledData() {
            DataID = "mPartyTiebreakerRolled";
        }

        public DataPlayerInfo Player;

        public Vector2 ButtonPosition;

        public override MetaType[] GenerateMeta(DataContext ctx)
        => new MetaType[] {
            new MetaPlayerUpdate(Player)
        };

        public override void FixupMeta(DataContext ctx) {
            Player = Get<MetaPlayerUpdate>(ctx);
        }

        protected override void Read(CelesteNetBinaryReader reader) {
            ButtonPosition = reader.ReadVector2();
        }

        protected override void Write(CelesteNetBinaryWriter writer) {
            writer.Write(ButtonPosition);
        }
    }
}
