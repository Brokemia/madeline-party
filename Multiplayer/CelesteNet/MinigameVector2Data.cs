using System;
using System.Collections.Generic;
using System.IO;
using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;
using Microsoft.Xna.Framework;

namespace MadelineParty.Multiplayer.CelesteNet {
    public class MinigameVector2Data : DataType<MinigameVector2Data>, MultiplayerData {
        static MinigameVector2Data() {
            DataID = "mPartyMinigameVector2";
        }
        public DataPlayerInfo Player;

        // Can represent touch switches etc
        public Vector2 vec;

        // Any extra data, used for on/off in touch switch game
        public int extra = 0;

        public void Initialize(Dictionary<string, object> args) {
            vec = args.OrDefault("vec", vec);
            extra = args.OrDefault("extra", extra);
        }

        public override MetaType[] GenerateMeta(DataContext ctx)
        => new MetaType[] {
            new MetaPlayerUpdate(Player)
        };

        public override void FixupMeta(DataContext ctx) {
            Player = Get<MetaPlayerUpdate>(ctx);
        }

        protected override void Read(CelesteNetBinaryReader reader) {
            vec = reader.ReadVector2();
            extra = reader.ReadInt32();
        }

        protected override void Write(CelesteNetBinaryWriter writer) {
            writer.Write(vec);
            writer.Write(extra);
        }
    }
}
