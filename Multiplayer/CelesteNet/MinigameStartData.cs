using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;
using System.Collections.Generic;

namespace MadelineParty.Multiplayer.CelesteNet {
    public class MinigameStartData : DataType<MinigameStartData>, MultiplayerData {
        static MinigameStartData() {
            DataID = "mPartyMinigameStart";
        }

        public DataPlayerInfo Player;

        // The minigame selected
        public int choice;
        // The time to start the minigame
        public long gameStart;

        public void Initialize(Dictionary<string, object> args) {
            choice = args.OrDefault("choice", choice);
            gameStart = args.OrDefault("gameStart", gameStart);
        }

        public override MetaType[] GenerateMeta(DataContext ctx)
        => new MetaType[] {
            new MetaPlayerUpdate(Player)
        };

        public override void FixupMeta(DataContext ctx) {
            Player = Get<MetaPlayerUpdate>(ctx);
        }

        protected override void Read(CelesteNetBinaryReader reader) {
            choice = reader.ReadInt32();
            gameStart = reader.ReadInt64();
        }

        protected override void Write(CelesteNetBinaryWriter writer) {
            writer.Write(choice);
            writer.Write(gameStart);
        }
    }
}
