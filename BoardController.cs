using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BrokemiaHelper;
using Celeste;
using Celeste.Mod;
using MadelineParty.GreenSpace;
using MadelineParty.Multiplayer;
using MadelineParty.Multiplayer.General;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;
using Logger = Celeste.Mod.Logger;

namespace MadelineParty {
    public class BoardController : Entity {
        public enum BoardStatus {
            WAITING,
            GAMESTART,
            PLAYERMOVE,
            ADJUST_POSITION
        }

        public enum Direction {
            Up,
            Down,
            Left,
            Right
        }

        public enum BoardSpaceType {
            Blue,
            Red,
            Start,
            Shop,
            Event,
            ImmediateEvent
        }

        public struct BoardSpace {
            public int ID;
            public int x, y;
            private List<BoardSpace> _destinations;
            public List<BoardSpace> GetDestinations(List<BoardSpace> spaces) {
                if (_destinations == null) {
                    _destinations = new List<BoardSpace>();
                    if (destIDs_DONTUSE != null) {
                        foreach (int id in destIDs_DONTUSE) {
                            _destinations.Add(spaces.Find(m => m.ID == id));
                        }
                    }
                }
                return _destinations;
            }
            public List<int> destIDs_DONTUSE;
            public BoardSpaceType type;
            public bool heartSpace;
            public string greenSpaceEvent;
            public Vector2 position { get { return new Vector2(x, y); } }
            public Vector2 screenPosition => ((Instance?.Position ?? Vector2.Zero) - ((Engine.Scene as Level)?.LevelOffset ?? Vector2.Zero) + new Vector2(x, y)) * 6;
        }

        protected class TurnDisplay : Entity {
            private Level level;

            public TurnDisplay() {
                AddTag(TagsExt.SubHUD);
            }

            public override void Added(Scene scene) {
                base.Added(scene);
                level = SceneAs<Level>();
            }

            public override void Render() {
                base.Render();
                string text = "Turn " + (Instance.turnDisplay == -1 ? GameData.Instance.turn : Instance.turnDisplay) + "/" + GameData.Instance.maxTurns;
                ActiveFont.DrawOutline(text, new Vector2(Celeste.Celeste.TargetWidth / 2, Celeste.Celeste.TargetHeight - 6 * 16) - level.ShakeVector * 6, new Vector2(0.5f, 0.5f), Vector2.One, Color.Blue, 2f, Color.Black);
            }
        }

        private class SubHUDRenderer : Entity {
            private BoardController controller;
            public SubHUDRenderer(BoardController controller) {
                this.controller = controller;
                AddTag(TagsExt.SubHUD);
            }

            public override void Render() {
                base.Render();
                controller.SubHUDRender();
            }
        }

        public static readonly Color pathColor = Color.White; // Use DarkSlateGray in dark mode
        public static readonly Color pathOutlineColor = Color.DarkGray; // Use Black in dark mode

        public const float TOKEN_SPEED = 80f;
        public const float TOKEN_SPACING = 18f;
        public static string[] TokenPaths = { "madeline/normal00", "badeline/normal00", "theo/excited00", "granny/normal00" };

        public List<BoardSpace> playerMovePath = null;

        // The distance the player will be moving
        public int playerMoveDistance = 0;
        public int playerMoveProgress = 0;
        public int movingPlayerID = 0;

        private Dictionary<int, Vector2> adjustedSpacePositions = new();

        // The index of the item in the shop being viewed
        private int shopItemViewing = 0;

        private Level level;

        private Random rand = new Random();

        // The number of players that have rolled dice
        // Used for the start of the game
        private int diceRolled = 0;

        private bool removingDieRolls;

        public PlayerToken[] playerTokens = { null, null, null, null };
        // turnOrder[0] == 2 means player[2] goes first
        public int[] turnOrder = { 0, 0, 0, 0 };
        public static int[] turnOrderRolls = { 0, 0, 0, 0 };
        public static int[] oneToTen = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        private int playerTurn = 0;
        private int turnDisplay = -1;

        private int heartStartID;

        private List<LeftButton> leftButtons = new();
        private List<RightButton> rightButtons = new();

        private List<GameScoreboard> scoreboards = new();

        public List<MTexture> diceNumbers;
        private List<DieNumber> numbersToDisplay = new();

        // id, rolls
        public static DieRoll delayedDieRoll;

        private static Dictionary<int, int> queuedStrawberries = new();

        private DateTime minigameStartTime;

        public static LevelData riggedMinigame;
        public static int riggedRoll = -1;

        public BoardStatus status = BoardStatus.GAMESTART;

        public static BoardController Instance;

        public static Dictionary<BoardSpaceType, MTexture> spaceTextures;
        public MTexture heartTexture = GFX.Game["objects/madelineparty/miniheart/00"];

        public BoardController(EntityData data) : base(data.Position) {
            Instance = this;
            heartStartID = data.Int("heart_start_ID", -1);
            diceNumbers = GFX.Game.GetAtlasSubtextures("decals/madelineparty/dicenumbers/dice_");
            AddTag(Tags.PauseUpdate);
            AddTag(Tags.FrozenUpdate);
        }

        public static List<EntityData> boardEntityData = new();
        
        public List<BoardSpace> boardSpaces = new();

        public static Dictionary<string, GreenSpaceEvent> greenSpaces;

        private VirtualRenderTarget lineRenderTarget;

