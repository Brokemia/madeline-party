using System;
using System.IO;
using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;

namespace MadelineParty.CelesteNet {
    public class MinigameStartData : DataType<MinigameStartData> {
        static MinigameStartData() {
            DataID = "mPartyMinigameStart";
        }

        public DataPlayerInfo Player;

        // The minigame selected
        public int choice;
        // The time to start the minigame
        public long gameStart;

        public override MetaType[] GenerateMeta(DataContext ctx)
        => new MetaType[] {
            new MetaPlayerUpdate(Player)
        };

        public override void FixupMeta(DataContext ctx) {
            Player = Get<MetaPlayerUpdate>(ctx);
        }

        public override void Read(DataContext ctx, BinaryReader reader) {
            choice = reader.ReadInt32();
            gameStart = reader.ReadInt64();
        }

        public override void Write(DataContext ctx, BinaryWriter writer) {
            writer.Write(choice);
            writer.Write(gameStart);
        }
    }
}
