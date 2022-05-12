
using Celeste.Mod;
using Celeste.Mod.CelesteNet.Client;
using Celeste.Mod.CelesteNet.DataTypes;
using MonoMod.Utils;
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

        private static bool celesteNetInstalled() => Everest.Loader.DependencyLoaded(celesteNetDependency);

        private static bool celesteNetConnected() => CelesteNetClientModule.Instance?.Client?.Con != null;

        private static void SendCelesteNet(MultiplayerData data) {
            DynamicData.For(data).Set("Player", CelesteNetClientModule.Instance.Client.PlayerInfo);
            CelesteNetClientModule.Instance.Client?.Send(data as DataType);
        }

        private static uint getIdCelesteNet() => CelesteNetClientModule.Instance.Client.PlayerInfo.ID;

        private static readonly Dictionary<string, Action<MultiplayerData>> sendMethods = new Dictionary<string, Action<MultiplayerData>> {
            { CELESTENET_NAMESPACE, SendCelesteNet }
        };

        // installed, connected
        private static readonly Dictionary<string, Tuple<Func<bool>, Func<bool>>> statusMethods = new Dictionary<string, Tuple<Func<bool>, Func<bool>>> {
            { CELESTENET_NAMESPACE, new Tuple<Func<bool>, Func<bool>>(celesteNetInstalled, celesteNetConnected) }
        };

        private static readonly Dictionary<string, Func<uint>> idMethod = new Dictionary<string, Func<uint>> {
            { CELESTENET_NAMESPACE, getIdCelesteNet }
        };

        private MultiplayerSingleton() { }

        public void Send(string id, Dictionary<string, object> args) {
            if(BackendConnected()) {
                MultiplayerData data = GetData(id);
                data.Initialize(args);
                sendMethods[multiplayerBackend](data);
            }
        }

        private MultiplayerData GetData(string id) {
            Type dataType = Type.GetType($"MadelineParty.Multiplayer.{multiplayerBackend}.{id}, {Assembly.GetExecutingAssembly().FullName}");
            MultiplayerData data = (MultiplayerData)dataType.GetConstructor(new Type[0]).Invoke(new object[0]);
            return data;
        }

        public bool BackendInstalled() {
            return statusMethods[multiplayerBackend].Item1();
        }

        public bool BackendConnected() {
            return BackendInstalled() && statusMethods[multiplayerBackend].Item2();
        }

        public uint GetPlayerID() {
            return BackendConnected() ? idMethod[multiplayerBackend]() : uint.MaxValue;
        }

    }
}
