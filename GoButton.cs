using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty {
    [CustomEntity("madelineparty/goButton")]
    class GoButton : Solid {
        private static MTexture texture = GFX.Game["objects/madelineparty/numberselect/go"];

        public GoButton(EntityData data, Vector2 offset) : base(data.Position + offset, 48, 48, true) {
            OnDashCollide = OnDashed;
            SurfaceSoundIndex = SurfaceIndex.TileToIndex['3'];
            AddTag(Tags.PauseUpdate);
            AddTag(Tags.FrozenUpdate);
        }

        public override void Render() {
            texture.Draw(Position);
            base.Render();
        }

        private DashCollisionResults OnDashed(Player player, Vector2 direction) {
            Level level = SceneAs<Level>();
            level.OnEndOfFrame += delegate {
                Leader.StoreStrawberries(player.Leader);
                level.Remove(player);
                level.UnloadLevel();

                level.Session.Level = "Game_MainRoom";
                switch (GameData.realPlayerID) {
                    case 0:
                        level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(level.Bounds.Left, level.Bounds.Top));
                        break;
                    case 1:
                        level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(level.Bounds.Right, level.Bounds.Top));
                        break;
                    case 2:
                        level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(level.Bounds.Left, level.Bounds.Bottom));
                        break;
                    case 3:
                        level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(level.Bounds.Right, level.Bounds.Bottom));
                        break;
                }
                level.LoadLevel(Player.IntroTypes.None);

                Leader.RestoreStrawberries(player.Leader);
            };
            return DashCollisionResults.Rebound;
        }

    }
}
