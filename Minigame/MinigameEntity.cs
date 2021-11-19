using System.Collections;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod.CelesteNet.Client;
using MadelineParty.CelesteNet;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty {
    [Tracked(true)]
    public abstract class MinigameEntity : Trigger {
        protected Level level;
        protected int displayNum = -1;
        protected List<MTexture> diceNumbers;
        public static bool started;
        public bool completed;
        public static float startTime = -1;

        protected MinigameEntity(EntityData data, Vector2 offset) : base(data, offset) {
            diceNumbers = GFX.Game.GetAtlasSubtextures("decals/madelineparty/dicenumbers/dice_");
            Visible = true;
            Depth = -99999;
            AddTag(Tags.PauseUpdate);
            AddTag(Tags.FrozenUpdate);
        }

        public override void Render() {
            base.Render();
            if (displayNum > 0) {
                Player player = level.Tracker.GetEntity<Player>();
                if (player != null)
                    diceNumbers[displayNum - 1].Draw(player.Position + new Vector2(-24, -72));
            }
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            level = SceneAs<Level>();
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            if (!started) {
                Player player = level.Tracker.GetEntity<Player>();
                player.StateMachine.State = 11;
                // Stops the player from being moved by wind immediately
                // Probably saves you from Badeline too
                player.JustRespawned = true;
                startTime = level.RawTimeActive;
                started = true;
                Add(new Coroutine(Countdown()));
            }
        }

        private IEnumerator Countdown() {
            Player player = level.Tracker.GetEntity<Player>();
            player.StateMachine.State = 11;
            // Stops the player from being moved by wind immediately
            // Probably saves you from Badeline too
            player.JustRespawned = true;
            level.CanRetry = false;
            player.Speed = Vector2.Zero;
            yield return 1.2f;
            displayNum = 3;
            yield return 1f;
            displayNum = 2;
            yield return 1f;
            displayNum = 1;
            yield return 1f;
            displayNum = -1;
            player.StateMachine.State = 0;
            level.CanRetry = true;
            AfterStart();
        }

        protected virtual void AfterStart() {

        }

        protected void CelesteNetSendMinigameResults(uint results) {
            CelesteNetClientModule.Instance.Client?.Send(new MinigameEndData {
                Player = CelesteNetClientModule.Instance.Client.PlayerInfo,
                results = results
            });
        }

        protected void CelesteNetSendMinigameStatus(uint status) {
            CelesteNetClientModule.Instance.Client?.Send(new MinigameStatusData {
                Player = CelesteNetClientModule.Instance.Client.PlayerInfo,
                results = status
            });
        }

        protected void CelesteNetSendVector2(Vector2 vec, int extra = 0) {
            CelesteNetClientModule.Instance.Client?.Send(new MinigameVector2Data {
                Player = CelesteNetClientModule.Instance.Client.PlayerInfo,
                vec = vec,
                extra = extra
            });
        }

        public virtual void CelesteNetReceiveVector2(Vector2 vec, int extra) {

        }
    }
}
