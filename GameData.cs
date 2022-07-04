using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Celeste;
using Celeste.Mod.CelesteNet.Client;
using Celeste.Mod.CelesteNet.DataTypes;
using MadelineParty.Multiplayer;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty
{
    public class GameData
    {
        public static GameData Instance { get; private set; } = new GameData();

        private GameData()
        {
        }

        public enum Item
        {
            DOUBLEDICE
        }

        public const int maxItems = 3;
        public int turn = 1;
        public int maxTurns = 10;
        public int playerNumber = -1;
        public uint turnOrderSeed = 21;
        public uint tieBreakerSeed = 20;
        public PlayerData[] players = { null, null, null, null };
        // The ID of the player at this client
        public int realPlayerID = -1;
        public bool gnetHost = true;
        public List<uint> celestenetIDs = new List<uint>();
        // Which playerSelectTrigger each player is in
        public ConcurrentDictionary<uint, int> playerSelectTriggers = new ConcurrentDictionary<uint, int>();
        public List<string> playedMinigames = new List<string>();
        // Matches player token ID to minigame status
        public Dictionary<int, uint> minigameStatus = new Dictionary<int, uint>();
        // Matches player token ID to minigame results
        public List<Tuple<int, uint>> minigameResults = new List<Tuple<int, uint>>();
        public int heartSpaceID = 12;
        public PlayerSelectTrigger currentPlayerSelection;
        public int heartCost = 5;
        public string minigame;
        public bool gameStarted;
        private List<Item> earlyShop = new List<Item>(new Item[] { Item.DOUBLEDICE });
        private List<Item> lateShop = new List<Item>(new Item[] { Item.DOUBLEDICE });
        public List<Item> shopContents
        {
            get
            {
                return turn <= maxTurns / 2 ? earlyShop : lateShop;
            }
        }

        // Set in the Module's Initialize method
        public Dictionary<Item, int> itemPrices = new Dictionary<Item, int>();

        private Random _textRand;

        private Random TextRand {
            get => _textRand == null ? _textRand = new Random((int)(turnOrderSeed / 3) - 120) : _textRand;
        }

        public static void Reset()
        {
            Instance = new GameData();
            MinigameEntity.startTime = -1;
            MinigameEntity.started = false;
            MinigameEntity.didRespawn = false;
            MinigameTheoMover.theoCount = 0;
            MinigameInfinityTrigger.loops = 0;
            MinigameInfinityTrigger.dist = 0;
            MinigameSwitchGatherer.switchCount = 0;
            MinigameSwitchGatherer.switchesOn = new();
        }

        public string GetPlayerName(int id) {
            if (MultiplayerSingleton.Instance.BackendConnected()) {
                if(realPlayerID == id) {
                    return MultiplayerSingleton.Instance.GetPlayerName(MultiplayerSingleton.Instance.GetPlayerID());
                }
                return MultiplayerSingleton.Instance.GetPlayerName(Instance.playerSelectTriggers.First(kvp => kvp.Value == id).Key);
            } else {
                return SaveData.Instance.Name;
            }
        }

        public string GetRandomDialogID(string listID) {
            return TextRand.Choose(Dialog.Clean(listID).Split(','));
        }

        public List<LevelData> GetAllMinigames(Level level) {
            return level.Session.MapData.Levels.FindAll((lvl) => lvl.Name.StartsWith("z_Minigame", StringComparison.InvariantCulture));
        }

        public List<LevelData> GetAllUnplayedMinigames(Level level) {
            return GetAllMinigames(level).FindAll((lvl) => !playedMinigames.Contains(lvl.Name));
        }

        public static string GetMinigameMusic(string name) {
            return "event:/madelineparty/music/minigame/chase_chiptune";
        }
    }
}
