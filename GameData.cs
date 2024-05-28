using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BrokemiaHelper;
using Celeste;
using MadelineParty.Board;
using MadelineParty.Minigame;
using MadelineParty.Multiplayer;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using static MadelineParty.Board.BoardController;

namespace MadelineParty
{
    public class GameData
    {
        public static GameData Instance { get; private set; } = new GameData();

        private GameData()
        {
        }

        public static readonly IReadOnlyDictionary<string, Item> items = new Dictionary<string, Item>() {
            {
                "Double Dice",
                new((player) => {
                        // Double dice are special
                        if(player != Instance.realPlayerID) {
                            return;
                        }
                        BoardController.Instance.RollDice(player, 2);
                    }) {
                    Name = "Double Dice",
                    Price = 3
                }
            },
            {
                "Triple Dice",
                new((player) => {
                        // Triple dice are special
                        if(player != Instance.realPlayerID) {
                            return;
                        }
                        BoardController.Instance.RollDice(player, 3);
                    }) {
                    Name = "Triple Dice",
                    Price = 6
                }
            },
            {
                "eciD esreveR",
                new((player) => {
                        BoardController.Instance.Add(new Coroutine(BoardController.Instance.DieRollAnimation(player, new[] { 0, -5, 0 }, ReverseDieRolled)));
                    }) {
                    Name = "eciD esreveR",
                    Price = 5
                }
            },
            {
                "Flip Flop",
                new((player) => {
                        BoardController.Instance.SetLeftButtonStatus(player, LeftButton.Modes.Inactive);
                        BoardController.Instance.SetRightButtonStatus(player, RightButton.Modes.Inactive);
                        Level level = Engine.Scene as Level;
                        var swappable = Instance.players.Where(p => p != null && p.token.id != player).ToList();
                        if(swappable.Count == 0) {
                            swappable = new List<PlayerData>() { Instance.players[player] };
                        }
                        var swapping = swappable[Instance.Random.Next(swappable.Count())];
                        swappable.Remove(swapping);
                        Dialog.Language.Dialog["MadelineParty_Swappable_Players"] = swapping == Instance.players[player] ? Dialog.Get("MadelineParty_Yourself") : Instance.GetPlayerName(swapping.token.id);
                        if(swappable.Count > 0) {
                            Dialog.Language.Dialog["MadelineParty_Swappable_Players"] += "|" + string.Join("|", swappable.ConvertAll(p => Instance.GetPlayerName(p.token.id)));
                        }
                        var textbox = new PersistentMiniTextbox("MadelineParty_Item_FlipFlop_Who", pauseUpdate: true);
                        level.Add(textbox);
                        textbox.OnFinish += () => BoardController.Instance.Add(new Coroutine(FlipFlopCoroutine(textbox, Instance.players[player], swapping, swappable.Count == 0), true));
                    }) {
                    Name = "Flip Flop",
                    Price = 4
                }
            },
            {
                "Minigame Skip",
                new((player) => {
                        Level level = Engine.Scene as Level;
                        if(level.Tracker.GetEntity<MinigameSelectUI>() is { } ui) {
                            ui.Reroll(player);
                        }
                    }) {
                    Name = "Minigame Skip",
                    Price = 7,
                    CanUseInTurn = false
                }
            },
            {
                "Heart Block",
                new((player) => {
                        BoardController.Instance.SetLeftButtonStatus(player, LeftButton.Modes.Inactive);
                        BoardController.Instance.SetRightButtonStatus(player, RightButton.Modes.Inactive);
                        Instance.heartBlocks.Add(player);
                        Engine.Scene.Add(new HeartBlock(
                            BoardController.Instance.boardSpaces.Find(s => s.ID == Instance.heartSpaceID).screenPosition - new Vector2(48), 48, 48));
                        Alarm.Set(BoardController.Instance, 2, () => BoardController.Instance.SetDice(player));
                    }) {
                    Name = "Heart Block",
                    Price = 10
                }
            },
        };

        private static void ReverseDieRolled(int playerID, int roll) {
            var board = BoardController.Instance;
            board.SetLeftButtonStatus(playerID, LeftButton.Modes.Inactive);
            board.SetRightButtonStatus(playerID, RightButton.Modes.Inactive);
            board.Add(new Coroutine(board.RemoveDieRollsAnimation()));
            // Get the past roll + 1 spaces
            board.playerMovePath = Instance.players[playerID].pastBoardSpaceIDs.Take(-roll + 1).Reverse().Select(id => board.boardSpaces.Find(s => s.ID == id)).ToList();
            board.movingPlayerID = playerID;
            board.playerMoveDistance = board.playerMovePath.Count - 1;
            board.playerMoveProgress = 0;

            board.status = BoardStatus.PLAYERMOVE;
        }

