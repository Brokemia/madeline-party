using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;
using MadelineParty.Multiplayer.General;

namespace MadelineParty.Multiplayer.CelesteNet.Data {
    public class RandomSeedData : DataType<RandomSeedData>, MultiplayerData {
        static RandomSeedData() {
            DataID = "mPartyRandomSeed";
        }

        public DataPlayerInfo Player;

        private RandomSeed data;

        public RandomSeed Data {
            get {
                data.ID = Player.ID;
                data.DisplayName = Player.DisplayName;
                return data;
            }
        }

        public void Initialize(MPData args) => data = args as RandomSeed;

        public override MetaType[] GenerateMeta(DataContext ctx)
        => new MetaType[] {
            new MetaPlayerUpdate(Player)
        };

        public override void FixupMeta(DataContext ctx) {
            Player = Get<MetaPlayerUpdate>(ctx);
        }

        protected override void Read(CelesteNetBinaryReader reader) {
            data = new();
            data.turnOrderSeed = reader.ReadUInt32();
            data.tieBreakerSeed = reader.ReadUInt32();
        }

        protected override void Write(CelesteNetBinaryWriter writer) {
            writer.Write(data.turnOrderSeed);
            writer.Write(data.tieBreakerSeed);
        }
    }
}
