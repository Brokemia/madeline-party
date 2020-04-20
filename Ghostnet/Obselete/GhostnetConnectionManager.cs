using System;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty
{
    [Obsolete("Replaced by a hook to Player.Update", true)]
    public class GhostnetConnectionManager : Entity
    {
        private Level level;

        public override void Added(Scene scene)
        {
            base.Added(scene);
            level = SceneAs<Level>();
        }

        public override void Update()
        {
            base.Update();
            if (MadelinePartyModule.ghostnetInstalled)
            {
                GhostnetUpdate();
            }

        }

        private void GhostnetUpdate()
        {
            if (MadelinePartyModule.ghostnetConnected && !MadelinePartyModule.connectionSetup)
            {
                MadelinePartyModule.Instance.GhostnetConnectionSetup();
            }
            else if (!MadelinePartyModule.ghostnetConnected && MadelinePartyModule.connectionSetup)
            {
                MadelinePartyModule.connectionSetup = false;
            }

            // If the player disconnects from a multiplayer game
            if (GameData.playerNumber != 1 && !MadelinePartyModule.ghostnetConnected)
            {
                Player player = level.Tracker.GetEntity<Player>();
                level.OnEndOfFrame += delegate
                {
                    Leader.StoreStrawberries(player.Leader);
                    level.Remove(player);
                    level.UnloadLevel();

                    level.Session.Level = MadelinePartyModule.START_ROOM;
                    level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(level.Bounds.Left, level.Bounds.Top));
                    level.LoadLevel(Player.IntroTypes.None);

                    Leader.RestoreStrawberries(level.Tracker.GetEntity<Player>().Leader);
                };
            }
        }
    }
}