        private static IEnumerator FlipFlopCoroutine(PersistentMiniTextbox textbox, PlayerData p1, PlayerData p2, bool oneOption) {
            yield return oneOption ? 4f : 7f;
            yield return DynamicData.For(textbox).Invoke("Close");
            var vanishTween = Tween.Create(Tween.TweenMode.Oneshot, Ease.ElasticIn, 1.5f, true);
            vanishTween.OnUpdate += t => p1.token.scaleModifier = p2.token.scaleModifier = new Vector2(1 - t.Eased);
            BoardController.Instance.Add(vanishTween);
            while(vanishTween.Percent < 1) {
                yield return null;
            }
            // Swap the positions and spaces of the two players
            var tempPos = p1.token.Position;
            p1.token.Position = p2.token.Position;
            p2.token.Position = tempPos;
            var tempSpace = p1.token.currentSpace;
            p1.token.currentSpace = p2.token.currentSpace;
            p2.token.currentSpace = tempSpace;
            p1.pastBoardSpaceIDs.Clear();
            p2.pastBoardSpaceIDs.Clear();
            yield return 1f;

            var appearTween = Tween.Create(Tween.TweenMode.Oneshot, Ease.ElasticOut, 1.5f, true);
            appearTween.OnUpdate += t => p1.token.scaleModifier = p2.token.scaleModifier = new Vector2(t.Eased);
            BoardController.Instance.Add(appearTween);
            while (appearTween.Percent < 1) {
                yield return null;
            }

            // Have the player roll as normal
            BoardController.Instance.SetDice(p1.token.id);
        }

        public class Item {
            public string Name { get; set; }
            public int Price { get; set; }
            public bool CanUseInTurn { get; set; } = true;
            private Action<int> Action { get; set; }

            public Item(Action<int> action) {
                Action = action;
            }

            public void UseItem(int playerID) {
                if (!MadelinePartyModule.SaveData.ItemsUsed.ContainsKey(Name)) {
                    MadelinePartyModule.SaveData.ItemsUsed[Name] = 0;
                }
                if (playerID == Instance.realPlayerID) {
                    MadelinePartyModule.SaveData.ItemsUsed[Name]++;
                }
                Action?.Invoke(playerID);
            }
        }

        public Random Random;

        public const int START_BERRIES = 10;
        public const int MAX_ITEMS = 3;
        public int turn = 1;
        public int maxTurns = 10;
        public int playerNumber = -1;
        public PlayerData[] players = { null, null, null, null };
        // The ID of the player at this client
        public int realPlayerID = -1;
        public bool celesteNetHost = true;
        public List<uint> celestenetIDs = new();
        // Which playerSelectTrigger each player is in
        public ConcurrentDictionary<uint, int> playerSelectTriggers = new();
        public List<string> playedMinigames = new();
        // Matches player token ID to number of minigames won
        public Dictionary<int, uint> minigameWins = new();
        // Matches player token ID to minigame status
        public Dictionary<int, uint> minigameStatus = new();
        // Matches player token ID to minigame results
        public List<Tuple<int, uint>> minigameResults = new();
        public int heartSpaceID = -1;
        public List<int> heartBlocks = new();
        public PlayerSelectTrigger currentPlayerSelection;
        public int heartCost = 15;
        public string minigame;
        public string board;
        public bool gameStarted;
        private static List<string> earlyShop = new() { "Double Dice", "Minigame Skip", "Flip Flop", "eciD esreveR" };
        private static List<string> lateShop = new() { "Double Dice", "Triple Dice", "Minigame Skip", "Heart Block" };
        public List<string> shopContents
        {
            get
            {
                return turn <= maxTurns / 2 ? earlyShop : lateShop;
            }
        }

        public PlayerData RealPlayer => players[realPlayerID];

        public static void Reset()
        {
            Instance = new GameData();
            BoardController.hackfixRespawn = false;
        }

        public string GetPlayerName(int id) {
            if (MultiplayerSingleton.Instance.BackendConnected()) {
                if(realPlayerID == id) {
                    return MultiplayerSingleton.Instance.GetPlayer(MultiplayerSingleton.Instance.CurrentPlayerID()).Name;
                }
                return MultiplayerSingleton.Instance.GetPlayer(Instance.playerSelectTriggers.First(kvp => kvp.Value == id).Key).Name;
            } else {
                return SaveData.Instance.Name;
            }
        }

        public string GetRandomDialogID(string listID) {
            return Random.Choose(Dialog.Clean(listID).Split(','));
        }

        private bool LevelMatchesSearch(LevelData level, MinigameSearchQuery query) {
            if (query == null) return true;
            if (level.Entities.Find(data => data.Name == MinigameMetadataController.EntityName) is not { } data) return true;
            var meta = MinigameMetadataController.LoadMetadata(data);
            if (meta.MinPlayers > query.PlayerCount || meta.MaxPlayers < query.PlayerCount) return false;
            // Make sure minigame tags contains at least all the tags in query.Tags
            if (!meta.MinigameTags.IsSupersetOf(query.Tags)) return false;
            return true;
        }

        public List<LevelData> GetAllMinigames(Level level, MinigameSearchQuery query) {
            return level.Session.MapData.Levels.FindAll((lvl) => lvl.Name.StartsWith("z_Minigame", StringComparison.InvariantCulture) && LevelMatchesSearch(lvl, query));
        }

        public List<LevelData> GetAllUnplayedMinigames(Level level, MinigameSearchQuery query) {
            return GetAllMinigames(level, query).FindAll((lvl) => !playedMinigames.Contains(lvl.Name));
        }

        public static string GetMinigameMusic(string name) {
            return "event:/madelineparty/music/minigame/chase_chiptune";
        }
    }
}
