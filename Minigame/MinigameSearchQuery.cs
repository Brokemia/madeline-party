using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadelineParty.Minigame {
    public class MinigameSearchQuery {
        public int PlayerCount { get; set; }

        public HashSet<string> Tags { get; set; } = new();
    }
}
