using System;
using System.IO;
using Celeste.Mod.Ghost.Net;
using Celeste.Mod;
using Monocle;

namespace MadelineParty.Ghostnet
{
    public class MadelinePartyChunk
    {
        // The size of the party being looked for
        public byte lookingForParty;
        public string version = MadelinePartyModule.Instance.Metadata.VersionString;
        public int playerSelectTrigger = -2;

        // If respondingTo == playerID, it's a broadcast to anyone
        public uint respondingTo;
        public uint playerID;
        public string playerName;

        public bool partyHost = true;
    }
}
