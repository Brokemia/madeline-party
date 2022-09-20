namespace MadelineParty.Multiplayer {
    public abstract class PlayerInfo {
        public abstract uint ID { get; }

        public abstract string Name { get; }
    }

    public class DummyPlayerInfo : PlayerInfo {
        public uint _ID;

        public override uint ID => _ID;

        public string _Name = "???";

        public override string Name => _Name;
    }
}
