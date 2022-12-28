using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;
using MadelineParty.Multiplayer.General;

namespace MadelineParty.Multiplayer.CelesteNet.Data {
    public class PartyData : DataType<PartyData>, MultiplayerData {
        static PartyData() {
            DataID = "mPartyParty";
        }

        public DataPlayerInfo Player;

        private Party data;

        public Party Data {
            get {
                data.ID = Player.ID;
                data.DisplayName = Player.DisplayName;
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
            data = new() {
                lookingForParty = reader.ReadByte(),
                desiredMode = reader.ReadNetString(),
                version = reader.ReadNetString(),
                playerSelectTrigger = reader.ReadInt32(),
                respondingTo = reader.ReadInt32(),
                partyHost = reader.ReadBoolean()
            };
        }

        protected override void Write(CelesteNetBinaryWriter writer) {
            writer.Write(data.lookingForParty);
            writer.WriteNetString(data.desiredMode);
            writer.WriteNetString(data.version);
            writer.Write(data.playerSelectTrigger);
            writer.Write(data.respondingTo);
            writer.Write(data.partyHost);
        }
    }
}
