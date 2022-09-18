using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod;
using Celeste.Mod.Entities;
using MadelineParty.Multiplayer;
using MadelineParty.Multiplayer.General;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace MadelineParty {
    // Collect touch switches before your opponent gets them
    [CustomEntity("madelineparty/minigameSwitchGatherer")]
    [Tracked]
    public class MinigameSwitchGatherer : MinigameEntity {
        public static uint switchCount;
        public static List<Vector2> switchesOn = new List<Vector2>();
        public List<TouchSwitch> switches;
        public Coroutine endCoroutine;
        public MinigameScoreDisplay display;
        public Random rand = new Random();

        public MinigameSwitchGatherer(EntityData data, Vector2 offset) : base(data, offset) {
        }

        public static new void Load() {
            On.Celeste.TouchSwitch.TurnOn += TouchSwitch_TurnOn;
        }

        public static void Unload() {
            On.Celeste.TouchSwitch.TurnOn -= TouchSwitch_TurnOn;
        }

        private static void TouchSwitch_TurnOn(On.Celeste.TouchSwitch.orig_TurnOn orig, TouchSwitch self) {
            if(MadelinePartyModule.IsSIDMadelineParty(self.SceneAs<Level>().Session.Area.SID)) {
                MinigameSwitchGatherer gatherer;
                if ((gatherer = self.Scene.Tracker.GetEntity<MinigameSwitchGatherer>()) != null) {
                    if(switchesOn.Contains(self.Position)) {
                        gatherer.CollectSwitch(self.Position);
                    } else {
                        throw new Exception("Hit switch that shouldn't be on");
                    }
                    return;
                }
            }

            orig(self);
        }

        public void CollectSwitch(Vector2 pos) {
            switchCount++;
            GameData.Instance.minigameStatus[GameData.Instance.realPlayerID] = switchCount;

            TouchSwitch ts = switches.Find((s) => s.Position == pos);
            DeactivateSwitch(ts);
            for (int i = 0; i < 32; i++) {
                float num = Calc.Random.NextFloat((float)Math.PI * 2f);
                level.Particles.Emit(TouchSwitch.P_FireWhite, ts.Position + Calc.AngleToVector(num, 6f), num);
            }
            MultiplayerSingleton.Instance.Send(new MinigameStatus { results = switchCount });
            MultiplayerSingleton.Instance.Send(new MinigameVector2 { vec = pos, extra = -1 });
            if (GameData.Instance.gnetHost && switchesOn.Count == 0) {
                NewSwitcheDistribution(ts);
            }
        }

        private void NewSwitcheDistribution(TouchSwitch last) {
            TouchSwitch newSwitch;
            // Make sure not to get the same switch twice in a row
            while ((newSwitch = switches[rand.Next(switches.Count)]) == last) ;

            ActivateSwitch(newSwitch);

            // After 10 seconds, have a 50% chance to spawn a second switch
            if (level.RawTimeActive - startTime >= 10 && rand.NextFloat() > 0.5f) {
                TouchSwitch newSwitch2;
                // Make sure not to get the same switch twice in a row and not to get two switches in the same spot
                while ((newSwitch2 = switches[rand.Next(switches.Count)]) == last || newSwitch2 == newSwitch) ;

                ActivateSwitch(newSwitch2);
            }
            foreach (Vector2 switchPos in switchesOn) {
                MultiplayerSingleton.Instance.Send(new MinigameVector2 { vec = switchPos, extra = 1 });
            }
        }

        public override void Added(Scene scene) {
            base.Added(scene);
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            if(!started) {
                switchesOn.Clear();
            }
            switches = scene.Tracker.GetEntities<TouchSwitch>().ConvertAll((e) => (TouchSwitch) e);
            foreach (TouchSwitch ts in switches) {
                if (!switchesOn.Contains(ts.Position)) {
                    DeactivateSwitch(ts);
                }
            }
            // TODO It would be really nice if we could zoom out to see the whole level, but it would probably require HDleste to be released
            //Add(new Coroutine(level.ZoomTo(new Vector2(level.Bounds.Width/2f, level.Bounds.Height/2f), 8*22.5f/level.Bounds.Height, 3f)));
        }

        protected override void AfterStart() {
            base.AfterStart();
            // Reset timer so it starts at 30 instead of (30 - the time it takes to count down)
            startTime = level.RawTimeActive;
            level.Add(display = new MinigameScoreDisplay(this));
            if(GameData.Instance.gnetHost) {
                ActivateSwitch(switches[rand.Next(switches.Count)]);
            }
        }

        protected void ActivateSwitch(TouchSwitch ts) {
            switchesOn.Add(ts.Position);
            ts.Visible = true;
            ts.Active = true;
            ts.Collidable = true;
            Add(new Coroutine(ActivateSwitchCoroutine(ts)));
            MultiplayerSingleton.Instance.Send(new MinigameVector2 { vec = ts.Position, extra  = 1 });
        }

        protected IEnumerator ActivateSwitchCoroutine(TouchSwitch ts) {
            DynamicData switchData = new DynamicData(ts);
            switchData.Get<SoundSource>("touchSfx").Play("event:/game/general/touchswitch_any");
            switchData.Get<Sprite>("icon").Rate = 4;
            float ease = 1;
            while(ease > 0) {
                yield return null;
                switchData.Get<Sprite>("icon").Rate = 1 + 3 * ease;
                ease -= Engine.DeltaTime;
            }
            switchData.Get<Sprite>("icon").Rate = 1;
        }

        protected void DeactivateSwitch(TouchSwitch ts) {
            switchesOn.Remove(ts.Position);
            ts.Visible = false;
            ts.Active = false;
            ts.Collidable = false;
        }

        public override void MultiplayerReceiveVector2(Vector2 vec, int extra) {
            base.MultiplayerReceiveVector2(vec, extra);
            foreach (TouchSwitch ts in switches) {
                if (ts.Position == vec) {
                    if(extra < 0) {
                        DeactivateSwitch(ts);
                        for (int i = 0; i < 32; i++) {
                            float num = Calc.Random.NextFloat((float)Math.PI * 2f);
                            level.Particles.Emit(TouchSwitch.P_FireWhite, ts.Position + Calc.AngleToVector(num, 6f), num);
                        }
                        if (GameData.Instance.gnetHost && switchesOn.Count == 0) {
                            NewSwitcheDistribution(ts);
                        }
                    } else {
                        ActivateSwitch(ts);
                    }
                }
            }
        }

        public override void Update() {
            base.Update();
            if (started && level.RawTimeActive - startTime >= 30 && endCoroutine == null) {
                Add(endCoroutine = new Coroutine(FinishMinigame()));
            }
        }

        protected IEnumerator FinishMinigame() {
            completed = true;
            startTime = -1;
            started = false;
            didRespawn = false;
            level.CanRetry = false;
            Console.WriteLine("Touch Switch Count: " + switchCount);
            GameData.Instance.minigameResults.Add(new Tuple<int, uint>(GameData.Instance.realPlayerID, switchCount));
            MultiplayerSingleton.Instance.Send(new MinigameEnd { results = switchCount });

            yield return new SwapImmediately(EndMinigame(HIGHEST_WINS, () => {
                switchCount = 0;
            }));
        }
    }
}