        static BoardController() {
            //boardSpaces.Add(new BoardSpace() { ID = 0, type = 's', x = 16, y = 52, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 1, } });
            //boardSpaces.Add(new BoardSpace() { ID = 1, type = 'b', x = 33, y = 42, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 2, } });
            //boardSpaces.Add(new BoardSpace() { ID = 2, type = 'b', x = 45, y = 23, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 3, 10, } });
            //boardSpaces.Add(new BoardSpace() { ID = 3, type = 'g', x = 78, y = 22, heartSpace = true, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 4, } });
            //boardSpaces.Add(new BoardSpace() { ID = 4, type = 'g', x = 106, y = 25, heartSpace = true, greenSpaceEvent = "tentacleDrag", destIDs_DONTUSE = new List<int> { 5, } });
            //boardSpaces.Add(new BoardSpace() { ID = 5, type = 'g', x = 117, y = 48, heartSpace = true, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 6, } });
            //boardSpaces.Add(new BoardSpace() { ID = 6, type = 'g', x = 106, y = 67, heartSpace = true, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 7, } });
            //boardSpaces.Add(new BoardSpace() { ID = 7, type = 'g', x = 78, y = 74, heartSpace = true, greenSpaceEvent = "gondola", destIDs_DONTUSE = new List<int> { 8, } });
            //boardSpaces.Add(new BoardSpace() { ID = 8, type = 'g', x = 51, y = 76, heartSpace = true, greenSpaceEvent = "gondola", destIDs_DONTUSE = new List<int> { 9, } });
            //boardSpaces.Add(new BoardSpace() { ID = 9, type = 'b', x = 37, y = 61, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 1, } });
            //boardSpaces.Add(new BoardSpace() { ID = 10, type = 'b', x = 26, y = -4, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 11, } });
            //boardSpaces.Add(new BoardSpace() { ID = 11, type = 'i', x = 63, y = -5, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 4, } });
            // Wiggler's Garden
            //boardSpaces.Add(new BoardSpace() { ID = 0, type = BoardSpaceType.Start, x = 54, y = 91, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 1, } });
            //boardSpaces.Add(new BoardSpace() { ID = 1, type = BoardSpaceType.Blue, x = 54, y = 107, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 2, } });
            //boardSpaces.Add(new BoardSpace() { ID = 2, type = BoardSpaceType.Blue, x = 33, y = 101, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 16, 3, } });
            //boardSpaces.Add(new BoardSpace() { ID = 3, type = BoardSpaceType.Red, x = 16, y = 94, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 4, } });
            //boardSpaces.Add(new BoardSpace() { ID = 4, type = BoardSpaceType.Event, x = 3, y = 82, heartSpace = true, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 5, } });
            //boardSpaces.Add(new BoardSpace() { ID = 5, type = BoardSpaceType.Blue, x = -5, y = 61, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 6, } });
            //boardSpaces.Add(new BoardSpace() { ID = 6, type = BoardSpaceType.Shop, x = -10, y = 43, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 7, } });
            //boardSpaces.Add(new BoardSpace() { ID = 7, type = BoardSpaceType.Event, x = 3, y = 29, heartSpace = true, greenSpaceEvent = "tentacleDrag", destIDs_DONTUSE = new List<int> { 8, } });
            //boardSpaces.Add(new BoardSpace() { ID = 8, type = BoardSpaceType.Blue, x = 11, y = 19, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 9, 10, } });
            //boardSpaces.Add(new BoardSpace() { ID = 9, type = BoardSpaceType.Blue, x = 11, y = 1, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 20, } });
            //boardSpaces.Add(new BoardSpace() { ID = 10, type = BoardSpaceType.Blue, x = 32, y = 16, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 11, } });
            //boardSpaces.Add(new BoardSpace() { ID = 11, type = BoardSpaceType.Blue, x = 52, y = 14, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 12, } });
            //boardSpaces.Add(new BoardSpace() { ID = 12, type = BoardSpaceType.Red, x = 70, y = 22, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 13, } });
            //boardSpaces.Add(new BoardSpace() { ID = 13, type = BoardSpaceType.Event, x = 70, y = 38, heartSpace = true, greenSpaceEvent = "gondola", destIDs_DONTUSE = new List<int> { 14, } });
            //boardSpaces.Add(new BoardSpace() { ID = 14, type = BoardSpaceType.Red, x = 68, y = 54, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 19, } });
            //boardSpaces.Add(new BoardSpace() { ID = 15, type = BoardSpaceType.Red, x = 23, y = 41, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 8, } });
            //boardSpaces.Add(new BoardSpace() { ID = 16, type = BoardSpaceType.Blue, x = 33, y = 83, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 17, } });
            //boardSpaces.Add(new BoardSpace() { ID = 17, type = BoardSpaceType.Red, x = 30, y = 69, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 18, } });
            //boardSpaces.Add(new BoardSpace() { ID = 18, type = BoardSpaceType.Event, x = 36, y = 57, heartSpace = false, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 15, } });
            //boardSpaces.Add(new BoardSpace() { ID = 19, type = BoardSpaceType.Blue, x = 50, y = 60, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 18, } });
            //boardSpaces.Add(new BoardSpace() { ID = 20, type = BoardSpaceType.Event, x = 27, y = -3, heartSpace = true, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 21, } });
            //boardSpaces.Add(new BoardSpace() { ID = 21, type = BoardSpaceType.Event, x = 44, y = -8, heartSpace = true, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 22, } });
            //boardSpaces.Add(new BoardSpace() { ID = 22, type = BoardSpaceType.Event, x = 61, y = -13, heartSpace = true, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 23, } });
            //boardSpaces.Add(new BoardSpace() { ID = 23, type = BoardSpaceType.Blue, x = 86, y = -8, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 24, } });
            //boardSpaces.Add(new BoardSpace() { ID = 24, type = BoardSpaceType.Blue, x = 110, y = 7, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 25, } });
            //boardSpaces.Add(new BoardSpace() { ID = 25, type = BoardSpaceType.Blue, x = 117, y = 27, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 28, } });
            //boardSpaces.Add(new BoardSpace() { ID = 26, type = BoardSpaceType.Blue, x = 116, y = 71, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 29, } });
            //boardSpaces.Add(new BoardSpace() { ID = 27, type = BoardSpaceType.Event, x = 95, y = 54, heartSpace = true, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 14, } });
            //boardSpaces.Add(new BoardSpace() { ID = 28, type = BoardSpaceType.Blue, x = 119, y = 47, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 26, 27, } });
            //boardSpaces.Add(new BoardSpace() { ID = 29, type = BoardSpaceType.Blue, x = 105, y = 87, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 30, } });
            //boardSpaces.Add(new BoardSpace() { ID = 30, type = BoardSpaceType.Red, x = 80, y = 86, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 31, } });
            //boardSpaces.Add(new BoardSpace() { ID = 31, type = BoardSpaceType.Blue, x = 71, y = 103, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 1, } });
            // Cycle Madness
            //boardSpaces.Add(new BoardSpace() { ID = 0, type = 's', x = 96, y = 89, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 1, } });
            //boardSpaces.Add(new BoardSpace() { ID = 1, type = 'b', x = 72, y = 92, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 2, } });
            //boardSpaces.Add(new BoardSpace() { ID = 2, type = 'a', x = 80, y = 112, heartSpace = false, greenSpaceEvent = "gondola", destIDs_DONTUSE = new List<int> { 3, } });
            //boardSpaces.Add(new BoardSpace() { ID = 3, type = 'b', x = 52, y = 115, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 27, } });
            //boardSpaces.Add(new BoardSpace() { ID = 4, type = 'b', x = 8, y = 89, heartSpace = true, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 5, } });
            //boardSpaces.Add(new BoardSpace() { ID = 5, type = 'r', x = -7, y = 63, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 6, } });
            //boardSpaces.Add(new BoardSpace() { ID = 6, type = 'a', x = 11, y = 42, heartSpace = false, greenSpaceEvent = "gondola", destIDs_DONTUSE = new List<int> { 7, } });
            //boardSpaces.Add(new BoardSpace() { ID = 7, type = 'b', x = 29, y = 54, heartSpace = true, greenSpaceEvent = "tentacleDrag", destIDs_DONTUSE = new List<int> { 24, } });
            //boardSpaces.Add(new BoardSpace() { ID = 8, type = 'g', x = 8, y = 15, heartSpace = true, greenSpaceEvent = "tentacleDrag", destIDs_DONTUSE = new List<int> { 9, } });
            //boardSpaces.Add(new BoardSpace() { ID = 9, type = 'b', x = 5, y = -3, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 11, } });
            //boardSpaces.Add(new BoardSpace() { ID = 10, type = 'r', x = 28, y = 23, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 8, } });
            //boardSpaces.Add(new BoardSpace() { ID = 11, type = 'g', x = 26, y = -12, heartSpace = false, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 12, } });
            //boardSpaces.Add(new BoardSpace() { ID = 12, type = 'b', x = 51, y = -15, heartSpace = true, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 13, } });
            //boardSpaces.Add(new BoardSpace() { ID = 13, type = 'i', x = 77, y = -15, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 26, } });
            //boardSpaces.Add(new BoardSpace() { ID = 14, type = 'b', x = 102, y = 12, heartSpace = false, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 21, } });
            //boardSpaces.Add(new BoardSpace() { ID = 15, type = 'a', x = 62, y = 35, heartSpace = false, greenSpaceEvent = "gondola", destIDs_DONTUSE = new List<int> { 23, } });
            //boardSpaces.Add(new BoardSpace() { ID = 16, type = 'r', x = 121, y = 88, heartSpace = true, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 18, } });
            //boardSpaces.Add(new BoardSpace() { ID = 17, type = 'b', x = 120, y = 66, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 16, } });
            //boardSpaces.Add(new BoardSpace() { ID = 18, type = 'b', x = 107, y = 81, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 19, } });
            //boardSpaces.Add(new BoardSpace() { ID = 19, type = 'g', x = 79, y = 71, heartSpace = true, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 20, } });
            //boardSpaces.Add(new BoardSpace() { ID = 20, type = 'b', x = 58, y = 61, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 22, } });
            //boardSpaces.Add(new BoardSpace() { ID = 21, type = 'b', x = 84, y = 18, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 25, } });
            //boardSpaces.Add(new BoardSpace() { ID = 22, type = 'g', x = 42, y = 41, heartSpace = false, greenSpaceEvent = "tentacleDrag", destIDs_DONTUSE = new List<int> { 15, } });
            //boardSpaces.Add(new BoardSpace() { ID = 23, type = 'i', x = 99, y = 42, heartSpace = false, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 17, } });
            //boardSpaces.Add(new BoardSpace() { ID = 24, type = 'b', x = 42, y = 77, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 1, } });
            //boardSpaces.Add(new BoardSpace() { ID = 25, type = 'b', x = 55, y = 22, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 10, } });
            //boardSpaces.Add(new BoardSpace() { ID = 26, type = 'a', x = 96, y = -8, heartSpace = false, greenSpaceEvent = "tentacleDrag", destIDs_DONTUSE = new List<int> { } });
            //boardSpaces.Add(new BoardSpace() { ID = 27, type = 'b', x = 26, y = 105, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 4, } });
        }

