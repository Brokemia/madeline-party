using System.Collections.Generic;
using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;

namespace MadelineParty.Multiplayer.CelesteNet {
    public class PlayerChoiceData : DataType<PlayerChoiceData>, MultiplayerData {
        static PlayerChoiceData() {
            DataID = "mPartyPlayerChoice";
        }

        public const int HEART = 0;
        public const int HEARTSPACEID = 1;
        public const int ENTERSHOP = 2;
        public const int SHOPITEM = 3;
        public const int DIRECTION = 4;

        public DataPlayerInfo Player;

        public int choiceType;
        // For buttons, 0 = left, 1 = right
        // For Direction, UP = 0, DOWN = 1, LEFT = 2, RIGHT = 3
        public int choice;

        public void Initialize(Dictionary<string, object> args) {
            choiceType = args.OrDefault("choiceType", choiceType);
            choice = args.OrDefault("choice", choice);
        }

        public override MetaType[] GenerateMeta(DataContext ctx)
        => new MetaType[] {
            new MetaPlayerUpdate(Player)
        };

        public override void FixupMeta(DataContext ctx) {
            Player = Get<MetaPlayerUpdate>(ctx);
        }

        protected override void Read(CelesteNetBinaryReader reader) {
            choiceType = reader.ReadInt32();
            choice = reader.ReadInt32();
        }

        protected override void Write(CelesteNetBinaryWriter writer) {
            writer.Write(choiceType);
            writer.Write(choice);
        }
    }
}
