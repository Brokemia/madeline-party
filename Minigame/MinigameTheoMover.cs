using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod;
using Celeste.Mod.CelesteNet.Client;
using MadelineParty.CelesteNet;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty {
    // Move an infinite number of Theos from one place to another
    public class MinigameTheoMover : MinigameEntity {
        protected Vector2 theoRespawnPoint;
        public static uint theoCount;
        public Coroutine endCoroutine;
        public MinigameScoreDisplay display;

        public MinigameTheoMover(EntityData data, Vector2 offset) : base(data, offset) {
            theoRespawnPoint = data.Nodes[0];
            Add(new HoldableCollider(OnHoldable));
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            level.Add(new TheoCrystal(theoRespawnPoint));
        }

        protected override void AfterStart() {
            base.AfterStart();
            // Reset timer so it starts at 30 instead of (30 - the time it takes to count down)
            startTime = level.RawTimeActive;
            level.Add(display = new MinigameScoreDisplay(this));
        }

        public override void Update() {
            base.Update();
            if (level.RawTimeActive - startTime >= 30 && endCoroutine == null) {
                Add(endCoroutine = new Coroutine(EndMinigame()));
            }
        }

        protected IEnumerator EndMinigame() {
            Player player = level.Tracker.GetEntity<Player>();
            // This check is probably unnecessary, but I left it in for safety
            while (player == null) {
                yield return null;
                player = level.Tracker.GetEntity<Player>();
            }
            completed = true;
            // Freeze the player so they can't do any more Theo throwing until everyone else is done
            player.StateMachine.State = Player.StFrozen;
            player.Speed = Vector2.Zero;
            startTime = -1;
            started = false;
            didRespawn = false;
            level.CanRetry = false;
            Console.WriteLine("Theo Count: " + theoCount);
            GameData.minigameResults.Add(new Tuple<int, uint>(GameData.realPlayerID, theoCount));
            if (MadelinePartyModule.IsCelesteNetInstalled()) {
                CelesteNetSendMinigameResults(theoCount);
            }

            yield return new SwapImmediately(EndMinigame(player, HIGHEST_WINS, () => {
                theoCount = 0;
            }));
        }

        private void OnHoldable(Holdable h) {
            if (h.Entity is TheoCrystal) {
                TheoCrystal theoCrystal = h.Entity as TheoCrystal;
                theoCrystal.RemoveSelf();
                theoCount++;
                GameData.minigameStatus[GameData.realPlayerID] = theoCount;
                if (MadelinePartyModule.IsCelesteNetInstalled()) {
                    CelesteNetSendMinigameStatus(theoCount);
                }
                level.Add(new TheoCrystal(theoRespawnPoint));
            }
        }
    }
}
