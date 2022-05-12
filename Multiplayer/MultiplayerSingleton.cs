
using Celeste.Mod;
using Celeste.Mod.CelesteNet.Client;
using Celeste.Mod.CelesteNet.DataTypes;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MadelineParty.Multiplayer {
    class MultiplayerSingleton {
        private static MultiplayerSingleton _instance;
        public static MultiplayerSingleton Instance => _instance ??= new MultiplayerSingleton();

        private static readonly string multiplayerBackend = CELESTENET_NAMESPACE;

        // CelesteNet
        private const string CELESTENET_NAMESPACE = "CelesteNet";
        private static readonly EverestModuleMetadata celesteNetDependency = new EverestModuleMetadata { Name = "CelesteNet.Client", Version = new Version(2, 0, 0) };

        private static bool celesteNetInstalled => Everest.Loader.DependencyLoaded(celesteNetDependency);

        private static bool celesteNetConnected => CelesteNetClientModule.Instance?.Client?.Con != null;

        private static readonly Dictionary<string, Action<MultiplayerData>> sendMethods = new Dictionary<string, Action<MultiplayerData>> {
            { CELESTENET_NAMESPACE, SendCelesteNet }
        };

        private MultiplayerSingleton() { }

        public void Send(string id, Dictionary<string, object> args) {
            if(BackendEnabled()) {
                MultiplayerData data = GetData(id);
                data.Initialize(args);
                sendMethods[multiplayerBackend](data);
            }
        }

        private static void SendCelesteNet(MultiplayerData data) {
            CelesteNetClientModule.Instance.Client?.Send(data as DataType);
        }

        private MultiplayerData GetData(string id) {
            Type dataType = Type.GetType($"MadelineParty.Multiplayer.{multiplayerBackend}.{id}, {Assembly.GetExecutingAssembly().FullName}");
            MultiplayerData data = (MultiplayerData)dataType.GetConstructor(new Type[0]).Invoke(new object[0]);
            return data;
        }

        private bool BackendEnabled() {
            switch(multiplayerBackend) {
                case CELESTENET_NAMESPACE:
                    return celesteNetInstalled && celesteNetConnected;
            }

            return false;
        }

    }
}
