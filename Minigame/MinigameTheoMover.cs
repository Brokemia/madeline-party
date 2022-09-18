using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod;
using MadelineParty.Multiplayer;
using MadelineParty.Multiplayer.General;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty {
    // Move an infinite number of Theos from one place to another
    public class MinigameTheoMover : MinigameEntity {
        private const int THEOS_NEEDED = 5;
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
            // Reset timer so it starts at 0 instead of 4.2
            startTime = level.RawTimeActive;
            level.Add(display = new MinigameScoreDisplay(this, THEOS_NEEDED, false));
        }

        public override void Update() {
            base.Update();
            // If another player beat us to it
            if (started && endCoroutine == null && GameData.Instance.minigameResults.Count > 0) {
                Add(endCoroutine = new Coroutine(EndMinigame()));
            }
        }

        protected IEnumerator EndMinigame() {
            completed = true;
            MinigameTimeDisplay display = level.Entities.FindFirst<MinigameScoreDisplay>();
            if (display != null)
                display.finalTime = level.RawTimeActive - startTime;
            uint timeElapsed = theoCount < THEOS_NEEDED ? uint.MaxValue : (uint)((level.RawTimeActive - startTime) * 10000);
            startTime = -1;
            started = false;
            didRespawn = false;
            level.CanRetry = false;
            Console.WriteLine("Theo Count: " + theoCount);
            GameData.Instance.minigameResults.Add(new Tuple<int, uint>(GameData.Instance.realPlayerID, timeElapsed));
            MultiplayerSingleton.Instance.Send(new MinigameEnd { results = timeElapsed });

            yield return new SwapImmediately(EndMinigame(LOWEST_WINS, () => {
                theoCount = 0;
            }));
        }

        private void OnHoldable(Holdable h) {
            if (h.Entity is TheoCrystal) {
                TheoCrystal theoCrystal = h.Entity as TheoCrystal;
                theoCrystal.RemoveSelf();
                theoCount++;
                
                GameData.Instance.minigameStatus[GameData.Instance.realPlayerID] = theoCount;
                MultiplayerSingleton.Instance.Send(new MinigameStatus { results = theoCount });
                if (theoCount >= THEOS_NEEDED && endCoroutine == null) {
                    Add(endCoroutine = new Coroutine(EndMinigame()));
                } else {
                    level.Add(new TheoCrystal(theoRespawnPoint));
                }
            }
        }
    }
}
