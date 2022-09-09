
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
        public const bool DEBUG_LOGGING = false;

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
        
        private static string getNameCelesteNet(uint id) => CelesteNetClientModule.Instance.Client.Data.TryGetRef(id, out DataPlayerInfo value) ? value.Name : "???";

        private static void loadCelesteNet() => CelesteNetMadelinePartyComponent.handleAction = Instance.Handle;

        private static void sendChatCelesteNet(string msg) {
            // FIXME Temporary workaround, CelesteNet team should be putting out a new API for me eventually
            DataChat chat = new DataChat { Text = msg, ID = 5 };
            CelesteNetClientModule.Instance.Context.Chat.Handle(CelesteNetClientModule.Instance.Context.Client.Con, chat);

        }

        // General

        private Dictionary<Type, List<Action<MPData>>> handlers = new();
        private Dictionary<Type, Dictionary<string, Action<MPData>>> uniqueHandlers = new();

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

        private static readonly Dictionary<string, Func<uint, string>> nameMethods = new() {
            { CELESTENET_NAMESPACE, getNameCelesteNet }
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
                if (DEBUG_LOGGING) Console.WriteLine("Multiplayer Sent: " + data.GetType());
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

        // TODO maybe turn ID and name into a general info thing

        public uint GetPlayerID() {
            return BackendConnected() ? idMethods[multiplayerBackend]() : uint.MaxValue;
        }

        public string GetPlayerName(uint id) {
            if(BackendConnected()) {
                return nameMethods[multiplayerBackend](id);
            }
            throw new Exception("Attempted to get player name while not connected");
        }

        public void RegisterHandler<T>(Action<MPData> handler) {
            if(!handlers.ContainsKey(typeof(T))) {
                handlers[typeof(T)] = new();
            }
            handlers[typeof(T)].Add(handler);
        }

        public void RegisterUniqueHandler<T>(string key, Action<MPData> handler) {
            if (!uniqueHandlers.ContainsKey(typeof(T))) {
                uniqueHandlers[typeof(T)] = new();
            }
            uniqueHandlers[typeof(T)][key] = handler;
        }

        private void Handle(MPData data) {
            if(DEBUG_LOGGING) Console.WriteLine("Multiplayer Received: " + data.GetType());
            if(handlers.TryGetValue(data.GetType(), out List<Action<MPData>> specificHandlers)) {
                foreach(var handler in specificHandlers) {
                    handler.Invoke(data);
                }
            }
            if (uniqueHandlers.TryGetValue(data.GetType(), out Dictionary<string, Action<MPData>> specificUniqueHandlers)) {
                foreach (var handler in specificUniqueHandlers.Values) {
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
