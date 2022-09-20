using Celeste.Mod.CelesteNet.Client;
using Celeste.Mod.CelesteNet.DataTypes;

namespace MadelineParty.Multiplayer.CelesteNet {
    public class CelesteNetPlayerInfo : PlayerInfo {
        private readonly uint id;
        private string name;

        public CelesteNetPlayerInfo(uint ID) {
            id = ID;
        }

        public override uint ID => id;

        public override string Name => name ?? (CelesteNetClientModule.Instance.Client.Data.TryGetRef(id, out DataPlayerInfo value) ? name = value.Name : "???");
    }
}