        public static void Load() {
            MultiplayerSingleton.Instance.RegisterHandler<DieRoll>(HandleDieRoll);
            MultiplayerSingleton.Instance.RegisterHandler<PlayerChoice>(HandlePlayerChoice);
            MultiplayerSingleton.Instance.RegisterHandler<MinigameStart>(HandleMinigameStart);
            MultiplayerSingleton.Instance.RegisterHandler<UseItemMenu>(HandleUseItemMenu);
            MultiplayerSingleton.Instance.RegisterHandler<UseItem>(HandleUseItem);
        }

        public static void LoadContent() {
            spaceTextures = new() {
                [BoardSpaceType.Red] = GFX.Game["decals/madelineparty/redspace"],
                [BoardSpaceType.Blue] = GFX.Game["decals/madelineparty/bluespace"],
                [BoardSpaceType.Event] = GFX.Game["decals/madelineparty/greenspace"],
                [BoardSpaceType.ImmediateEvent] = GFX.Game["decals/madelineparty/autogreenspace"],
                [BoardSpaceType.Shop] = GFX.Game["decals/madelineparty/shopspace"]
            };

            greenSpaces = new();
            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                foreach(var type in assembly.GetTypesSafe()) {
                    if(type.IsDefined(typeof(GreenSpaceAttribute), false) && typeof(GreenSpaceEvent).IsAssignableFrom(type)) {
                        GreenSpaceEvent gse = type.GetConstructor(new Type[0]).Invoke(new object[0]) as GreenSpaceEvent;
                        greenSpaces[type.GetCustomAttributes<GreenSpaceAttribute>().First().id] = gse;
                    }
                }
            }
            foreach (var space in greenSpaces.Values) {
                space.LoadContent();
            }
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            level = SceneAs<Level>();
            level.CanRetry = false;
            level.Add(new TurnDisplay());
            level.Add(new SubHUDRenderer(this));
        }

        public static bool hackfixRespawn;

        public override void Awake(Scene scene) {
            base.Awake(scene);
            //if (!hackfixRespawn) { //FIXME hackfix
            //    hackfixRespawn = true;
            //    scene.Tracker.GetEntity<Player>().Die(Vector2.Zero);
            //    return;
            //}
            LoadBoardSpaces();
            PlaceTokens();

            List<LeftButton> found = scene.Entities.FindAll<LeftButton>();
            found.Sort();
            var textbox = new PersistentMiniTextbox("MadelineParty_Start", pauseUpdate: true, time: 1.7f);
            for (int i = 0; i < found.Count; i++) {
                leftButtons.Add(found[i]);
                if (GameData.Instance.players[i] != null && !GameData.Instance.gameStarted) {
                    int p = i;
                    textbox.OnFinish += () => SetDice(p);
                } else {
                    found[i].SetCurrentMode(LeftButton.Modes.Inactive);
                }
            }
            
            List<RightButton> foundRight = scene.Entities.FindAll<RightButton>();
            foundRight.Sort();
            for (int i = 0; i < foundRight.Count; i++) {
                rightButtons.Add(foundRight[i]);
                foundRight[i].SetCurrentMode(RightButton.Modes.Inactive);
            }

            List<GameScoreboard> foundScoreboard = scene.Entities.FindAll<GameScoreboard>();
            foundScoreboard.Sort();
            for (int i = 0; i < foundScoreboard.Count; i++) {
                scoreboards.Add(foundScoreboard[i]);
            }

            // Make sure turn order is determined after minigame completion
            if (GameData.Instance.gameStarted) {
                List<PlayerData> temp = new(GameData.Instance.players);
                temp.Sort();
                int playersGoneThrough = 0;
                for (int i = 0; i < temp.Count; i++) {
                    if (temp[i] == null) continue;
                    turnOrder[playersGoneThrough] = temp[i].TokenSelected;
                    playersGoneThrough++;
                }
                status = BoardStatus.WAITING;
                Alarm.Set(this, 0.5f, delegate {
                    ChangeTurn(turnOrder[0]);
                });

                if (delayedDieRoll != null) {
                    if (isWaitingOnPlayer(GameData.Instance.playerSelectTriggers[delayedDieRoll.ID])) {
                        string rollString = "";
                        foreach (int i in delayedDieRoll.rolls) {
                            rollString += i + ", ";
                        }
                        Logger.Log("MadelineParty", "Delayed data interpreted as die roll from player " + delayedDieRoll.DisplayName + ". Rolls: " + rollString);

                        RollDice(GameData.Instance.playerSelectTriggers[delayedDieRoll.ID], delayedDieRoll.rolls);
                    }
                    delayedDieRoll = null;
                }

                if(queuedStrawberries.Count > 0) {
                    foreach(var kvp in queuedStrawberries) {
                        ChangeStrawberries(kvp.Key, kvp.Value);
                    }
                    queuedStrawberries.Clear();
                }
            } else {
                // FIXME wipe isn't being found
                if (level.Wipe != null) {
                    Action onComplete = level.Wipe.OnComplete;
                    level.Wipe.OnComplete = delegate {
                        Scene.Add(textbox);
                        onComplete?.Invoke();
                    };
                } else {
                    Scene.Add(textbox);
                }
                if (heartStartID < 0) {
                    List<BoardSpace> possibleHeartSpaces = boardSpaces.FindAll(s => s.heartSpace);
                    heartStartID = possibleHeartSpaces[new Random((int)(GameData.Instance.tieBreakerSeed - int.MaxValue) - 557).Next(possibleHeartSpaces.Count)].ID;
                }
                GameData.Instance.heartSpaceID = heartStartID;
            }

            // Re-add heart blocks
            for (int i = 0; i < GameData.Instance.heartBlocks.Count; i++) {
                Console.WriteLine(boardSpaces.Find(s => s.ID == GameData.Instance.heartSpaceID).screenPosition - new Vector2(48));
                scene.Add(new HeartBlock(
                            boardSpaces.Find(s => s.ID == GameData.Instance.heartSpaceID).screenPosition - new Vector2(48), 48, 48));
            }

            GameData.Instance.gameStarted = true;
        }

        private void LoadBoardSpaces() {
            boardSpaces.Clear();
            foreach (EntityData data in boardEntityData) {
                Vector2 pos = data.Position + level.LevelOffset - Position;
                boardSpaces.Add(new BoardSpace {
                    ID = data.ID,
                    type = data.Enum("type", BoardSpaceType.Blue),
                    x = (int)pos.X,
                    y = (int)pos.Y,
                    heartSpace = data.Bool("heart_space", false),
                    greenSpaceEvent = data.Attr("event_ID"),
                    destIDs_DONTUSE = new()
                });
                foreach (var node in data.Nodes) {
                    boardSpaces.Last().destIDs_DONTUSE.Add(
                        boardEntityData.OrderBy(e => (e.Position - node).LengthSquared())
                                       .First(e => e.ID != boardSpaces.Last().ID)
                                       .ID);
                }
            }
        }

        private void PlaceTokens() {
            var space = boardSpaces.Find(s => s.type == BoardSpaceType.Start);
            int tokensAdded = 0;
            for (int k = 0; k < GameData.Instance.players.Length; k++) {
                if (GameData.Instance.players[k] != null) {
                    if (!GameData.Instance.gameStarted) {
                        // TODO Convert token offset to use same method as calculateAdjustedSpacePosition()
                        PlayerToken token = new(k, TokenPaths[GameData.Instance.players[k].TokenSelected], space.screenPosition + new Vector2(0, tokensAdded * TOKEN_SPACING), new Vector2(.25f, .25f), -1, space);
                        playerTokens[k] = token;
                        GameData.Instance.players[k].token = token;
                        GameData.Instance.players[k].pastBoardSpaceIDs.Clear();
                        GameData.Instance.players[k].pastBoardSpaceIDs.Add(space.ID);
                    } else {
                        playerTokens[k] = GameData.Instance.players[k].token;
                    }
                    tokensAdded++;
                }
            }

            for (int k = playerTokens.Length - 1; k >= 0; k--) {
                if (playerTokens[k] != null) level.Add(playerTokens[k]);
            }
        }

