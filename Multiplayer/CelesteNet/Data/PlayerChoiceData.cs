using System.Collections.Generic;
using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;
using MadelineParty.Multiplayer.General;

namespace MadelineParty.Multiplayer.CelesteNet.Data {
    public class PlayerChoiceData : DataType<PlayerChoiceData>, MultiplayerData {
        static PlayerChoiceData() {
            DataID = "mPartyPlayerChoice";
        }

        public DataPlayerInfo Player;

        private PlayerChoice data;

        public PlayerChoice Data {
            get {
                data.ID = Player.ID;
                data.DisplayName = Player.DisplayName;
                return data;
            }
        }

        public void Initialize(MPData args) => data = args as PlayerChoice;

        public override MetaType[] GenerateMeta(DataContext ctx)
        => new MetaType[] {
            new MetaPlayerUpdate(Player)
        };

        public override void FixupMeta(DataContext ctx) {
            Player = Get<MetaPlayerUpdate>(ctx);
        }

        protected override void Read(CelesteNetBinaryReader reader) {
            data = new();
            data.choiceType = reader.ReadString();
            data.choice = reader.ReadInt32();
        }

        protected override void Write(CelesteNetBinaryWriter writer) {
            writer.Write(data.choiceType);
            writer.Write(data.choice);
        }
    }
}
