using System;
namespace MadelineParty.Ghostnet
{
    public class MinigameStartData
    {
        public uint playerID;
        public string playerName;

        // The minigame selected
        public int choice;
        // The time to start the minigame
        public long gameStart;
    }
}
