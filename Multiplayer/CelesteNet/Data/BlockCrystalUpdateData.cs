using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;
using MadelineParty.Multiplayer.General;

namespace MadelineParty.Multiplayer.CelesteNet.Data {
    public class BlockCrystalUpdateData : DataType<BlockCrystalUpdateData>, MultiplayerData {
        static BlockCrystalUpdateData() {
            DataID = "mPartyBlockCrystalUpdate";
        }
        public DataPlayerInfo Player;

        private BlockCrystalUpdate data;

        public BlockCrystalUpdate Data {
            get {
                data.ID = Player.ID;
                data.DisplayName = Player.DisplayName;
                return data;
            }
        }

        public void Initialize(MPData args) => data = args as BlockCrystalUpdate;

        public override MetaType[] GenerateMeta(DataContext ctx)
        => new MetaType[] {
            new MetaPlayerUpdate(Player)
        };

        public override void FixupMeta(DataContext ctx) {
            Player = Get<MetaPlayerUpdate>(ctx);
        }

        protected override void Read(CelesteNetBinaryReader reader) {
            data = new() {
                crystalId = reader.ReadInt32(),
                position = reader.ReadVector2(),
                spawning = reader.ReadBoolean()
            };
        }

        protected override void Write(CelesteNetBinaryWriter writer) {
            writer.Write(data.crystalId);
            writer.Write(data.position);
            writer.Write(data.spawning);
        }
    }
}
