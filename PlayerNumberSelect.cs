using System;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod;
using MadelineParty.Multiplayer;
using MadelineParty.Multiplayer.General;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty {

    public class PlayerNumberSelect : Solid {
        public const int MAXPLAYERS = 4;

        public class PlayerNumberPlus : Solid {
            private PlayerNumberSelect parent;
            private MTexture texture;

            public PlayerNumberPlus(Vector2 position, PlayerNumberSelect parent) : base(position, 16, 16, true) {
                this.parent = parent;
                OnDashCollide = OnDashed;
                SurfaceSoundIndex = SurfaceIndex.TileToIndex['3'];
                texture = GFX.Game["objects/madelineparty/playernumberselect/plus"];
                AddTag(Tags.PauseUpdate);
                AddTag(Tags.FrozenUpdate);
            }

            public override void Render() {
                texture.Draw(this.Position);
                base.Render();
            }

            public void Break(Vector2 from, Vector2 direction, bool playSound = true, bool playDebrisSound = true) {
                if (playSound) {
                    Audio.Play("event:/game/general/wall_break_ice", Position);
                }

                if (MultiplayerSingleton.Instance.BackendConnected()) {
                    parent.playerNumber++;
                    if (parent.playerNumber > MAXPLAYERS) {
                        parent.playerNumber = 1;
                    }
                } else {
                    Logger.Log("MadelineParty", "CelesteNet not installed or connected");
                }
            }

            private DashCollisionResults OnDashed(Player player, Vector2 direction) {
                Break(player.Center, direction);
                return DashCollisionResults.Rebound;
            }
        }

        public class PlayerNumberMinus : Solid {
            private PlayerNumberSelect parent;
            private MTexture texture;

            public PlayerNumberMinus(Vector2 position, PlayerNumberSelect parent) : base(position, 16, 16, true) {
                this.parent = parent;
                OnDashCollide = OnDashed;
                SurfaceSoundIndex = SurfaceIndex.TileToIndex['3'];
                texture = GFX.Game["objects/madelineparty/playernumberselect/minus"];
                AddTag(Tags.PauseUpdate);
                AddTag(Tags.FrozenUpdate);
            }

            public override void Render() {
                texture.Draw(this.Position);
                base.Render();
            }

            public void Break(Vector2 from, Vector2 direction, bool playSound = true, bool playDebrisSound = true) {
                if (playSound) {
                    Audio.Play("event:/game/general/wall_break_ice", Position);
                }

                if (MultiplayerSingleton.Instance.BackendConnected()) {
                    parent.playerNumber--;
                    if (parent.playerNumber < 1) {
                        parent.playerNumber = MAXPLAYERS;
                    }
                } else {
                    Logger.Log("MadelineParty", "CelesteNet not installed or connected");
                }
            }

            private DashCollisionResults OnDashed(Player player, Vector2 direction) {
                Break(player.Center, direction);
                return DashCollisionResults.Rebound;
            }
        }

        private Level level;

        private const string texturePrefix = "objects/madelineparty/playernumberselect/playernumber";

        private PlayerNumberPlus plus;
        private PlayerNumberMinus minus;

        public int playerNumber = 1;

        private MTexture[] textures = new MTexture[] { null, null, null, null };

        public PlayerNumberSelect(Vector2 position, Vector2[] nodes)
            : base(position, 32, 32, true) {
            GameData.Reset();
            if (nodes.Length == 0) {
                plus = new PlayerNumberPlus(position + new Vector2(-60, 0), this);
                minus = new PlayerNumberMinus(position + new Vector2(60 + 16, 0), this);
            } else if (nodes.Length == 1) {
                plus = new PlayerNumberPlus(nodes[0], this);
                minus = new PlayerNumberMinus(position + new Vector2(60, 0), this);
            } else {
                plus = new PlayerNumberPlus(nodes[0], this);
                minus = new PlayerNumberMinus(nodes[1], this);
            }
            textures[0] = GFX.Game[texturePrefix + "00"];
            textures[1] = GFX.Game[texturePrefix + "01"];
            textures[2] = GFX.Game[texturePrefix + "02"];
            textures[3] = GFX.Game[texturePrefix + "03"];
            OnDashCollide = OnDashed;
            SurfaceSoundIndex = SurfaceIndex.TileToIndex['3'];
            AddTag(Tags.PauseUpdate);
            AddTag(Tags.FrozenUpdate);
        }

        public PlayerNumberSelect(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Nodes) {
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            level = scene as Level;
            level.CanRetry = false;
            scene.Add(plus);
            scene.Add(minus);
        }

        public override void Render() {
            textures[playerNumber - 1].Draw(this.Position);
            base.Render();
        }

        public override void Update() {
            base.Update();
            if (playerNumber != 1 && !MultiplayerSingleton.Instance.BackendConnected()) {
                playerNumber = 1;
            }

        }

        public void Break(Vector2 from, Vector2 direction, bool playSound = true, bool playDebrisSound = true) {
            if (playSound) {
                Audio.Play("event:/game/general/wall_break_ice", Position);
            }
            Player player = Scene.Tracker.GetEntity<Player>();
            GameData.playerNumber = playerNumber;
            if (playerNumber != 1) {
                MultiplayerSingleton.Instance.Send(new Party { respondingTo = -1, lookingForParty = (byte)GameData.playerNumber });
            }

            level.OnEndOfFrame += delegate {
                Leader.StoreStrawberries(player.Leader);
                level.Remove(player);
                level.UnloadLevel();

                level.Session.Level = "Game_Lobby";
                level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(level.Bounds.Left, level.Bounds.Top));
                level.LoadLevel(Player.IntroTypes.None);

                Leader.RestoreStrawberries(level.Tracker.GetEntity<Player>().Leader);
            };
        }

        private DashCollisionResults OnDashed(Player player, Vector2 direction) {
            Break(player.Center, direction);
            return DashCollisionResults.Rebound;
        }
    }
}