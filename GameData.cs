using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;

namespace MadelineParty
{
    public class GameData
    {
        private GameData()
        {
        }

        public enum Item
        {
            DOUBLEDICE
        }

        public const int maxItems = 3;
        public static int turn = 1;
        // TODO Add selector for turn number
        public static int maxTurns = 10;
        public static int playerNumber = -1;
        public static uint turnOrderSeed = 21;
        public static uint tieBreakerSeed = 20;
        public static PlayerData[] players = { null, null, null, null };
        // The ID of the player at this client
        public static int realPlayerID = -1;
        public static bool gnetHost = true;
        public static List<uint> celestenetIDs = new List<uint>();
        // Which playerSelectTrigger each player is in
        public static ConcurrentDictionary<uint, int> playerSelectTriggers = new ConcurrentDictionary<uint, int>();
        public static List<string> playedMinigames = new List<string>();
        // Matches player token ID to minigame status
        public static Dictionary<int, uint> minigameStatus = new Dictionary<int, uint>();
        // Matches player token ID to minigame results
        public static List<Tuple<int, uint>> minigameResults = new List<Tuple<int, uint>>();
        public static Vector2 heartSpace = new Vector2(6, 8);
        public static PlayerSelectTrigger currentPlayerSelection;
        public static int heartCost = 5;
        public static LevelData minigame;
        public static bool gameStarted;
        private static List<Item> earlyShop = new List<Item>(new Item[] { Item.DOUBLEDICE });
        private static List<Item> lateShop = new List<Item>(new Item[] { Item.DOUBLEDICE });
        public static List<Item> shopContents
        {
            get
            {
                return turn < 5 ? earlyShop : lateShop;
            }
        }

        // Set in the Module's Initialize method
        public static Dictionary<Item, int> itemPrices = new Dictionary<Item, int>();

        public static void Reset()
        {
            turn = 1;
            maxTurns = 10;
            playerNumber = -1;
            turnOrderSeed = 21;
            tieBreakerSeed = 20;
            players = new PlayerData[]{ null, null, null, null };
            realPlayerID = -1;
            heartSpace = new Vector2(6, 8);
            gnetHost = true;
            celestenetIDs = new List<uint>();
            heartCost = 5;
            playerSelectTriggers = new ConcurrentDictionary<uint, int>();
            playedMinigames = new List<string>();
            minigameStatus = new Dictionary<int, uint>();
            minigameResults = new List<Tuple<int, uint>>();
            currentPlayerSelection = null;
            minigame = null;
            gameStarted = false;
            MinigameEntity.startTime = -1;
            MinigameEntity.started = false;
            MinigameTheoMover.theoCount = 0;
        }

    }
}
