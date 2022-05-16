
using Celeste.Mod;
using Celeste.Mod.CelesteNet.Client;
using Celeste.Mod.CelesteNet.DataTypes;
using MadelineParty.Multiplayer.CelesteNet;
using MadelineParty.Multiplayer.General;
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

        private static void loadCelesteNet() => CelesteNetMadelinePartyComponent.handleAction = Instance.Handle;

        private static void sendChatCelesteNet(string msg) => CelesteNetClientModule.Instance.Context.Chat.Log.Add(new DataChat { Text = msg });

        // General

        private Dictionary<Type, List<Action<MPData>>> handlers = new();

        private static readonly Dictionary<string, Action<MultiplayerData>> sendMethods = new() {
            { CELESTENET_NAMESPACE, SendCelesteNet }
        };

        // installed, connected
        private static readonly Dictionary<string, Tuple<Func<bool>, Func<bool>>> statusMethods = new() {
            { CELESTENET_NAMESPACE, new Tuple<Func<bool>, Func<bool>>(celesteNetInstalled, celesteNetConnected) }
        };

        private static readonly Dictionary<string, Func<uint>> idMethods = new() {
            { CELESTENET_NAMESPACE, getIdCelesteNet }
        };

        private readonly Dictionary<string, Action> loadMethods = new() {
            { CELESTENET_NAMESPACE, loadCelesteNet }
        };

        private readonly Dictionary<string, Action<string>> chatMethods = new() {
            { CELESTENET_NAMESPACE, sendChatCelesteNet }
        };

        private MultiplayerSingleton() { }

        public void LoadContent() {
            if (BackendInstalled()) {
                loadMethods[multiplayerBackend]?.Invoke();
            }
        }

        public void Send(MPData args) {
            if(BackendConnected()) {
                MultiplayerData data = GetData(args.GetType().Name + "Data");
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
            return BackendConnected() ? idMethods[multiplayerBackend]() : uint.MaxValue;
        }

        public void RegisterHandler<T>(Action<MPData> handler) {
            if(!handlers.ContainsKey(typeof(T))) {
                handlers[typeof(T)] = new();
            }
            handlers[typeof(T)].Add(handler);
        }

        private void Handle(MPData data) {
            if(handlers.TryGetValue(data.GetType(), out List<Action<MPData>> specificHandlers)) {
                foreach(var handler in specificHandlers) {
                    handler.Invoke(data);
                }
            }
        }

        public void SendChat(string msg) {
            if (BackendConnected()) {
                chatMethods[multiplayerBackend]?.Invoke(msg);
            }
        }

    }
}
