using Celeste;
using System.Collections.Generic;

namespace MadelineParty {
    public abstract class ModeManager {
        // TODO register modes somehow, rather than hardcoding them in BeginModeTrigger
        public abstract string Mode { get; }
        
        public static ModeManager Instance { get; set; }

        public void AfterPlayerSelect(Level level) {
            // Stats on how many times we've picked each character
            if (!MadelinePartyModule.SaveData.CharacterChoices.TryGetValue(GameData.Instance.realPlayerID, out int value)) {
                value = 0;
            }
            MadelinePartyModule.SaveData.CharacterChoices[GameData.Instance.realPlayerID] = value + 1;

            SendToPostPlayerSelect(level);
        }

        protected abstract void SendToPostPlayerSelect(Level level);

        public void AfterPlayersRanked(Level level) {
            level.OnEndOfFrame += delegate {
                GameData.Instance.minigameResults.Clear();
                GameData.Instance.minigameStatus.Clear();
                SendToPostPlayerRanking(level);
            };
        }

        protected abstract void SendToPostPlayerRanking(Level level);

        public abstract void DistributeMinigameRewards(List<int> winners);

        public abstract void AfterMinigameChosen(Level level);
    }
}
