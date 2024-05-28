using Celeste;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadelineParty.Minigame {
    [Tracked(true)]
    public class MinigamePersistentData : Entity {
        public bool DidRespawn { get; set; }
        public bool Started { get; set; }
        public float StartTime { get; set; } = -1;

        private Dictionary<int, List<string>> roles = new();

        public MinigamePersistentData() {
            Tag = Tags.Global;
        }

        public void AssignRole(int playerID, string role) {
            if (!roles.ContainsKey(playerID)) {
                roles[playerID] = new();
            }
            roles[playerID].Add(role);
        }

        public bool HasRole(int playerID, string role) {
            return roles.ContainsKey(playerID) && roles[playerID].Contains(role);
        }
    }
}
