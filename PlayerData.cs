using System;
using System.Collections.Generic;

namespace MadelineParty
{
    public class PlayerData : IComparable
    {
        public uint CelesteNetID = uint.MaxValue;
        public int TurnOrder;
        public int TokenSelected;
        // Used to maintain player position across minigames
        public PlayerToken token;
        // What the player rolls at the beginning of the game
        // Determines Turn Order
        public int StartingRoll;
        public List<GameData.Item> items = new List<GameData.Item>();

        public int strawberries {
            get;
            private set;
        }
        public int hearts;

        public PlayerData(int token)
        {
            TokenSelected = token;
        }

        public PlayerData(int token, uint gnetID) : this(token)
        {
            CelesteNetID = gnetID;
        }

        // Adds or subtracts the specified number of strawberries from the total
        public void ChangeStrawberries(int strawberries)
        {
            this.strawberries += strawberries;
            if (this.strawberries < 0)
            {
                this.strawberries = 0;
            }
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;
            if (obj is PlayerData other)
            {
                int compare = other.StartingRoll.CompareTo(StartingRoll);  
                if (compare == 0)
                {
                    return TurnOrder.CompareTo(other.TurnOrder);
                }
                return compare;
            }
            return 1;
        }
    }
}
