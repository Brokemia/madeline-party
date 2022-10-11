using Celeste.Mod.CelesteNet.Client;
using Celeste.Mod.CelesteNet.DataTypes;
using Microsoft.Xna.Framework;

namespace MadelineParty.Multiplayer.CelesteNet {
    public class CelesteNetPlayerInfo : PlayerInfo {
        private readonly uint id;
        private string name;

        public CelesteNetPlayerInfo(uint ID) {
            id = ID;
        }

        public override uint ID => id;

        public override string Name => name ?? (CelesteNetClientModule.Instance.Client.Data.TryGetRef(id, out DataPlayerInfo value) ? name = value.Name : "???");
        
        public override string Level => (CelesteNetClientModule.Instance.Client.Data.TryGetRef(id, out DataPlayerState value) ? value.Level : null);
        
        public override string SID => (CelesteNetClientModule.Instance.Client.Data.TryGetRef(id, out DataPlayerState value) ? value.SID : null);
        
        public override Vector2 Position => (CelesteNetClientModule.Instance.Client.Data.TryGetRef(id, out DataPlayerFrame value) ? value.Position : default);
    }
}