        public GameScoreboard GetScoreboard(PlayerToken player) {
            return GetScoreboard(player.id);
        }

        public GameScoreboard GetScoreboard(int player) {
            return scoreboards[player];
        }

        public LeftButton GetLeftButton(PlayerToken player) {
            return GetLeftButton(player.id);
        }

        public LeftButton GetLeftButton(int player) {
            return leftButtons[player];
        }

        public void SetLeftButtonStatus(PlayerToken player, LeftButton.Modes mode) {
            SetLeftButtonStatus(player.id, mode);
        }

        public void SetLeftButtonStatus(int player, LeftButton.Modes mode) {
            leftButtons[player].SetCurrentMode(mode);
        }

        public RightButton GetRightButton(PlayerToken player) {
            return GetRightButton(player.id);
        }

        public RightButton GetRightButton(int player) {
            return rightButtons[player];
        }

        public void SetRightButtonStatus(PlayerToken player, RightButton.Modes mode) {
            SetRightButtonStatus(player.id, mode);
        }

        public void SetRightButtonStatus(int player, RightButton.Modes mode) {
            rightButtons[player].SetCurrentMode(mode);
        }

        public void SetDice(int player) {
            SetLeftButtonStatus(player, LeftButton.Modes.Dice);
        }

        public void SetUseItem(int player) {
            if(GameData.Instance.players[player].Items.Count(item => item.CanUseInTurn) == 1) {
                SetRightButtonStatus(player, RightButton.Modes.SingleItem);
            } else {
                SetRightButtonStatus(player, RightButton.Modes.UseItem);
            }
        }

        private void ChangeTurn(int player) {
            SetDice(player);
            if(GameData.Instance.players[player].Items.Count(item => item.CanUseInTurn) > 0) {
                SetUseItem(player);
            }
            if(level.Wipe != null) {
                Action onComplete = level.Wipe.OnComplete;
                level.Wipe.OnComplete = delegate {
                    level.Add(new PersistentMiniTextbox(GetCurrentTurnText(player), pauseUpdate: true, time: 3));
                    onComplete?.Invoke();
                    if (GameData.Instance.heartBlocks.Remove(player)) {
                        Alarm.Set(this, 0.7f, () => level.Tracker.GetEntity<HeartBlock>()?.FadeOut());
                    }
                };
            } else {
                level.Add(new PersistentMiniTextbox(GetCurrentTurnText(player), pauseUpdate: true, time: 3));
                if (GameData.Instance.heartBlocks.Remove(player)) {
                    Alarm.Set(this, 0.7f, () => level.Tracker.GetEntity<HeartBlock>()?.FadeOut());
                }
            }
        }

        private string GetCurrentTurnText(int player) {
            // First, set the name to use as a dialog entry
            Dialog.Language.Dialog["MadelineParty_Current_Turn_Name"] = GameData.Instance.GetPlayerName(player);
            return GameData.Instance.GetRandomDialogID((playerTurn == 0 && GameData.Instance.turn == 1) ? "MadelineParty_First_Turn_Text_List" : "MadelineParty_Current_Turn_Text_List");
        }

        private Vector2 ScreenCoordsFromBoardCoords(Vector2 boardCoords) {
            return ScreenCoordsFromBoardCoords(boardCoords, new Vector2(0, 0));
        }

        private Vector2 ScreenCoordsFromBoardCoords(Vector2 boardCoords, Vector2 offsetInPxls) {
            return new Vector2((X - level.LevelOffset.X + boardCoords.X * 10) * 6 + offsetInPxls.X, (Y - level.LevelOffset.Y + boardCoords.Y * 10) * 6 + offsetInPxls.Y);
        }

        // Calculate where the player should end up to avoid overlapping with other players too much
        // Players on the same space should roughly splay out in a line perpendicular to the next space to move
        // If there are two spaces the player could go to, the destination should be considered as the average of the two spaces
        private void calculateAdjustedSpacePositions(BoardSpace space) {
            adjustedSpacePositions.Clear();
            var avg = Vector2.Zero;
            var destinations = space.GetDestinations(boardSpaces);
            foreach (var dst in destinations) {
                avg += (space.screenPosition - dst.screenPosition).SafeNormalize();
            }
            avg.Normalize();

            var playersHere = playerTokens.Where(t => t != null && t.currentSpace.Equals(space)).ToList();

            for(int i = 0; i < playersHere.Count; i++) {
                playersHere[i].Depth = -i;
                adjustedSpacePositions[playersHere[i].id] = space.screenPosition + avg.Perpendicular() * TOKEN_SPACING * (i - playersHere.Count / 2f);
            }
        }

        public override void Update() {
            base.Update();
            switch (status) {
                case BoardStatus.PLAYERMOVE:
                    if (playerMoveProgress == playerMoveDistance // If we've hit our destination
                            || (playerMoveProgress + 1 < playerMovePath.Count && playerMovePath[playerMoveProgress + 1].ID == GameData.Instance.heartSpaceID
                                && GameData.Instance.heartBlocks.Count > 0)) { // Or if we're blocked by a heart block
                        playerMoveDistance = 0;
                        playerMovePath = null;
                        playerMoveProgress = 0;
                        status = BoardStatus.ADJUST_POSITION;
                        calculateAdjustedSpacePositions(CurrentPlayerToken.currentSpace);
                        break;
                    }
                    // If we've reached the end of the path but not the end of our movement
                    // We must be at an intersection
                    if (playerMoveProgress == playerMovePath.Count - 1) {
                        playerMoveDistance -= playerMoveProgress;
                        playerMoveProgress = 0;
                        playerMovePath = new List<BoardSpace> { CurrentPlayerToken.currentSpace };
                        status = BoardStatus.WAITING;

                        if (playerMovePath[playerMoveProgress].GetDestinations(boardSpaces).Count > 2) {
                            throw new NotSupportedException("Intersections with more than two places to go are not supported");
                        }
                        bool leftUsed = false;
                        foreach (BoardSpace dest in playerMovePath[playerMoveProgress].GetDestinations(boardSpaces)) {
                            // Skip blocked spaces
                            if(dest.ID == GameData.Instance.heartSpaceID && GameData.Instance.heartBlocks.Count > 0) {
                                continue;
                            }

                            Direction dir = getCardinalDirection(playerMovePath[playerMoveProgress].x, playerMovePath[playerMoveProgress].y, dest.x, dest.y);

                            if (leftUsed) {
                                SetRightButtonStatus(CurrentPlayerToken, (RightButton.Modes)Enum.Parse(typeof(RightButton.Modes), dir.ToString()));
                            } else {
                                SetLeftButtonStatus(CurrentPlayerToken, (LeftButton.Modes)Enum.Parse(typeof(LeftButton.Modes), dir.ToString()));
                                leftUsed = true;
                            }
                        }
                        break;
                    }
                    // If we're not at an intersection
                    BoardSpace approaching = playerMovePath[playerMoveProgress + 1];
                    // Check if we've hit our next space
                    if (CurrentPlayerToken.Position.Equals(approaching.screenPosition)) {
                        playerMoveProgress++;
                        CurrentPlayerToken.currentSpace = playerMovePath[playerMoveProgress];
                        GameData.Instance.players[movingPlayerID].pastBoardSpaceIDs.Add(playerMovePath[playerMoveProgress].ID);

                        // If we're on the heart space
                        if (GameData.Instance.heartSpaceID == CurrentPlayerToken.currentSpace.ID) {
                            bool canAfford = GameData.Instance.players[movingPlayerID].Strawberries >= GameData.Instance.heartCost;
                            status = BoardStatus.WAITING;
                            if (canAfford) {
                                SetLeftButtonStatus(CurrentPlayerToken, LeftButton.Modes.ConfirmHeartBuy);
                            }
                            SetRightButtonStatus(CurrentPlayerToken, RightButton.Modes.CancelHeartBuy);
                            scoreboards[turnOrder[playerTurn]].SetCurrentMode(GameScoreboard.Modes.BUYHEART);
                            Dialog.Language.Dialog["MadelineParty_Heart_Cost"] = GameData.Instance.heartCost.ToString();
                            level.Add(new PersistentMiniTextbox(GameData.Instance.GetRandomDialogID(canAfford ? "MadelineParty_Buy_Heart_Prompt_List" : "MadelineParty_Heart_TooPoor_List"), pauseUpdate: true, time: 3));
                        }
                        // If we're at the item shop and have enough free space
                        else if (CurrentPlayerToken.currentSpace.type == BoardSpaceType.Shop && GameData.Instance.players[movingPlayerID].Items.Count < GameData.MAX_ITEMS) {
                            status = BoardStatus.WAITING;
                            SetLeftButtonStatus(CurrentPlayerToken, LeftButton.Modes.ConfirmShopEnter);
                            SetRightButtonStatus(CurrentPlayerToken, RightButton.Modes.CancelShopEnter);
                            scoreboards[turnOrder[playerTurn]].SetCurrentMode(GameScoreboard.Modes.ENTERSHOP);
                            level.Add(new PersistentMiniTextbox(GameData.Instance.GetRandomDialogID("MadelineParty_Enter_Shop_Prompt_List"), pauseUpdate: true, time: 3));
                        } else if (CurrentPlayerToken.currentSpace.type == BoardSpaceType.ImmediateEvent) { // If this is an auto green space
                            status = BoardStatus.WAITING;
                            DoGreenSpace(CurrentPlayerToken.currentSpace, () => {
                                AfterChoice();

                                if (playerMoveProgress != playerMoveDistance) {
                                    // Regenerate the path in case the player has been moved
                                    playerMovePath[playerMoveProgress] = CurrentPlayerToken.currentSpace;
                                    playerMovePath.RemoveRange(playerMoveProgress + 1, playerMovePath.Count - playerMoveProgress - 1);
                                    for (int i = playerMoveProgress; i < playerMoveDistance; i++) {
                                        if (playerMovePath[i].GetDestinations(boardSpaces).Count == 1) {
                                            playerMovePath.Add(playerMovePath[i].GetDestinations(boardSpaces)[0]);
                                        } else {
                                            break;
                                        }
                                    }
                                }
                            });
                        }
                        break;
                    }
                    CurrentPlayerToken.Position = Calc.Approach(CurrentPlayerToken.Position, approaching.screenPosition, TOKEN_SPEED * Engine.DeltaTime);
                    break;
                case BoardStatus.ADJUST_POSITION:
                    var stillMoving = false;
                    foreach(var kvp in adjustedSpacePositions) {
                        if ((playerTokens[kvp.Key].Position - kvp.Value).LengthSquared() >= 0.1f) {
                            stillMoving = true;
                        }
                        playerTokens[kvp.Key].Position = Calc.Approach(playerTokens[kvp.Key].Position, kvp.Value, TOKEN_SPEED * Engine.DeltaTime);
                    }
                    if(!stillMoving) {
                        status = BoardStatus.WAITING;
                        HandleSpaceAction(EndTurn);
                    }
                    
                    break;
            }
        }

