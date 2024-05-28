using MadelineParty.Multiplayer.General;
using Monocle;
using System;

namespace MadelineParty.Multiplayer {
    public class MultiplayerHandlerComponent<T> : Component where T : MPData {
        private readonly string key;
        private readonly Action<MPData> handler;

        public MultiplayerHandlerComponent(string uniqueKey, Action<MPData> handler) : base(false, false) {
            key = uniqueKey;
            this.handler = handler;
        }

        public override void Added(Entity entity) {
            base.Added(entity);
            MultiplayerSingleton.Instance.RegisterUniqueHandler<T>(key, handler);
        }

        public override void Removed(Entity entity) {
            base.Removed(entity);
            MultiplayerSingleton.Instance.UnregisterUniqueHandler<T>(key);
        }

        public override void EntityRemoved(Scene scene) {
            base.EntityRemoved(scene);
            MultiplayerSingleton.Instance.UnregisterUniqueHandler<T>(key);
        }

        public override void SceneEnd(Scene scene) {
            base.SceneEnd(scene);
            MultiplayerSingleton.Instance.UnregisterUniqueHandler<T>(key);
        }
    }
}
