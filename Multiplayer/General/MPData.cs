using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadelineParty.Multiplayer.General {
    public interface MPData {
    }

    public class DieRoll : MPData {
        public uint ID;
        public int[] rolls;
    }

    public class MinigameEnd : MPData {
        public uint ID;
        // Can represent time, item thingies collected, or other
        public uint results;
    }

    public class MinigameStart : MPData {
        public uint ID;
        // The minigame selected
        public int choice;
        // The time to start the minigame
        public long gameStart;
    }

    public class MinigameStatus : MPData {
        public uint ID;
        // Can represent time, item thingies collected, or other
        public uint results;
    }

    public class MinigameVector2 : MPData {
        public uint ID;
        // Can represent touch switches etc
        public Vector2 vec;
        // Any extra data, used for on/off in touch switch game
        public int extra;
    }

    public class Party : MPData {
        public uint ID;
        // The size of the party being looked for
        public byte lookingForParty;
        public string version = MadelinePartyModule.Instance.Metadata.VersionString;
        public int playerSelectTrigger = -2;

        // If respondingTo == -1, it's a broadcast to anyone
        public int respondingTo;

        public bool partyHost = true;
    }

    public class PlayerChoice : MPData {
        public uint ID;
        public string choiceType;
        // For buttons, 0 = left, 1 = right
        // For Direction, UP = 0, DOWN = 1, LEFT = 2, RIGHT = 3
        public int choice;
    }

    public class RandomSeed : MPData {
        public uint ID;
        public uint turnOrderSeed;
        public uint tieBreakerSeed;
    }

    public class TiebreakerRolled : MPData {
        public uint ID;
        public Vector2 ButtonPosition;
    }

}
