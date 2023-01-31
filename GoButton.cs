using Celeste;
using Celeste.Mod.Entities;
using MadelineParty.Multiplayer;
using MadelineParty.Multiplayer.General;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty {
    [CustomEntity("madelineparty/goButton")]
    class GoButton : Solid {
        private static MTexture texture = GFX.Game["objects/madelineparty/numberselect/go"];

        public GoButton(EntityData data, Vector2 offset) : base(data.Position + offset, 40, 40, true) {
            OnDashCollide = OnDashed;
            SurfaceSoundIndex = SurfaceIndex.TileToIndex['3'];
            AddTag(Tags.PauseUpdate);
            AddTag(Tags.FrozenUpdate);
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            MultiplayerSingleton.Instance.RegisterUniqueHandler<PlayerChoice>("GoButton", HandlePlayerChoice);
        }

        public override void Render() {
            texture.Draw(Position);
            base.Render();
        }

        private void HandlePlayerChoice(MPData data) {
            if (data is not PlayerChoice playerChoice) return;
            // If another player in our party has changed the turn count
            if (GameData.Instance.celestenetIDs.Contains(playerChoice.ID) && playerChoice.ID != MultiplayerSingleton.Instance.CurrentPlayerID() && "GOBUTTON".Equals(playerChoice.choiceType)) {
                OnDashed(SceneAs<Level>().Tracker.GetEntity<Player>(), default);
            }
        }

        private DashCollisionResults OnDashed(Player player, Vector2 direction) {
            Level level = SceneAs<Level>();
            MadelinePartyModule.SaveData.GamesStarted++;
            if(GameData.Instance.celesteNetHost) {
                MultiplayerSingleton.Instance.Send(new PlayerChoice { choiceType = "GOBUTTON" });
            }
            level.OnEndOfFrame += delegate {
                Leader.StoreStrawberries(player.Leader);
                level.Remove(player);
                level.UnloadLevel();

                level.Session.Level = GameData.Instance.board;
                switch (GameData.Instance.realPlayerID) {
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
