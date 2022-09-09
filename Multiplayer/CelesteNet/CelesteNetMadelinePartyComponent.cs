using System;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.Client;
using Celeste.Mod.CelesteNet.DataTypes;
using Logger = Celeste.Mod.Logger;
using Microsoft.Xna.Framework;
using Monocle;
using MadelineParty.Multiplayer.General;

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
    }
}
