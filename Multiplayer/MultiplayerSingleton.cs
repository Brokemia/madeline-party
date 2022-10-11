using Celeste.Mod;
using MadelineParty.Multiplayer.General;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MadelineParty.Multiplayer {
    public sealed class MultiplayerSingleton {
        public const bool DEBUG_LOGGING = false;

        private static MultiplayerSingleton _instance;
        public static MultiplayerSingleton Instance => _instance ??= new();

        private static readonly string backendName = "CelesteNet";

        private static MultiplayerBackend backend = CreateBackend();

        private static MultiplayerBackend CreateBackend() {
            Type singletonType = Type.GetType($"MadelineParty.Multiplayer.{backendName}.{backendName}Backend, {Assembly.GetExecutingAssembly().FullName}");
            return (MultiplayerBackend)singletonType.GetConstructor(Everest._EmptyTypeArray).Invoke(Everest._EmptyObjectArray);
        }

        private MultiplayerSingleton() { }

        private readonly Dictionary<Type, List<Action<MPData>>> handlers = new();
        private readonly Dictionary<Type, Dictionary<string, Action<MPData>>> uniqueHandlers = new();

        public void LoadContent() {
            if (BackendInstalled()) {
                backend.LoadContent();
            }
        }

        public void Send(MPData args) {
            if(BackendConnected()) {
                MultiplayerData data = GetData(args.GetType().Name + "Data");
                data.Initialize(args);
                backend.Send(data);
#pragma warning disable CS0162 // Unreachable code detected
                if (DEBUG_LOGGING) Console.WriteLine("Multiplayer Sent: " + data.GetType());
#pragma warning restore CS0162 // Unreachable code detected
            }
        }

        private MultiplayerData GetData(string id) {
            Type dataType = Type.GetType($"MadelineParty.Multiplayer.{backendName}.Data.{id}, {Assembly.GetExecutingAssembly().FullName}");
            MultiplayerData data = (MultiplayerData)dataType.GetConstructor(Everest._EmptyTypeArray).Invoke(Everest._EmptyObjectArray);
            return data;
        }

        public bool BackendInstalled() {
            return backend.BackendInstalled();
        }

        public bool BackendConnected() {
            return BackendInstalled() && backend.BackendConnected();
        }

        public uint CurrentPlayerID() {
            return BackendConnected() ? backend.CurrentPlayerID() : uint.MaxValue;
        }

        public PlayerInfo GetPlayer(uint id) {
            if(BackendConnected()) {
                return backend.GetPlayer(id);
            }
            return new DummyPlayerInfo() { _ID = id };
        }

        public List<PlayerInfo> GetPlayers() {
            if (BackendConnected()) {
                return backend.GetPlayers();
            }
            return new();
        }

        public void RegisterHandler<T>(Action<MPData> handler) {
            if(!handlers.ContainsKey(typeof(T))) {
                handlers[typeof(T)] = new();
            }
            handlers[typeof(T)].Add(handler);
        }

        public void RegisterUniqueHandler<T>(string key, Action<MPData> handler) where T : MPData {
            if (!uniqueHandlers.ContainsKey(typeof(T))) {
                uniqueHandlers[typeof(T)] = new();
            }
            uniqueHandlers[typeof(T)][key] = handler;
        }

        public void Handle(MPData data) {
#pragma warning disable CS0162 // Unreachable code detected
            if (DEBUG_LOGGING) Console.WriteLine("Multiplayer Received: " + data.GetType());
#pragma warning restore CS0162 // Unreachable code detected
            if (handlers.TryGetValue(data.GetType(), out List<Action<MPData>> specificHandlers)) {
                foreach (var handler in specificHandlers) {
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
                backend.SendChat(msg);
            }
        }
    }
}
