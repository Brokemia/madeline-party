using System;
namespace MadelineParty.Ghostnet
{
    public class PlayerChoiceData
    {
        public enum ChoiceType
        {
            HEART,
            HEARTX,
            HEARTY,
            ENTERSHOP,
            SHOPITEM,
            DIRECTION
        }

        public uint playerID;
        public string playerName;

        public ChoiceType choiceType;
        // For buttons, 0 = left, 1 = right
        // For Direction, UP = 0, DOWN = 1, LEFT = 2, RIGHT = 3
        public int choice;
    }
}
