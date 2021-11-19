using System;
using System.IO;
using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;
using Microsoft.Xna.Framework;

namespace MadelineParty.CelesteNet {
    public class MinigameVector2Data : DataType<MinigameVector2Data> {
        static MinigameVector2Data() {
            DataID = "mPartyMinigameVector2";
        }
        public DataPlayerInfo Player;

        // Can represent touch switches etc
        public Vector2 vec;

        // Any extra data, used for on/off in touch switch game
        public int extra = 0;

        public override MetaType[] GenerateMeta(DataContext ctx)
        => new MetaType[] {
            new MetaPlayerUpdate(Player)
        };

        public override void FixupMeta(DataContext ctx) {
            Player = Get<MetaPlayerUpdate>(ctx);
        }

        public override void Read(DataContext ctx, BinaryReader reader) {
            vec = reader.ReadVector2();
            extra = reader.ReadInt32();
        }

        public override void Write(DataContext ctx, BinaryWriter writer) {
            writer.Write(vec);
            writer.Write(extra);
        }
    }
}