        private void HandleSpaceAction(Action next) {
            var space = CurrentPlayerToken.currentSpace;
            // Don't do an action if we're on the heart space
            if (GameData.Instance.heartSpaceID == space.ID) {
                next();
                return;
            }
            
            if(!MadelinePartyModule.SaveData.SpacesHit.ContainsKey(space.type)) {
                MadelinePartyModule.SaveData.SpacesHit[space.type] = 0;
            }
            MadelinePartyModule.SaveData.SpacesHit[space.type]++;
            
            switch (space.type) {
                case BoardSpaceType.Blue:
                    ChangeStrawberries(movingPlayerID, 3);
                    next();
                    break;
                case BoardSpaceType.Red:
                    ChangeStrawberries(movingPlayerID, -3);
                    next();
                    break;
                case BoardSpaceType.Event:
                    DoGreenSpace(space, next);
                    break;
                default:
                    next();
                    break;
            }
        }

        public void ChangeStrawberries(int playerID, int amt, float changeSpeed = 0.25f) {
            scoreboards[playerID].StrawberryChange(amt, changeSpeed);
            GameData.Instance.players[playerID].ChangeStrawberries(amt);
        }

        public static void QueueStrawberryChange(int playerID, int amt) {
            if(!queuedStrawberries.ContainsKey(playerID)) {
                queuedStrawberries[playerID] = amt;
            } else {
                queuedStrawberries[playerID] += amt;
            }
        }

        public void SkipItem() {
            // Only send out data if we are the player that skipped the item
            if (turnOrder[playerTurn] == GameData.Instance.realPlayerID) {
                MultiplayerSingleton.Instance.Send(new PlayerChoice { choiceType = "SHOPITEM", choice = 1 });
            }
            shopItemViewing++;
            if (shopItemViewing < GameData.Instance.shopContents.Count) {
                var nextItem = GameData.items[GameData.Instance.shopContents[shopItemViewing]];
                scoreboards[turnOrder[playerTurn]].SetCurrentMode(GameScoreboard.Modes.BUYITEM, nextItem);
                if (GameData.Instance.players[turnOrder[playerTurn]].Strawberries >= nextItem.Price) {
                    SetLeftButtonStatus(CurrentPlayerToken, LeftButton.Modes.ConfirmItemBuy);
                } else {
                    SetLeftButtonStatus(CurrentPlayerToken, LeftButton.Modes.Inactive);
                }
            } else {
                scoreboards[turnOrder[playerTurn]].SetCurrentMode(GameScoreboard.Modes.NORMAL);
                SetLeftButtonStatus(CurrentPlayerToken, LeftButton.Modes.Inactive);
                SetRightButtonStatus(CurrentPlayerToken, RightButton.Modes.Inactive);
                AfterChoice();
            }
        }

        public void BuyItem() {
            var itemBought = GameData.items[GameData.Instance.shopContents[shopItemViewing]];
            // Only send out data if we are the player that bought the item
            if (turnOrder[playerTurn] == GameData.Instance.realPlayerID) {
                MultiplayerSingleton.Instance.Send(new PlayerChoice { choiceType = "SHOPITEM", choice = 0 });
                if(!MadelinePartyModule.SaveData.ItemsBought.ContainsKey(itemBought.Name)) {
                    MadelinePartyModule.SaveData.ItemsBought[itemBought.Name] = 0;
                }
                MadelinePartyModule.SaveData.ItemsBought[itemBought.Name]++;
            }
            GameData.Instance.players[turnOrder[playerTurn]].Items.Add(itemBought);
            shopItemViewing = 0;
            scoreboards[turnOrder[playerTurn]].SetCurrentMode(GameScoreboard.Modes.NORMAL);
            SetLeftButtonStatus(CurrentPlayerToken, LeftButton.Modes.Inactive);
            SetRightButtonStatus(CurrentPlayerToken, RightButton.Modes.Inactive);

            ChangeStrawberries(turnOrder[playerTurn], -itemBought.Price, .08f);
            AfterChoice();
        }

        public void SkipShop() {
            // Only send out data if we are the player that skipped the shop
            if (turnOrder[playerTurn] == GameData.Instance.realPlayerID) {
                MultiplayerSingleton.Instance.Send(new PlayerChoice { choiceType = "ENTERSHOP", choice = 1 });
            }
            scoreboards[turnOrder[playerTurn]].SetCurrentMode(GameScoreboard.Modes.NORMAL);
            SetLeftButtonStatus(CurrentPlayerToken, LeftButton.Modes.Inactive);

            AfterChoice();
        }

