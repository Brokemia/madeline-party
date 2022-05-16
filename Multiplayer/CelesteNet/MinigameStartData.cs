﻿using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;
using MadelineParty.Multiplayer.General;
using System.Collections.Generic;

namespace MadelineParty.Multiplayer.CelesteNet {
    public class MinigameStartData : DataType<MinigameStartData>, MultiplayerData {
        static MinigameStartData() {
            DataID = "mPartyMinigameStart";
        }

        public DataPlayerInfo Player;

        private MinigameStart data;

        public MinigameStart Data {
            get {
                data.ID = Player.ID;
                return data;
            }
        }

        public void Initialize(MPData args) => data = args as MinigameStart;

        public override MetaType[] GenerateMeta(DataContext ctx)
        => new MetaType[] {
            new MetaPlayerUpdate(Player)
        };

        public override void FixupMeta(DataContext ctx) {
            Player = Get<MetaPlayerUpdate>(ctx);
        }

        protected override void Read(CelesteNetBinaryReader reader) {
            data.choice = reader.ReadInt32();
            data.gameStart = reader.ReadInt64();
        }

        protected override void Write(CelesteNetBinaryWriter writer) {
            writer.Write(data.choice);
            writer.Write(data.gameStart);
        }
    }
}
