using Celeste;
using Celeste.Mod;
using MadelineParty.Multiplayer;
using MadelineParty.Multiplayer.General;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty {

    [Tracked]
    public class PlayerNumberSelect : NumberSelect {

        private Level level;

        public PlayerNumberSelect(Vector2 position, Vector2[] nodes)
            : base(position, nodes, new int[]{ 1, 2, 3, 4 }) {
            GameData.Reset();
        }

        public PlayerNumberSelect(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.NodesOffset(offset)) {
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            level = scene as Level;
            level.CanRetry = false;
        }

        public override void Update() {
            base.Update();
            if (Value != 1 && !MultiplayerSingleton.Instance.BackendConnected()) {
                valueIdx = 0;
            }
        }

        protected override DashCollisionResults OnDashed(Player player, Vector2 direction) {
            Audio.Play("event:/game/general/wall_break_ice", Position);
            return DashCollisionResults.Bounce;
        }


        protected override DashCollisionResults OnPlus(Player player, Vector2 direction) {
            Audio.Play("event:/game/general/wall_break_ice", Position);

            if (MultiplayerSingleton.Instance.BackendConnected()) {
                IncremementValue();
            } else {
                Scene.Add(new PersistentMiniTextbox("MadelineParty_CelesteNet_Missing", persistent: false));
                Logger.Log("MadelineParty", "Multiplayer backend not installed or connected");
            }
            return base.OnPlus(player, direction);
        }

        protected override DashCollisionResults OnMinus(Player player, Vector2 direction) {
            Audio.Play("event:/game/general/wall_break_ice", Position);

            if (MultiplayerSingleton.Instance.BackendConnected()) {
                DecremementValue();
            } else {
                Scene.Add(new PersistentMiniTextbox("MadelineParty_CelesteNet_Missing", persistent: false));
                Logger.Log("MadelineParty", "Multiplayer backend not installed or connected");
            }
            return base.OnMinus(player, direction);
        }
    }
}