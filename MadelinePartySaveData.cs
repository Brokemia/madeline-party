using Celeste.Mod;
using MadelineParty.Board;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadelineParty
{
    public class MadelinePartySaveData : EverestModuleSaveData {
        public int Version { get; set; } = 1;

        public int BerriesCollected { get; set; }
        public int HeartsCollected { get; set; }
        public int BerryRecord { get; set; }
        public int HeartRecord { get; set; }
        public int GamesStarted { get; set; }
        public int GamesWon { get; set; }
        public int GamesFinished { get; set; }
        public int MinigamesPlayed { get; set; }
        public int MinigamesWon { get; set; }
        public int TurnsPlayed { get; set; }
        public Dictionary<string, int> ItemsUsed { get; set; } = new();
        public Dictionary<string, int> ItemsBought { get; set; } = new();
        public Dictionary<BoardController.BoardSpaceType, int> SpacesHit { get; set; } = new();
        // Maps character ID to number of times playing that character
        public Dictionary<int, int> CharacterChoices { get; set; } = new();
    }
}
