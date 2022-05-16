using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;
using MadelineParty.Multiplayer.General;

namespace MadelineParty.Multiplayer.CelesteNet {
    public class DieRollData : DataType<DieRollData>, MultiplayerData {
        static DieRollData() {
            DataID = "mPartyDieRoll";
        }

        public DataPlayerInfo Player;

        private DieRoll data;

        public DieRoll Data {
            get {
                data.ID = Player.ID;
                return data;
            }
        }

        public void Initialize(MPData args) => data = args as DieRoll;

        public override MetaType[] GenerateMeta(DataContext ctx)
        => new MetaType[] {
            new MetaPlayerUpdate(Player)
        };

        public override void FixupMeta(DataContext ctx) {
            Player = Get<MetaPlayerUpdate>(ctx);
        }

        protected override void Read(CelesteNetBinaryReader reader) {
            data.rolls = new int[reader.ReadInt32()];
            for(int i = 0; i < data.rolls.Length; i++) {
                data.rolls[i] = reader.ReadInt32();
            }
        }

        protected override void Write(CelesteNetBinaryWriter writer) {
            writer.Write(data.rolls.Length);
            foreach(int r in data.rolls) {
                writer.Write(r);
            }
        }
    }
}
