using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Celeste;
using Celeste.Mod.CelesteNet.Client;
using Celeste.Mod.CelesteNet.DataTypes;
using MadelineParty.Multiplayer;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace MadelineParty
{
    public class GameData
    {
        public static GameData Instance { get; private set; } = new GameData();

        private GameData()
        {
        }

        private static readonly Random rand = new();

        public static readonly IReadOnlyDictionary<string, Item> items = new Dictionary<string, Item>() {
            {
                "Double Dice",
                new() {
                    Name = "Double Dice",
                    Price = 10,
                    Action = (player) => {
                        // Double dice are special
                        if(player != Instance.realPlayerID) {
                            return;
                        }
                        BoardController.Instance.RollDice(player, true);
                    }
                }
            },
            {
                "Flip Flop",
                new() {
                    Name = "Flip Flop",
                    Price = 7,
                    Action = (player) => {
                        BoardController.Instance.SetLeftButtonStatus(player, LeftButton.Modes.Inactive);
                        BoardController.Instance.SetRightButtonStatus(player, RightButton.Modes.Inactive);
                        Level level = Engine.Scene as Level;
                        var swappable = Instance.players.Where(p => p != null && p.token.id != player).ToList();
                        if(swappable.Count == 0) {
                            swappable = new List<PlayerData>() { Instance.players[player] };
                        }
                        var swapping = swappable[rand.Next(swappable.Count())];
                        swappable.Remove(swapping);
                        Dialog.Language.Dialog["MadelineParty_Swappable_Players"] = swapping == Instance.players[player] ? Dialog.Get("MadelineParty_Yourself") : Instance.GetPlayerName(swapping.token.id);
                        if(swappable.Count > 0) {
                            Dialog.Language.Dialog["MadelineParty_Swappable_Players"] += "|" + string.Join("|", swappable.ConvertAll(p => Instance.GetPlayerName(p.token.id)));
                        }
                        var textbox = new PersistentMiniTextbox("MadelineParty_Item_FlipFlop_Who");
                        level.Add(textbox);
                        textbox.OnFinish += () => BoardController.Instance.Add(new Coroutine(FlipFlopCoroutine(textbox, Instance.players[player], swapping, swappable.Count == 0), true));
                    }
                }
            }
        };

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
            public Action<int> Action { get; set; }
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
        private List<string> earlyShop = new() { "Double Dice" };
        private List<string> lateShop = new() { "Flip Flop", "Double Dice" };
        public List<string> shopContents
        {
            get
            {
                return turn <= maxTurns / 2 ? earlyShop : lateShop;
            }
        }

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
