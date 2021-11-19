using System;
using System.IO;
using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;

namespace MadelineParty.CelesteNet {
    public class PlayerChoiceData : DataType<PlayerChoiceData> {
        static PlayerChoiceData() {
            DataID = "mPartyPlayerChoice";
        }

        public enum ChoiceType {
            HEART,
            HEARTX,
            HEARTY,
            ENTERSHOP,
            SHOPITEM,
            DIRECTION
        }

        public DataPlayerInfo Player;

        public ChoiceType choiceType;
        // For buttons, 0 = left, 1 = right
        // For Direction, UP = 0, DOWN = 1, LEFT = 2, RIGHT = 3
        public int choice;

        public override MetaType[] GenerateMeta(DataContext ctx)
        => new MetaType[] {
            new MetaPlayerUpdate(Player)
        };

        public override void FixupMeta(DataContext ctx) {
            Player = Get<MetaPlayerUpdate>(ctx);
        }

        public override void Read(DataContext ctx, BinaryReader reader) {
            choiceType = (ChoiceType)reader.ReadInt32();
            choice = reader.ReadInt32();
        }

        public override void Write(DataContext ctx, BinaryWriter writer) {
            writer.Write((int)choiceType);
            writer.Write(choice);
        }
    }
}
