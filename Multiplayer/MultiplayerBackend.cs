using System.Collections.Generic;

namespace MadelineParty.Multiplayer {
    public abstract class MultiplayerBackend {
        public virtual void LoadContent() { }

        public abstract void Send(MultiplayerData data);

        public abstract bool BackendInstalled();

        public abstract bool BackendConnected();

        public abstract PlayerInfo GetPlayer(uint id);

        public abstract List<PlayerInfo> GetPlayers();

        public abstract uint CurrentPlayerID();

        public virtual void SendChat(string msg) { }
    }
}