        public void EnterShop() {
            // Only send out data if we are the player that entered the shop
            if (turnOrder[playerTurn] == GameData.Instance.realPlayerID) {
                MultiplayerSingleton.Instance.Send(new PlayerChoice { choiceType = "ENTERSHOP", choice = 0 });
            }
            shopItemViewing = 0;
            var firstItem = GameData.items[GameData.Instance.shopContents[shopItemViewing]];
            scoreboards[turnOrder[playerTurn]].SetCurrentMode(GameScoreboard.Modes.BUYITEM, firstItem);
            SetRightButtonStatus(CurrentPlayerToken, RightButton.Modes.CancelItemBuy);
            if (GameData.Instance.players[turnOrder[playerTurn]].Strawberries >= firstItem.Price) {
                SetLeftButtonStatus(CurrentPlayerToken, LeftButton.Modes.ConfirmItemBuy);
            } else {
                SetLeftButtonStatus(CurrentPlayerToken, LeftButton.Modes.Inactive);
            }
        }

        public void SkipHeart() {
            // Only send out data if we are the player that skipped the heart
            if (turnOrder[playerTurn] == GameData.Instance.realPlayerID) {
                MultiplayerSingleton.Instance.Send(new PlayerChoice { choiceType = "HEART", choice = 1 });
            }
            scoreboards[turnOrder[playerTurn]].SetCurrentMode(GameScoreboard.Modes.NORMAL);
            SetLeftButtonStatus(CurrentPlayerToken, LeftButton.Modes.Inactive);

            AfterChoice();
        }

        public void BuyHeart() {
            // Only send out data if we are the player that bought the heart
            if (turnOrder[playerTurn] == GameData.Instance.realPlayerID) {
                MultiplayerSingleton.Instance.Send(new PlayerChoice { choiceType = "HEART", choice = 0 });
            }
            ChangeStrawberries(turnOrder[playerTurn], -GameData.Instance.heartCost, 0.08f);
            GameData.Instance.players[turnOrder[playerTurn]].AddHeart();
            scoreboards[turnOrder[playerTurn]].SetCurrentMode(GameScoreboard.Modes.NORMAL);
            rightButtons[turnOrder[playerTurn]].SetCurrentMode(RightButton.Modes.Inactive);

            GameData.Instance.heartSpaceID = -1;
            // Only send out data if we are the player that bought the heart
            if (turnOrder[playerTurn] == GameData.Instance.realPlayerID) {
                List<BoardSpace> possibleHeartSpaces = boardSpaces.FindAll((s) => s.heartSpace && GameData.Instance.players[turnOrder[playerTurn]].token.currentSpace.ID != s.ID);
                GameData.Instance.heartSpaceID = possibleHeartSpaces[rand.Next(possibleHeartSpaces.Count)].ID;
                MultiplayerSingleton.Instance.Send(new PlayerChoice { choiceType = "HEARTSPACEID", choice = GameData.Instance.heartSpaceID });
            }
            Add(new Coroutine(WaitForNewHeartSpaceCoroutine()));
        }

        private IEnumerator WaitForNewHeartSpaceCoroutine() {
            while (GameData.Instance.heartSpaceID < 0) {
                yield return null;
            }

            AfterChoice();
        }

        private void AfterChoice() {
            status = BoardStatus.PLAYERMOVE;
            SetLeftButtonStatus(CurrentPlayerToken, LeftButton.Modes.Inactive);
            SetRightButtonStatus(CurrentPlayerToken, RightButton.Modes.Inactive);

            if (playerMoveProgress == playerMoveDistance
                || (playerMovePath[playerMoveProgress + 1].ID == GameData.Instance.heartSpaceID && GameData.Instance.heartBlocks.Count > 0)) {
                playerMoveDistance = 0;
                playerMovePath = null;
                playerMoveProgress = 0;
                status = BoardStatus.WAITING;
                HandleSpaceAction(EndTurn);
            }
        }

        private Direction getCardinalDirection(int srcX, int srcY, int destX, int destY) {
            int dx = destX - srcX;
            int dy = destY - srcY;
            float steepness = Math.Abs(dy / (float)dx);
            if (steepness > 1) {
                if (dy < 0) {
                    return Direction.Up;
                } else {
                    return Direction.Down;
                }
            } else {
                if (dx > 0) {
                    return Direction.Right;
                } else {
                    return Direction.Left;
                }
            }
        }

        public void ContinueMovementAfterIntersection(Direction chosen) {
            // Only send out data if we are the player that chose the direction
            if (turnOrder[playerTurn] == GameData.Instance.realPlayerID) {
                MultiplayerSingleton.Instance.Send(new PlayerChoice { choiceType = "DIRECTION", choice = (int)chosen });
            }
            SetLeftButtonStatus(CurrentPlayerToken, LeftButton.Modes.Inactive);
            SetRightButtonStatus(CurrentPlayerToken, RightButton.Modes.Inactive);
            bool found = false;
            foreach (BoardSpace dest in playerMovePath[playerMoveProgress].GetDestinations(boardSpaces)) {
                if (getCardinalDirection(playerMovePath[playerMoveProgress].x, playerMovePath[playerMoveProgress].y, dest.x, dest.y) == chosen) {
                    found = true;
                    playerMovePath.Add(dest);
                    break;
                }
            }
            if (!found) {
                throw new Exception("Chose direction that doesn't exist???");
            }

            for (int i = 1; i < playerMoveDistance; i++) {
                if (playerMovePath[i].GetDestinations(boardSpaces).Count == 1) {
                    playerMovePath.Add(playerMovePath[i].GetDestinations(boardSpaces)[0]);
                } else {
                    break;
                }
            }

            status = BoardStatus.PLAYERMOVE;
        }

        public void EndTurn() {
            status = BoardStatus.WAITING;
            playerTurn++;
            if (playerTurn >= GameData.Instance.playerNumber) {
                MadelinePartyModule.SaveData.TurnsPlayed++;
                playerTurn = 0;
                turnDisplay = GameData.Instance.turn;
                GameData.Instance.turn++;
                Add(new Coroutine(InitiateMinigame()));
            } else {
                ChangeTurn(CurrentPlayerToken.id);
            }
        }

        public static void ChoseMinigame(string chosen, long startTime = 0) {
            GameData.Instance.minigame = chosen;
        }

