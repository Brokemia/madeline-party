using System;
using Celeste;
using MadelineParty.Entities;
using MadelineParty.Multiplayer;
using MadelineParty.Multiplayer.General;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty
{
    public class MinigameFinishTrigger : MinigameEntity {
        public MinigameFinishTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        }

        protected override void AfterStart() {
            base.AfterStart();
            level.Add(new MinigameTimeDisplay(this));
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);
            // Stop problems with the player entering the trigger multiple times
            if (GameData.Instance.minigameResults.Exists((obj) => obj.Item1 == GameData.Instance.realPlayerID))
                return;
            completed = true;
            MinigameTimeDisplay display = level.Entities.FindFirst<MinigameTimeDisplay>();
            if (display != null)
                display.finalTime = level.RawTimeActive - Data.StartTime;
            float timeElapsed = (level.RawTimeActive - Data.StartTime) * 10000;
            level.CanRetry = false;
            foreach(GroupedKevin kevin in level.Tracker.GetEntities<GroupedKevin>()) {
                kevin.deactivated = true;
            }
            GameData.Instance.minigameResults.Add(new Tuple<int, uint>(GameData.Instance.realPlayerID, (uint)timeElapsed));
            MultiplayerSingleton.Instance.Send(new MinigameEnd { results = (uint)timeElapsed });

            Add(new Coroutine(EndMinigame(LOWEST_WINS, () => {})));
        }
    }
}
