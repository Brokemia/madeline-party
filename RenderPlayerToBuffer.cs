using Monocle;
using System;

namespace MadelineParty {
    [Tracked]
    public class RenderPlayerToBuffer : Component {
        private Action afterGameplay;

        public RenderPlayerToBuffer(Action render) : base(false, false) {
            afterGameplay = render;
        }

        public void AfterGameplay() {
            afterGameplay?.Invoke();
        }
    }
}
