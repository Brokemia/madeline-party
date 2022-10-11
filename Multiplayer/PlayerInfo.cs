using Microsoft.Xna.Framework;

namespace MadelineParty.Multiplayer {
    public abstract class PlayerInfo {
        public abstract uint ID { get; }

        public abstract string Name { get; }

        public abstract string Level { get; }
        
        public abstract string SID { get; }
        
        public abstract Vector2 Position { get; }
    }

    public class DummyPlayerInfo : PlayerInfo {
        public uint _ID;

        public override uint ID => _ID;

        public string _Name = "???";

        public string _Level = null;

        public string _SID = null;

        public Vector2 _Position;

        public override string Name => _Name;

        public override string Level => _Level;

        public override string SID => _SID;

        public override Vector2 Position => _Position;
    }
}
