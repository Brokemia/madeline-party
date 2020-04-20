using System;
namespace MadelineParty
{
    public class DisplayChangeData
    {
        public enum Changed
        {
            LEFT,
            RIGHT,
            SCOREBOARD
        }

        public Changed changed;
        // The ID of the enum entry it has been changed to
        public int changedTo;

        public DisplayChangeData()
        {
        }
    }
}
