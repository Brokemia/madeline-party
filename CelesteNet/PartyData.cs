using System;
using System.IO;
using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;

namespace MadelineParty.CelesteNet {
    public class PartyData : DataType<PartyData> {
        static PartyData() {
            DataID = "mPartyParty";
        }

        // The size of the party being looked for
        public byte lookingForParty;
        public string version = MadelinePartyModule.Instance.Metadata.VersionString;
        public int playerSelectTrigger = -2;

        // If respondingTo == playerID, it's a broadcast to anyone
        public uint respondingTo;
        public DataPlayerInfo Player;

        public bool partyHost = true;

        public override MetaType[] GenerateMeta(DataContext ctx)
        => new MetaType[] {
            new MetaPlayerUpdate(Player)
        };

        public override void FixupMeta(DataContext ctx) {
            Player = Get<MetaPlayerUpdate>(ctx);
        }

        protected override void Read(CelesteNetBinaryReader reader) {
            lookingForParty = reader.ReadByte();
            version = reader.ReadNetString();
            playerSelectTrigger = reader.ReadInt32();
            respondingTo = reader.ReadUInt32();
            partyHost = reader.ReadBoolean();
        }

        protected override void Write(CelesteNetBinaryWriter writer) {
            writer.Write(lookingForParty);
            writer.WriteNetString(version);
            writer.Write(playerSelectTrigger);
            writer.Write(respondingTo);
            writer.Write(partyHost);
        }
    }
}
