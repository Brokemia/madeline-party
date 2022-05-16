using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;
using MadelineParty.Multiplayer.General;
using System.Collections.Generic;

namespace MadelineParty.Multiplayer.CelesteNet {
    public class PartyData : DataType<PartyData>, MultiplayerData {
        static PartyData() {
            DataID = "mPartyParty";
        }

        public DataPlayerInfo Player;

        private Party data;

        public Party Data {
            get {
                data.ID = Player.ID;
                return data;
            }
        }

        public void Initialize(MPData args) => data = args as Party;

        public override MetaType[] GenerateMeta(DataContext ctx)
        => new MetaType[] {
            new MetaPlayerUpdate(Player)
        };

        public override void FixupMeta(DataContext ctx) {
            Player = Get<MetaPlayerUpdate>(ctx);
        }

        protected override void Read(CelesteNetBinaryReader reader) {
            data.lookingForParty = reader.ReadByte();
            data.version = reader.ReadNetString();
            data.playerSelectTrigger = reader.ReadInt32();
            data.respondingTo = reader.ReadInt32();
            data.partyHost = reader.ReadBoolean();
        }

        protected override void Write(CelesteNetBinaryWriter writer) {
            writer.Write(data.lookingForParty);
            writer.WriteNetString(data.version);
            writer.Write(data.playerSelectTrigger);
            writer.Write(data.respondingTo);
            writer.Write(data.partyHost);
        }
    }
}
