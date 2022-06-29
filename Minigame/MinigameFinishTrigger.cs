using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using MadelineParty.Multiplayer;
using MadelineParty.Multiplayer.General;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty {
    public class MinigameFinishTrigger : MinigameEntity {

        public MinigameTimeDisplay timer;

        public MinigameFinishTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        }

        protected override void AfterStart() {
            base.AfterStart();
            // Reset timer so it starts at 0 instead of 4.2
            startTime = level.RawTimeActive;
            level.Add(timer = new MinigameTimeDisplay(this));
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);
            // Stop problems with the player entering the trigger multiple times
            if (GameData.Instance.minigameResults.Exists((obj) => obj.Item1 == GameData.Instance.realPlayerID))
                return;
            completed = true;
            MinigameTimeDisplay display = level.Entities.FindFirst<MinigameTimeDisplay>();
            if (display != null)
                display.finalTime = level.RawTimeActive - startTime;
            float timeElapsed = (level.RawTimeActive - startTime) * 10000;
            startTime = -1;
            started = false;
            didRespawn = false;
            level.CanRetry = false;
            foreach(SyncedKevin kevin in level.Tracker.GetEntities<SyncedKevin>()) {
                kevin.deactivated = true;
            }
            GameData.Instance.minigameResults.Add(new Tuple<int, uint>(GameData.Instance.realPlayerID, (uint)timeElapsed));
            MultiplayerSingleton.Instance.Send(new MinigameEnd { results = (uint)timeElapsed });

            Add(new Coroutine(EndMinigame(LOWEST_WINS, () => {})));
        }
    }
}
