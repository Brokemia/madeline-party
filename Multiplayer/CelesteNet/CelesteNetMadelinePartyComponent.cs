using System;
using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.Client;
using Microsoft.Xna.Framework;
using MadelineParty.Multiplayer.General;
using MadelineParty.Multiplayer.CelesteNet.Data;

namespace MadelineParty.Multiplayer.CelesteNet {
    public class CelesteNetMadelinePartyComponent : CelesteNetGameComponent {

        public static Action<MPData> handleAction;

        public CelesteNetMadelinePartyComponent(CelesteNetClientContext context, Game game) : base(context, game) {
            Visible = false;
        }

        public void Handle(CelesteNetConnection con, PartyData data) {
            handleAction.Invoke(data.Data);
        }
    
        public void Handle(CelesteNetConnection con, DieRollData data) {
            handleAction.Invoke(data.Data);
        }

        public void Handle(CelesteNetConnection con, PlayerChoiceData data) {
            handleAction.Invoke(data.Data);
        }

        public void Handle(CelesteNetConnection con, MinigameStartData data) {
            handleAction.Invoke(data.Data);
        }

        public void Handle(CelesteNetConnection con, MinigameEndData data) {
            handleAction.Invoke(data.Data);
        }

        public void Handle(CelesteNetConnection con, MinigameStatusData data) {
            handleAction.Invoke(data.Data);
        }

        public void Handle(CelesteNetConnection con, MinigameVector2Data data) {
            handleAction.Invoke(data.Data);
        }

        public void Handle(CelesteNetConnection con, RandomSeedData data) {
            handleAction.Invoke(data.Data);
        }

        public void Handle(CelesteNetConnection con, TiebreakerRolledData data) {
            handleAction.Invoke(data.Data);
        }

        public void Handle(CelesteNetConnection con, UseItemData data) {
            handleAction.Invoke(data.Data);
        }

        public void Handle(CelesteNetConnection con, UseItemMenuData data) {
            handleAction.Invoke(data.Data);
        }

        public void Handle(CelesteNetConnection con, MinigameReadyData data) {
            handleAction.Invoke(data.Data);
        }

        public void Handle(CelesteNetConnection con, MinigameMenuData data) {
            handleAction.Invoke(data.Data);
        }

        public void Handle(CelesteNetConnection con, SyncedKevinHitData data) {
            handleAction.Invoke(data.Data);
        }

        public void Handle(CelesteNetConnection con, BlockCrystalUpdateData data) {
            handleAction.Invoke(data.Data);
        }
    }
}