        public IEnumerator InitiateMinigame() {
            yield return 1f;
            level.Add(new PersistentMiniTextbox(GameData.Instance.GetRandomDialogID("MadelineParty_Minigame_Time_List"), pauseUpdate: true));
            hackfixRespawn = false; //FIXME hackfix
            if (GameData.Instance.celesteNetHost) {
                List<LevelData> minigames = GameData.Instance.GetAllUnplayedMinigames(level);
                string chosenMinigame = minigames[rand.Next(minigames.Count)].Name;
                if (riggedMinigame != null && minigames.IndexOf(riggedMinigame) >= 0) {
                    chosenMinigame = minigames[minigames.IndexOf(riggedMinigame)].Name;
                    riggedMinigame = null;
                }
                minigameStartTime = DateTime.UtcNow.AddSeconds(3);
                Console.WriteLine("Minigame chosen: " + chosenMinigame);
                ChoseMinigame(chosenMinigame);
                MultiplayerSingleton.Instance.Send(new MinigameStart { choice = chosenMinigame, gameStart = minigameStartTime.ToFileTimeUtc() });
            }
            Console.WriteLine("Host? " + GameData.Instance.celesteNetHost);

            Console.WriteLine("Begin minigame wait");
            while (GameData.Instance.minigame == null /*|| minigameStartTime.CompareTo(DateTime.UtcNow) < 0*/) {
                yield return null;
            }

            var selectUI = new MinigameSelectUI(GameData.Instance.minigame);
            Scene.Add(selectUI);
            selectUI.OnSelect = selection => {
                GameData.Instance.playedMinigames.Add(selection);
                Player player = level.Tracker.GetEntity<Player>();
                level.OnEndOfFrame += delegate {
                    player.Speed = Vector2.Zero;
                    Leader.StoreStrawberries(player.Leader);
                    level.Remove(player);
                    level.UnloadLevel();

                    level.Session.Level = selection;
                    level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(level.Bounds.Left, level.Bounds.Top));
                    level.LoadLevel(Player.IntroTypes.None);
                    level.Session.Audio.Music.Event = GameData.GetMinigameMusic(selection);

                    Leader.RestoreStrawberries(player.Leader);
                };
            };
        }

        public void UseItem(int player, int itemIdx = 0) {
            // Skip over items that can't be used
            while (itemIdx < GameData.Instance.players[player].Items.Count && !GameData.Instance.players[player].Items[itemIdx].CanUseInTurn) {
                itemIdx++;
            }
            if (player == GameData.Instance.realPlayerID) {
                MultiplayerSingleton.Instance.Send(new UseItemMenu { player = player, index = itemIdx });
            }
            if (itemIdx == GameData.Instance.players[player].Items.Count) {
                SetDice(player);
                SetUseItem(player);
                scoreboards[player].SetCurrentMode(GameScoreboard.Modes.NORMAL);
                return;
            }
            scoreboards[player].SetCurrentMode(GameScoreboard.Modes.USEITEM, GameData.Instance.players[player].Items[itemIdx]);
            rightButtons[player].SetCurrentMode(RightButton.Modes.Cancel);
            leftButtons[player].SetCurrentMode(LeftButton.Modes.Confirm);
            rightButtons[player].OnPressButton += mode => {
                leftButtons[player].SetCurrentMode(LeftButton.Modes.Inactive);
                UseItem(player, itemIdx + 1);
            };
            leftButtons[player].OnPressButton += mode => {
                MultiplayerSingleton.Instance.Send(new UseItem { player = player, itemIdx = itemIdx });
                rightButtons[player].SetCurrentMode(RightButton.Modes.Inactive);
                GameData.Instance.players[player].Items[itemIdx].UseItem(player);
                GameData.Instance.players[player].Items.RemoveAt(itemIdx);
            };
        }

        // Returns which number player this is, ignoring unpicked characters
        private int getRelativePlayerID(int absPlayerID) {
            int currPlayer = 0;
            for (int i = 0; i < GameData.Instance.players.Length; i++) {
                if (i == absPlayerID) {
                    return currPlayer;
                }
                if (GameData.Instance.players[i] != null) {
                    currPlayer++;
                }
            }
            return currPlayer;
        }

        public IEnumerator DieRollAnimation(int playerID, int[] rolls, Action<int, int> after = null) {
            SetLeftButtonStatus(playerID, LeftButton.Modes.Inactive);
            SetRightButtonStatus(playerID, RightButton.Modes.Inactive);
            while (removingDieRolls) yield return null;
            foreach (int roll in rolls) {
                if (roll == 0) continue;
                DieNumber number = new(this, roll - 1, status == BoardStatus.GAMESTART ? getRelativePlayerID(playerID) : numbersToDisplay.Count);
                level.Add(number);
                numbersToDisplay.Add(number);
                // Number of expected die rolls
                int numberOfSpaces = status == BoardStatus.GAMESTART ? GameData.Instance.playerNumber : rolls.Length;
                // new Vector2(8, 4) for text instead of graphics
                number.MoveNumber((rolls.Length == 1 ? leftButtons[playerID].Position : rightButtons[playerID].Position) + new Vector2(0, 12) + new Vector2(8, 4), level.LevelOffset + new Vector2(level.Bounds.Width / 2 - 10 * (numberOfSpaces - 1) + 20 * number.posIndex, 8));
                yield return .25f;
            }
            yield return 3.25f;
            int rollSum = 0;
            foreach (int i in rolls) rollSum += i;
            (after ?? DiceRolled).Invoke(playerID, rollSum);
        }

        public IEnumerator RemoveDieRollsAnimation() {
            removingDieRolls = true;
            yield return .25f;
            foreach (DieNumber n in numbersToDisplay) {
                level.ParticlesFG.Emit(Refill.P_Shatter, 5, n.Position + new Vector2(4), Vector2.One * 2f, (float)Math.PI);
                level.ParticlesFG.Emit(Refill.P_Shatter, 5, n.Position + new Vector2(4), Vector2.One * 2f, 0);
                n.RemoveSelf();
                // TODO Some sort of editing while iterating error happened here, needs further looking into
                // Sodiumitis reported it
                yield return .25f;
            }
            numbersToDisplay.Clear();
            removingDieRolls = false;
        }

        public void RollDice(int playerID) {
            RollDice(playerID, 1);
        }

        // Usually called only due to Celestenet messages
        public void RollDice(int playerID, int[] rolls) {
            if (playerID == GameData.Instance.realPlayerID) {
                MultiplayerSingleton.Instance.Send(new DieRoll { rolls = rolls });
            }
            Add(new Coroutine(DieRollAnimation(playerID, rolls)));
        }

        public void RollDice(int playerID, int rollCount) {
            List<int> rolls = new List<int>();
            if (status == BoardStatus.GAMESTART) {
                rolls.Add(turnOrderRolls[playerID]);
            } else {
                if (riggedRoll > 0) {
                    rolls.Add(riggedRoll);
                    riggedRoll = -1;
                } else {
                    rolls.Add(rand.Next(10) + 1);
                }
                for(int i = 1; i < rollCount; i++) {
                    rolls.Add(rand.Next(10) + 1);
                }
            }
            if (playerID == GameData.Instance.realPlayerID) {
                MultiplayerSingleton.Instance.Send(new DieRoll { rolls = rolls.ToArray() });
            }
            Add(new Coroutine(DieRollAnimation(playerID, rolls.ToArray())));
        }

        public void DiceRolled(int playerID, int roll) {
            SetLeftButtonStatus(playerID, LeftButton.Modes.Inactive);
            SetRightButtonStatus(playerID, RightButton.Modes.Inactive);
            if (status == BoardStatus.GAMESTART) {
                GameData.Instance.players[playerID].StartingRoll = roll;
                Console.WriteLine("Roll: " + playerID + " " + roll);
                diceRolled++;
                if (diceRolled >= GameData.Instance.playerNumber) {
                    Add(new Coroutine(RemoveDieRollsAnimation()));
                    diceRolled = 0;
                    List<PlayerData> temp = new(GameData.Instance.players);
                    temp.Sort();
                    int playersGoneThrough = 0;
                    for (int i = 0; i < temp.Count; i++) {
                        if (temp[i] == null) continue;
                        turnOrder[playersGoneThrough] = temp[i].TokenSelected;
                        Logger.Log("MadelineParty", "Turn order log: Player ID " + turnOrder[playersGoneThrough] + " with a roll of " + temp[i].StartingRoll);
                        playersGoneThrough++;

                        ChangeStrawberries(temp[i].TokenSelected, 10);
                    }
                    
                    status = BoardStatus.WAITING;

                    Dialog.Language.Dialog["MadelineParty_Start_Berry_Count"] = GameData.START_BERRIES.ToString();
                    var textbox = new PersistentMiniTextbox("MadelineParty_Start_Free_Berries", pauseUpdate: true, time: 3);
                    textbox.OnFinish += () => ChangeTurn(turnOrder[0]);
                    Scene.Add(textbox);
                }
                return;
            }
            Add(new Coroutine(RemoveDieRollsAnimation()));
            movingPlayerID = playerID;
            playerMoveDistance = roll;
            playerMoveProgress = 0;
            playerMovePath = new List<BoardSpace>
            {
                playerTokens[playerID].currentSpace
            };
            for (int i = 0; i < playerMoveDistance; i++) {
                if (playerMovePath[i].GetDestinations(boardSpaces).Count == 1) {
                    playerMovePath.Add(playerMovePath[i].GetDestinations(boardSpaces)[0]);
                } else {
                    break;
                }
            }

            status = BoardStatus.PLAYERMOVE;
        }

        private static DynamicData spriteBatchData;
        public void SubHUDRender() {
            Draw.SpriteBatch.End();
            if (spriteBatchData is null) {
                spriteBatchData = DynamicData.For(Draw.SpriteBatch);
            }
            SamplerState before = spriteBatchData.Get<SamplerState>("samplerState");
            Matrix matrixBefore = spriteBatchData.Get<Matrix>("transformMatrix");
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred,
                spriteBatchData.Get<BlendState>("blendState"),
                SamplerState.PointClamp,
                spriteBatchData.Get<DepthStencilState>("depthStencilState"),
                spriteBatchData.Get<RasterizerState>("rasterizerState"),
                spriteBatchData.Get<Effect>("customEffect"),
                matrixBefore * Matrix.CreateTranslation(new Vector3(-level.ShakeVector.X, -level.ShakeVector.Y, 0) * 6));

            foreach (BoardSpace space in boardSpaces) {
                if (space.ID != GameData.Instance.heartSpaceID && !string.IsNullOrWhiteSpace(space.greenSpaceEvent) && greenSpaces.TryGetValue(space.greenSpaceEvent, out GreenSpaceEvent spaceEvent)) {
                    spaceEvent.RenderSubHUD(space, boardSpaces);
                }
            }

            Draw.SpriteBatch.End();
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred,
                spriteBatchData.Get<BlendState>("blendState"),
                before,
                spriteBatchData.Get<DepthStencilState>("depthStencilState"),
                spriteBatchData.Get<RasterizerState>("rasterizerState"),
                spriteBatchData.Get<Effect>("customEffect"),
                matrixBefore);
        }

        public override void Render() {
            base.Render();

            if (lineRenderTarget == null) {
                lineRenderTarget = VirtualContent.CreateRenderTarget("madelineparty-board-lines", level.Bounds.Width, level.Bounds.Height);
                GameplayRenderer.End();
                Engine.Graphics.GraphicsDevice.SetRenderTarget(lineRenderTarget);
                Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null,
                    Matrix.CreateTranslation(-level.Bounds.X, -level.Bounds.Y, 0));
                foreach (BoardSpace space in boardSpaces) {
                    Vector2 spacePos = Position + new Vector2(space.x, space.y);
                    foreach (BoardSpace dest in space.GetDestinations(boardSpaces)) {
                        Draw.Line(spacePos, Position + new Vector2(dest.x, dest.y), pathColor);
                    }
                }
                Draw.SpriteBatch.End();
                Engine.Instance.GraphicsDevice.SetRenderTarget(GameplayBuffers.Gameplay);
                GameplayRenderer.Begin();
            }
            
            Vector2 renderPos = level.Bounds.Location.ToVector2();
            Draw.SpriteBatch.Draw(lineRenderTarget, renderPos + Vector2.UnitX, pathOutlineColor);
            Draw.SpriteBatch.Draw(lineRenderTarget, renderPos - Vector2.UnitX, pathOutlineColor);
            Draw.SpriteBatch.Draw(lineRenderTarget, renderPos + Vector2.UnitY, pathOutlineColor);
            Draw.SpriteBatch.Draw(lineRenderTarget, renderPos - Vector2.UnitY, pathOutlineColor);
            Draw.SpriteBatch.Draw(lineRenderTarget, renderPos, pathColor);

            foreach (BoardSpace space in boardSpaces) {
                Vector2 spacePos = Position + new Vector2(space.x, space.y);
                if (space.ID != GameData.Instance.heartSpaceID) {
                    if(string.IsNullOrWhiteSpace(space.greenSpaceEvent) && greenSpaces.TryGetValue(space.greenSpaceEvent, out GreenSpaceEvent spaceEvent)) {
                        spaceEvent.Render(space, boardSpaces);
                    } else if (spaceTextures.ContainsKey(space.type)) {
                        spaceTextures[space.type].DrawCentered(spacePos);
                    }
                } else {
                    heartTexture.DrawCentered(spacePos, Color.White);
                }
            }
        }

        public static void generateTurnOrderRolls() {
            List<int> list = new List<int>(oneToTen);
            Random rand = new Random((int)GameData.Instance.turnOrderSeed);
            for (int i = 0; i < 4; i++) {
                turnOrderRolls[i] = list[rand.Next(list.Count)];
                list.Remove(turnOrderRolls[i]);
            }
        }

        // Whether one of the two buttons for the player specified is active
        public bool isWaitingOnPlayer(int playerID) {
            return leftButtons[playerID].GetCurrentMode() != LeftButton.Modes.Inactive || rightButtons[playerID].GetCurrentMode() != RightButton.Modes.Inactive;
        }

        public PlayerToken CurrentPlayerToken => playerTokens[turnOrder[playerTurn]];

        public void DoGreenSpace(BoardSpace space, Action next) {
            if(greenSpaces.TryGetValue(space.greenSpaceEvent, out GreenSpaceEvent spaceEvent)) {
                spaceEvent.RunGreenSpace(this, space, next ?? (() => { }));
            }
        }

        private static void HandleDieRoll(MPData data) {
            if (data is not DieRoll dieRoll) return;
            // If another player in our party has rolled the dice and we're waiting on them for an action
            if (GameData.Instance.celestenetIDs.Contains(dieRoll.ID) && dieRoll.ID != MultiplayerSingleton.Instance.CurrentPlayerID()) {

                if (!MadelinePartyModule.Instance.level.Session.Level.StartsWith("Board_")) {
                    // Activate it once in the right room
                    // This is so players that roll before everyone shows up don't break everything
                    delayedDieRoll = dieRoll;
                } else {
                    if (Instance?.isWaitingOnPlayer(GameData.Instance.playerSelectTriggers[dieRoll.ID]) ?? false) {
                        string rollString = "";
                        foreach (int i in dieRoll.rolls) {
                            rollString += i + ", ";
                        }
                        Logger.Log("MadelineParty", "Received die roll from player " + dieRoll.DisplayName + ". Rolls: " + rollString);

                        Instance.RollDice(GameData.Instance.playerSelectTriggers[dieRoll.ID], dieRoll.rolls);
                    }
                }
            }
        }

        private static void HandlePlayerChoice(MPData data) {
            if (data is not PlayerChoice playerChoice) return;
            // If another player in our party has made a choice
            if (GameData.Instance.celestenetIDs.Contains(playerChoice.ID) && playerChoice.ID != MultiplayerSingleton.Instance.CurrentPlayerID()) {
                Logger.Log("MadelineParty", "Choice detected of type " + playerChoice.choiceType + " with value " + playerChoice.choice);
                switch (playerChoice.choiceType) {
                    case "HEART":
                        if (playerChoice.choice == 0) {
                            Instance?.BuyHeart();
                        } else {
                            Instance?.SkipHeart();
                        }
                        break;
                    case "ENTERSHOP":
                        if (playerChoice.choice == 0) {
                            Instance?.EnterShop();
                        } else {
                            Instance?.SkipShop();
                        }
                        break;
                    case "SHOPITEM":
                        if (playerChoice.choice == 0) {
                            Instance?.BuyItem();
                        } else {
                            Instance?.SkipItem();
                        }
                        break;
                    case "DIRECTION":
                        Instance?.ContinueMovementAfterIntersection((Direction)playerChoice.choice);
                        break;
                    case "HEARTSPACEID":
                        GameData.Instance.heartSpaceID = playerChoice.choice;
                        break;
                }
            }
        }

        private static void HandleMinigameStart(MPData data) {
            if (data is not MinigameStart start) return;
            // If we've received information about a minigame starting from another player in our party
            if (GameData.Instance.celestenetIDs.Contains(start.ID) && start.ID != MultiplayerSingleton.Instance.CurrentPlayerID()) {
                ChoseMinigame(start.choice, start.gameStart);
                ModeManager.Instance.AfterMinigameChosen();
            }
        }

        private static void HandleUseItemMenu(MPData data) {
            if (data is not UseItemMenu menu) return;
            Console.WriteLine("Use item menu: " + menu.player + " " + menu.index);
            if (GameData.Instance.celestenetIDs.Contains(menu.ID) && menu.ID != MultiplayerSingleton.Instance.CurrentPlayerID()) {
                Instance.UseItem(menu.player, menu.index);
            }
        }

        private static void HandleUseItem(MPData data) {
            if (data is not UseItem use) return;
            Console.WriteLine("Use item: " + use.player + " " + use.itemIdx);
            if (GameData.Instance.celestenetIDs.Contains(use.ID) && use.ID != MultiplayerSingleton.Instance.CurrentPlayerID()) {
                Instance.leftButtons[use.player].SetCurrentMode(LeftButton.Modes.Inactive);
                Instance.rightButtons[use.player].SetCurrentMode(RightButton.Modes.Inactive);
                GameData.Instance.players[use.player].Items[use.itemIdx].UseItem(use.player);
                GameData.Instance.players[use.player].Items.RemoveAt(use.itemIdx);
            }
        }
    }
}