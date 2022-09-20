using Celeste.Mod;
using Celeste.Mod.CelesteNet.Client;
using Celeste.Mod.CelesteNet.Client.Components;
using Celeste.Mod.CelesteNet.DataTypes;
using MonoMod.Utils;
using System;
using System.Collections.Generic;

namespace MadelineParty.Multiplayer.CelesteNet {
    public class CelesteNetBackend : MultiplayerBackend {
        private static readonly EverestModuleMetadata celesteNetDependency = new() { Name = "CelesteNet.Client", Version = new Version(2, 0, 0) };

        public override bool BackendInstalled() => Everest.Loader.DependencyLoaded(celesteNetDependency);

        public override bool BackendConnected() => CelesteNetClientModule.Instance?.Client?.Con != null;

        public override void Send(MultiplayerData data) {
            DynamicData.For(data).Set("Player", CelesteNetClientModule.Instance.Client.PlayerInfo);
            CelesteNetClientModule.Instance.Client?.Send(data as DataType);
        }

        public override PlayerInfo GetPlayer(uint id) {
            return new CelesteNetPlayerInfo(id);
        }

        public override uint CurrentPlayerID() => CelesteNetClientModule.Instance.Client.PlayerInfo.ID;

        public override List<PlayerInfo> GetPlayers() {
            throw new NotImplementedException();
            //return CelesteNetClientModule.Instance.Client.Data.Get.TryGetRef(id, out DataPlayerInfo value);
        }

        public override void LoadContent() {
            CelesteNetMadelinePartyComponent.handleAction = MultiplayerSingleton.Instance.Handle;
        }

        public override void SendChat(string msg) {
            // FIXME Temporary workaround, CelesteNet team should be putting out a new API for me eventually
            DataChat chat = new() { Text = msg, ID = 5 };
            CelesteNetClientModule.Instance.Context.Chat.Handle(CelesteNetClientModule.Instance.Context.Client.Con, chat);
        }
    }
}
