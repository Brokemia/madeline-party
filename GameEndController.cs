using System;
using System.Collections;
using Celeste;
using MadelineParty.Ghostnet;
using Monocle;

namespace MadelineParty
{
    public class GameEndController : Entity, IPauseUpdateGhostnetChat
    {
        private Level level;

        public override void Added(Scene scene)
        {
            base.Added(scene);
            level = SceneAs<Level>();
            Add(new Coroutine(GameEndRoutine()));
            AddTag(Tags.PauseUpdate);
            AddTag(Tags.FrozenUpdate);
        }

        private IEnumerator GameEndRoutine()
        {
            yield return 7f;
            level.OnEndOfFrame += delegate
            {
                Player player = level.Tracker.GetEntity<Player>();
                Leader.StoreStrawberries(player.Leader);
                level.Remove(player);
                level.UnloadLevel();

                level.Session.Level = MadelinePartyModule.START_ROOM;
                level.Session.RespawnPoint = level.GetSpawnPoint(new Microsoft.Xna.Framework.Vector2(level.Bounds.Left, level.Bounds.Top));

                level.LoadLevel(Player.IntroTypes.None);

                Leader.RestoreStrawberries(player.Leader);
            };
        }
    }
}
