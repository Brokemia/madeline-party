using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadelineParty.Multiplayer.General {
    public abstract class MPData {
        public uint ID;
        public string DisplayName;
    }

    public class MinigameMenu : MPData {
        public int selection;
    }

    public class MinigameReady : MPData {
        public int player;
    }

    public class UseItem : MPData {
        public int player;
        public int itemIdx;
    }

    public class UseItemMenu : MPData {
        public int player;
        public int index;
    }

    public class DieRoll : MPData {
        public int[] rolls;
    }

    public class MinigameEnd : MPData {
        // Can represent time, item thingies collected, or other
        public uint results;
    }

    public class MinigameStart : MPData {
        // The minigame selected
        public string choice;
        // The time to start the minigame
        public long gameStart;
    }

    public class MinigameStatus : MPData {
        // Can represent time, item thingies collected, or other
        public uint results;
    }

    public class MinigameVector2 : MPData {
        // Can represent touch switches etc
        public Vector2 vec;
        // Any extra data, used for on/off in touch switch game
        public int extra;
    }

    public class Party : MPData {
        // The size of the party being looked for
        public byte lookingForParty;
        public string version = MadelinePartyModule.Instance.Metadata.VersionString;
        public int playerSelectTrigger = -2;

        // If respondingTo == -1, it's a broadcast to anyone
        public int respondingTo;

        public bool partyHost = true;
    }

    public class PlayerChoice : MPData {
        public string choiceType;
        // For buttons, 0 = left, 1 = right
        // For Direction, UP = 0, DOWN = 1, LEFT = 2, RIGHT = 3
        public int choice;
    }

    public class RandomSeed : MPData {
        public uint turnOrderSeed;
        public uint tieBreakerSeed;
    }

    public class TiebreakerRolled : MPData {
        public Vector2 ButtonPosition;
    }

}
