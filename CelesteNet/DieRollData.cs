using System.IO;
using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;

namespace MadelineParty.CelesteNet {
    public class DieRollData : DataType<DieRollData> {
        static DieRollData() {
            DataID = "mPartyDieRoll";
        }

        public DataPlayerInfo Player;

        public int[] rolls;

        public override MetaType[] GenerateMeta(DataContext ctx)
        => new MetaType[] {
            new MetaPlayerUpdate(Player)
        };

        public override void FixupMeta(DataContext ctx) {
            Player = Get<MetaPlayerUpdate>(ctx);
        }

        public override void Read(DataContext ctx, BinaryReader reader) {
            rolls = new int[reader.ReadInt32()];
            for(int i = 0; i < rolls.Length; i++) {
                rolls[i] = reader.ReadInt32();
            }
        }

        public override void Write(DataContext ctx, BinaryWriter writer) {
            writer.Write(rolls.Length);
            foreach(int r in rolls) {
                writer.Write(r);
            }
        }
    }
}
