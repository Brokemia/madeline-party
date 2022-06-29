using Celeste;
using Celeste.Mod.Entities;
using MadelineParty.Multiplayer;
using MadelineParty.Multiplayer.General;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadelineParty {

    [CustomEntity("madelineparty/beginModeTrigger")]
    public class BeginModeTrigger : Trigger {
        private string mode;

        public BeginModeTrigger(EntityData data, Vector2 offset) : base(data, offset) {
            mode = data.Attr("mode");
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);

            GameData.Instance.playerNumber = Scene.Tracker.GetEntity<PlayerNumberSelect>()?.Value ?? 1;
            if (GameData.Instance.playerNumber != 1) {
                MultiplayerSingleton.Instance.Send(new Party { respondingTo = -1, lookingForParty = (byte)GameData.Instance.playerNumber });
            }

            Level level = SceneAs<Level>();
            level.OnEndOfFrame += delegate {
                level.Teleport("Game_Lobby");
            };
        }

    }
}
