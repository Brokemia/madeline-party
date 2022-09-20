using Celeste;
using Celeste.Mod.Entities;
using MadelineParty.Multiplayer;
using MadelineParty.Multiplayer.General;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty {

    [CustomEntity("madelineparty/turnCountSelect")]
    public class TurnCountSelect : NumberSelect {

        private Level level;

        public TurnCountSelect(Vector2 position, Vector2[] nodes)
            : base(position, nodes, new int[]{ 10, 15, 20, 25, 30, 35, 40, 45, 50, 1, 2, 5 }) {
        }

        public TurnCountSelect(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.NodesOffset(offset)) {
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            level = scene as Level;
            level.CanRetry = false;
            MultiplayerSingleton.Instance.RegisterUniqueHandler<PlayerChoice>("TurnCountSelect", HandlePlayerChoice);
        }

        private void HandlePlayerChoice(MPData data) {
            if (data is not PlayerChoice playerChoice) return;
            // If another player in our party has changed the turn count
            if (GameData.Instance.celestenetIDs.Contains(playerChoice.ID) && playerChoice.ID != MultiplayerSingleton.Instance.CurrentPlayerID() && playerChoice.choiceType.Equals("TURNCOUNTSELECT")) {
                valueIdx = playerChoice.choice;
                GameData.Instance.maxTurns = Value;
            }
        }

        protected override DashCollisionResults OnDashed(Player player, Vector2 direction) {
            Audio.Play("event:/game/general/wall_break_ice", Position);
            return DashCollisionResults.Bounce;
        }

        protected override DashCollisionResults OnPlus(Player player, Vector2 direction) {
            Audio.Play("event:/game/general/wall_break_ice", Position);
            IncremementValue();
            GameData.Instance.maxTurns = Value;
            MultiplayerSingleton.Instance.Send(new PlayerChoice { choiceType = "TURNCOUNTSELECT", choice = valueIdx });
            return base.OnPlus(player, direction);
        }

        protected override DashCollisionResults OnMinus(Player player, Vector2 direction) {
            Audio.Play("event:/game/general/wall_break_ice", Position);
            DecremementValue();
            GameData.Instance.maxTurns = Value;
            MultiplayerSingleton.Instance.Send(new PlayerChoice { choiceType = "TURNCOUNTSELECT", choice = valueIdx });
            return base.OnMinus(player, direction);
        }
    }
}