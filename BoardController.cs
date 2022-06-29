using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        }

        public enum Direction {
            Up,
            Down,
            Left,
            Right
        }

        public struct BoardSpace {
            public int ID;
            public int x, y;
            private List<BoardSpace> _destinations;
            public List<BoardSpace> destinations {
                get {
                    if (_destinations == null) {
                        _destinations = new List<BoardSpace>();
                        if (destIDs_DONTUSE != null) {
                            foreach (int id in destIDs_DONTUSE) {
                                _destinations.Add(boardSpaces.Find(m => m.ID == id));
                            }
                        }
                    }
                    return _destinations;
                }
                set {
                    _destinations = value;
                }
            }
            public List<int> destIDs_DONTUSE;
            public char type;
            public bool heartSpace;
            public string greenSpaceEvent;
            public Vector2 position { get { return new Vector2(x, y); } }
            public Vector2 screenPosition => ((Instance?.Position ?? Vector2.Zero) - ((Engine.Scene as Level)?.LevelOffset ?? Vector2.Zero) + new Vector2(x, y)) * 6;

            public override string ToString() {
                string res = $"boardSpaces.Add(new BoardSpace() {{ ID = {ID}, type = '{type}', x = {x}, y = {y}, heartSpace = {heartSpace.ToString().ToLower()}, greenSpaceEvent = \"{greenSpaceEvent}\", destIDs_DONTUSE = new List<int>{{";
                foreach (BoardSpace dest in destinations) {
                    res += dest.ID + ", ";
                }
                res += "}} );";
                return res;
            }
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

        private static Color pathColor = Color.White; // Use DarkSlateGray in dark mode
        private static Color pathOutlineColor = Color.DarkGray; // Use Black in dark mode

        public const float TOKEN_SPEED = 80f;
        public static string[] TokenPaths = { "madeline/normal00", "badeline/normal00", "theo/excited00", "granny/normal00" };

        private List<BoardSpace> playerMovePath = null;

        // The distance the player will be moving
        private int playerMoveDistance = 0;
        private int playerMoveProgress = 0;
        private int movingPlayerID = 0;

        // The index of the item in the shop being viewed
        private int shopItemViewing = 0;

        private Level level;

        private Random rand = new Random();

        private Random _textRand;

        private Random TextRand {
            get => _textRand == null ? _textRand = new Random((int)(GameData.Instance.turnOrderSeed / 3) - GameData.Instance.turn - 120) : _textRand;
        }

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

        public static Dictionary<char, MTexture> spaceTextures;
        public MTexture heartTexture = GFX.Game["decals/madelineparty/heartstill"];

        public BoardController(EntityData data) : base(data.Position) {
            Instance = this;
            //boardDecals = new Dictionary<Vector2, Decal>();
            diceNumbers = GFX.Game.GetAtlasSubtextures("decals/madelineparty/dicenumbers/dice_");
            AddTag(Tags.PauseUpdate);
            AddTag(Tags.FrozenUpdate);
        }

        // b = blue space
        // r = red space
        // s = start
        // i = item shop

        public static List<BoardSpace> boardSpaces = new();

        private static Dictionary<string, GreenSpaceEvent> greenSpaces;

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
            //boardSpaces.Add(new BoardSpace() { ID = 0, type = 's', x = 54, y = 91, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 1, } });
            //boardSpaces.Add(new BoardSpace() { ID = 1, type = 'b', x = 54, y = 107, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 2, } });
            //boardSpaces.Add(new BoardSpace() { ID = 2, type = 'b', x = 33, y = 101, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 16, 3, } });
            //boardSpaces.Add(new BoardSpace() { ID = 3, type = 'r', x = 16, y = 94, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 4, } });
            //boardSpaces.Add(new BoardSpace() { ID = 4, type = 'g', x = 3, y = 82, heartSpace = true, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 5, } });
            //boardSpaces.Add(new BoardSpace() { ID = 5, type = 'b', x = -5, y = 61, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 6, } });
            //boardSpaces.Add(new BoardSpace() { ID = 6, type = 'i', x = -10, y = 43, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 7, } });
            //boardSpaces.Add(new BoardSpace() { ID = 7, type = 'g', x = 3, y = 29, heartSpace = true, greenSpaceEvent = "tentacleDrag", destIDs_DONTUSE = new List<int> { 8, } });
            //boardSpaces.Add(new BoardSpace() { ID = 8, type = 'b', x = 11, y = 19, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 9, 10, } });
            //boardSpaces.Add(new BoardSpace() { ID = 9, type = 'b', x = 11, y = 1, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 20, } });
            //boardSpaces.Add(new BoardSpace() { ID = 10, type = 'b', x = 32, y = 16, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 11, } });
            //boardSpaces.Add(new BoardSpace() { ID = 11, type = 'b', x = 52, y = 14, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 12, } });
            //boardSpaces.Add(new BoardSpace() { ID = 12, type = 'r', x = 70, y = 22, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 13, } });
            //boardSpaces.Add(new BoardSpace() { ID = 13, type = 'g', x = 70, y = 38, heartSpace = true, greenSpaceEvent = "gondola", destIDs_DONTUSE = new List<int> { 14, } });
            //boardSpaces.Add(new BoardSpace() { ID = 14, type = 'r', x = 68, y = 54, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 19, } });
            //boardSpaces.Add(new BoardSpace() { ID = 15, type = 'r', x = 23, y = 41, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 8, } });
            //boardSpaces.Add(new BoardSpace() { ID = 16, type = 'b', x = 33, y = 83, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 17, } });
            //boardSpaces.Add(new BoardSpace() { ID = 17, type = 'r', x = 30, y = 69, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 18, } });
            //boardSpaces.Add(new BoardSpace() { ID = 18, type = 'g', x = 36, y = 57, heartSpace = false, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 15, } });
            //boardSpaces.Add(new BoardSpace() { ID = 19, type = 'b', x = 50, y = 60, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 18, } });
            //boardSpaces.Add(new BoardSpace() { ID = 20, type = 'g', x = 27, y = -3, heartSpace = true, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 21, } });
            //boardSpaces.Add(new BoardSpace() { ID = 21, type = 'g', x = 44, y = -8, heartSpace = true, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 22, } });
            //boardSpaces.Add(new BoardSpace() { ID = 22, type = 'g', x = 61, y = -13, heartSpace = true, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 23, } });
            //boardSpaces.Add(new BoardSpace() { ID = 23, type = 'b', x = 86, y = -8, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 24, } });
            //boardSpaces.Add(new BoardSpace() { ID = 24, type = 'b', x = 110, y = 7, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 25, } });
            //boardSpaces.Add(new BoardSpace() { ID = 25, type = 'b', x = 117, y = 27, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 28, } });
            //boardSpaces.Add(new BoardSpace() { ID = 26, type = 'b', x = 116, y = 71, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 29, } });
            //boardSpaces.Add(new BoardSpace() { ID = 27, type = 'g', x = 95, y = 54, heartSpace = true, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 14, } });
            //boardSpaces.Add(new BoardSpace() { ID = 28, type = 'b', x = 119, y = 47, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 26, 27, } });
            //boardSpaces.Add(new BoardSpace() { ID = 29, type = 'b', x = 105, y = 87, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 30, } });
            //boardSpaces.Add(new BoardSpace() { ID = 30, type = 'r', x = 80, y = 86, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 31, } });
            //boardSpaces.Add(new BoardSpace() { ID = 31, type = 'b', x = 71, y = 103, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 1, } });
            // Cycle Madness
            boardSpaces.Add(new BoardSpace() { ID = 0, type = 's', x = 52, y = 61, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 2, } });
            boardSpaces.Add(new BoardSpace() { ID = 1, type = 's', x = 52, y = 61, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 2, } });
            boardSpaces.Add(new BoardSpace() { ID = 2, type = 'b', x = 47, y = 80, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 3, } });
            boardSpaces.Add(new BoardSpace() { ID = 3, type = 'g', x = 33, y = 101, heartSpace = false, greenSpaceEvent = "gondola", destIDs_DONTUSE = new List<int> { 4, } });
            boardSpaces.Add(new BoardSpace() { ID = 4, type = 'b', x = 7, y = 96, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 5, } });
            boardSpaces.Add(new BoardSpace() { ID = 5, type = 'b', x = -5, y = 76, heartSpace = true, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 6, } });
            boardSpaces.Add(new BoardSpace() { ID = 6, type = 'r', x = -7, y = 51, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 7, } });
            boardSpaces.Add(new BoardSpace() { ID = 7, type = 'g', x = 8, y = 39, heartSpace = false, greenSpaceEvent = "gondola", destIDs_DONTUSE = new List<int> { 8, } });
            boardSpaces.Add(new BoardSpace() { ID = 8, type = 'b', x = 33, y = 45, heartSpace = true, greenSpaceEvent = "tentacleDrag", destIDs_DONTUSE = new List<int> { 26, } });
            boardSpaces.Add(new BoardSpace() { ID = 9, type = 'g', x = 15, y = 16, heartSpace = true, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 10, } });
            boardSpaces.Add(new BoardSpace() { ID = 10, type = 'b', x = 11, y = 1, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 12, } });
            boardSpaces.Add(new BoardSpace() { ID = 11, type = 'r', x = 33, y = 23, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 9, } });
            boardSpaces.Add(new BoardSpace() { ID = 12, type = 'g', x = 27, y = -9, heartSpace = false, greenSpaceEvent = "tentacleDrag", destIDs_DONTUSE = new List<int> { 13, } });
            boardSpaces.Add(new BoardSpace() { ID = 13, type = 'b', x = 55, y = -15, heartSpace = true, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 14, } });
            boardSpaces.Add(new BoardSpace() { ID = 14, type = 'b', x = 81, y = -15, heartSpace = true, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 15, } });
            boardSpaces.Add(new BoardSpace() { ID = 15, type = 'i', x = 107, y = -10, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 16, } });
            boardSpaces.Add(new BoardSpace() { ID = 16, type = 'g', x = 104, y = 12, heartSpace = false, greenSpaceEvent = "tentacleDrag", destIDs_DONTUSE = new List<int> { 23, } });
            boardSpaces.Add(new BoardSpace() { ID = 17, type = 'g', x = 70, y = 33, heartSpace = false, greenSpaceEvent = "gondola", destIDs_DONTUSE = new List<int> { 25, } });
            boardSpaces.Add(new BoardSpace() { ID = 18, type = 'r', x = 115, y = 71, heartSpace = true, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 20, } });
            boardSpaces.Add(new BoardSpace() { ID = 19, type = 'b', x = 119, y = 52, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 18, } });
            boardSpaces.Add(new BoardSpace() { ID = 20, type = 'b', x = 98, y = 86, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 21, } });
            boardSpaces.Add(new BoardSpace() { ID = 21, type = 'r', x = 74, y = 87, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 22, } });
            boardSpaces.Add(new BoardSpace() { ID = 22, type = 'b', x = 63, y = 72, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 24, } });
            boardSpaces.Add(new BoardSpace() { ID = 23, type = 'b', x = 84, y = 18, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 27, } });
            boardSpaces.Add(new BoardSpace() { ID = 24, type = 'g', x = 52, y = 40, heartSpace = false, greenSpaceEvent = "tentacleDrag", destIDs_DONTUSE = new List<int> { 17, } });
            boardSpaces.Add(new BoardSpace() { ID = 25, type = 'b', x = 104, y = 33, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 19, } });
            boardSpaces.Add(new BoardSpace() { ID = 26, type = 'b', x = 38, y = 63, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 2, } });
            boardSpaces.Add(new BoardSpace() { ID = 27, type = 'b', x = 55, y = 22, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 11, } });
        }

        public static void Load() {
            MultiplayerSingleton.Instance.RegisterHandler<DieRoll>(HandleDieRoll);
            MultiplayerSingleton.Instance.RegisterHandler<PlayerChoice>(HandlePlayerChoice);
            MultiplayerSingleton.Instance.RegisterHandler<MinigameStart>(HandleMinigameStart);
        }

        public static void LoadContent() {
            spaceTextures = new() {
                ['r'] = GFX.Game["decals/madelineparty/redspace"],
                ['b'] = GFX.Game["decals/madelineparty/bluespace"],
                ['g'] = GFX.Game["decals/madelineparty/greenspace"],
                ['i'] = GFX.Game["decals/madelineparty/shopspace"]
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

            foreach (BoardSpace space in boardSpaces) {
                if (space.type == 's') {
                    int tokensAdded = 0;
                    for (int k = 0; k < GameData.Instance.players.Length; k++) {
                        if (GameData.Instance.players[k] != null) {
                            if (!GameData.Instance.gameStarted) {
                                PlayerToken token = new PlayerToken(k, TokenPaths[GameData.Instance.players[k].TokenSelected], space.screenPosition + new Vector2(0, tokensAdded * 18), new Vector2(.25f, .25f), -1, space);
                                playerTokens[k] = token;
                                GameData.Instance.players[k].token = token;
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
            }
        }

        static bool hackfixRespawn;

        public override void Awake(Scene scene) {
            base.Awake(scene);
            if (!hackfixRespawn) { //FIXME hackfix
                hackfixRespawn = true;
                scene.Tracker.GetEntity<Player>().Die(Vector2.Zero);
                return;
            }
            List<LeftButton> found = scene.Entities.FindAll<LeftButton>();
            found.Sort();
            for (int i = 0; i < found.Count; i++) {
                leftButtons.Add(found[i]);
                if (GameData.Instance.players[i] != null && !GameData.Instance.gameStarted) {
                    int p = i;
                    Alarm.Set(this, 0.5f, delegate {
                        SetDice(p);
                        if(level.Wipe != null) {
                            Action onComplete = level.Wipe.OnComplete;
                            level.Wipe.OnComplete = delegate {
                                Scene.Add(new MiniTextbox("MadelineParty_Start"));
                                onComplete?.Invoke();
                            };
                        } else {
                            Scene.Add(new MiniTextbox("MadelineParty_Start"));
                        }
                    });
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
                        Logger.Log("MadelineParty", "Delayed emote interpreted as die roll from player " + delayedDieRoll.DisplayName + ". Rolls: " + rollString);

                        if (delayedDieRoll.rolls.Length == 2)
                            GameData.Instance.players[GameData.Instance.playerSelectTriggers[delayedDieRoll.ID]].items.Remove(GameData.Item.DOUBLEDICE);
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
            }
            GameData.Instance.gameStarted = true;
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

        private void SetDice(int player) {
            SetLeftButtonStatus(player, LeftButton.Modes.Dice);
        }

        private void SetDoubleDice(int player) {
            SetRightButtonStatus(player, RightButton.Modes.DoubleDice);
        }

        private void ChangeTurn(int player) {
            SetDice(player);
            if (GameData.Instance.players[player].items.Contains(GameData.Item.DOUBLEDICE)) {
                SetDoubleDice(player);
            }
            if(level.Wipe != null) {
                Action onComplete = level.Wipe.OnComplete;
                level.Wipe.OnComplete = delegate {
                    level.Add(new MiniTextbox(GetCurrentTurnText(player)));
                    onComplete?.Invoke();
                };
            } else {
                level.Add(new MiniTextbox(GetCurrentTurnText(player)));
            }
        }

        private string GetCurrentTurnText(int player) {
            // First, set the name to use as a dialog entry
            string name;
            if(MultiplayerSingleton.Instance.BackendConnected()) {
                name = MultiplayerSingleton.Instance.GetPlayerName(GameData.Instance.celestenetIDs[player]);
            } else {
                name = "{savedata Name}";
            }
            Dialog.Language.Dialog["MadelineParty_Current_Turn_Name"] = name;
            return GetRandomDialogID("MadelineParty_Current_Turn_Text_List");
        }

        public static string GetRandomDialogID(string listID) {
            return Instance.TextRand.Choose(Dialog.Clean(listID).Split(','));
        }

        private Vector2 ScreenCoordsFromBoardCoords(Vector2 boardCoords) {
            return ScreenCoordsFromBoardCoords(boardCoords, new Vector2(0, 0));
        }

        private Vector2 ScreenCoordsFromBoardCoords(Vector2 boardCoords, Vector2 offsetInPxls) {
            return new Vector2((X - level.LevelOffset.X + boardCoords.X * 10) * 6 + offsetInPxls.X, (Y - level.LevelOffset.Y + boardCoords.Y * 10) * 6 + offsetInPxls.Y);
        }

        private Vector2 SwapXY(Vector2 v) {
            return new Vector2(v.Y, v.X);
        }

        public override void Update() {
            base.Update();
            switch (status) {
                case BoardStatus.PLAYERMOVE:
                    // If we've reached the end of the path but not the end of our movement
                    // We must be at an intersection
                    if (playerMoveProgress == playerMovePath.Count - 1) {
                        playerMoveDistance -= playerMoveProgress;
                        playerMoveProgress = 0;
                        playerMovePath = new List<BoardSpace> { CurrentPlayerToken.currentSpace };
                        status = BoardStatus.WAITING;

                        if (playerMovePath[playerMoveProgress].destinations.Count > 2) {
                            throw new NotSupportedException("Intersections with more than two places to go are not supported");
                        }
                        bool leftUsed = false;
                        foreach (BoardSpace dest in playerMovePath[playerMoveProgress].destinations) {
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

                        // If we're on the heart space
                        if (GameData.Instance.heartSpaceID == CurrentPlayerToken.currentSpace.ID && GameData.Instance.players[movingPlayerID].strawberries >= GameData.Instance.heartCost) {
                            status = BoardStatus.WAITING;
                            SetLeftButtonStatus(CurrentPlayerToken, LeftButton.Modes.ConfirmHeartBuy);
                            SetRightButtonStatus(CurrentPlayerToken, RightButton.Modes.CancelHeartBuy);
                            scoreboards[turnOrder[playerTurn]].SetCurrentMode(GameScoreboard.Modes.BUYHEART);
                            Dialog.Language.Dialog["MadelineParty_Heart_Cost"] = GameData.Instance.heartCost.ToString();
                            level.Add(new MiniTextbox(GetRandomDialogID("MadelineParty_Buy_Heart_Prompt_List")));
                        }
                        // If we're at the item shop and have enough free space
                        else if (CurrentPlayerToken.currentSpace.type == 'i' && GameData.Instance.players[movingPlayerID].items.Count < GameData.maxItems) {
                            status = BoardStatus.WAITING;
                            SetLeftButtonStatus(CurrentPlayerToken, LeftButton.Modes.ConfirmShopEnter);
                            SetRightButtonStatus(CurrentPlayerToken, RightButton.Modes.CancelShopEnter);
                            scoreboards[turnOrder[playerTurn]].SetCurrentMode(GameScoreboard.Modes.ENTERSHOP);
                            level.Add(new MiniTextbox(GetRandomDialogID("MadelineParty_Enter_Shop_Prompt_List")));
                        } else if (playerMoveProgress == playerMoveDistance) { // Check if we've hit our destination
                            playerMoveDistance = 0;
                            playerMovePath = null;
                            playerMoveProgress = 0;
                            status = BoardStatus.WAITING;
                            HandleSpaceAction(EndTurn);
                        }
                        break;
                    }
                    CurrentPlayerToken.Position = Calc.Approach(CurrentPlayerToken.Position, approaching.screenPosition, TOKEN_SPEED * Engine.DeltaTime);
                    break;
            }
        }

        private void HandleSpaceAction(Action next) {
            // Don't do an action if we're on the heart space
            if(GameData.Instance.heartSpaceID == CurrentPlayerToken.currentSpace.ID) {
                next();
            }
            switch (CurrentPlayerToken.currentSpace.type) {
                case 'b':
                    ChangeStrawberries(movingPlayerID, 3);
                    next();
                    break;
                case 'r':
                    ChangeStrawberries(movingPlayerID, -3);
                    next();
                    break;
                case 'g':
                    DoGreenSpace(CurrentPlayerToken.currentSpace, next);
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
                scoreboards[turnOrder[playerTurn]].SetCurrentMode(GameScoreboard.Modes.BUYITEM, GameData.Instance.shopContents[shopItemViewing]);
                if (GameData.Instance.players[turnOrder[playerTurn]].strawberries >= GameData.Instance.itemPrices[GameData.Instance.shopContents[shopItemViewing]]) {
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
            // Only send out data if we are the player that bought the item
            if (turnOrder[playerTurn] == GameData.Instance.realPlayerID) {
                MultiplayerSingleton.Instance.Send(new PlayerChoice { choiceType = "SHOPITEM", choice = 0 });
            }
            GameData.Item itemBought = GameData.Instance.shopContents[shopItemViewing];
            GameData.Instance.players[turnOrder[playerTurn]].items.Add(itemBought);
            shopItemViewing = 0;
            scoreboards[turnOrder[playerTurn]].SetCurrentMode(GameScoreboard.Modes.NORMAL, GameData.Instance.shopContents[shopItemViewing]);
            SetLeftButtonStatus(CurrentPlayerToken, LeftButton.Modes.Inactive);
            SetRightButtonStatus(CurrentPlayerToken, RightButton.Modes.Inactive);

            ChangeStrawberries(turnOrder[playerTurn], -GameData.Instance.itemPrices[itemBought], .08f);
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
            scoreboards[turnOrder[playerTurn]].SetCurrentMode(GameScoreboard.Modes.BUYITEM, GameData.Instance.shopContents[shopItemViewing]);
            SetRightButtonStatus(CurrentPlayerToken, RightButton.Modes.CancelItemBuy);
            Console.WriteLine(GameData.Instance.players[turnOrder[playerTurn]].strawberries + " " + GameData.Instance.itemPrices[GameData.Instance.shopContents[shopItemViewing]]);
            if (GameData.Instance.players[turnOrder[playerTurn]].strawberries >= GameData.Instance.itemPrices[GameData.Instance.shopContents[shopItemViewing]]) {
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
            GameData.Instance.players[turnOrder[playerTurn]].hearts++;
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

            if (playerMoveProgress == playerMoveDistance) {
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
            foreach (BoardSpace dest in playerMovePath[playerMoveProgress].destinations) {
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
                if (playerMovePath[i].destinations.Count == 1) {
                    playerMovePath.Add(playerMovePath[i].destinations[0]);
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
                playerTurn = 0;
                turnDisplay = GameData.Instance.turn;
                GameData.Instance.turn++;
                Add(new Coroutine(InitiateMinigame()));
            } else {
                ChangeTurn(CurrentPlayerToken.id);
            }
        }

        public void ChoseMinigame(int chosen, long startTime = 0) {
            List<LevelData> minigames = level.Session.MapData.Levels.FindAll((obj) => obj.Name.StartsWith("z_Minigame", StringComparison.InvariantCulture));
            minigames.RemoveAll((obj) => GameData.Instance.playedMinigames.Contains(obj.Name));
            GameData.Instance.minigame = minigames[chosen];
            GameData.Instance.playedMinigames.Add(GameData.Instance.minigame.Name);
            minigameStartTime = DateTime.FromFileTimeUtc(startTime);
        }

        public IEnumerator InitiateMinigame() {
            yield return 1f;
            level.Add(new PersistentMiniTextbox(GetRandomDialogID("MadelineParty_Minigame_Time_List")));
            hackfixRespawn = false; //FIXME hackfix
            if (GameData.Instance.gnetHost) {
                List<LevelData> minigames = level.Session.MapData.Levels.FindAll((obj) => obj.Name.StartsWith("z_Minigame", StringComparison.InvariantCulture));
                minigames.RemoveAll((obj) => GameData.Instance.playedMinigames.Contains(obj.Name));
                int chosenMinigame = rand.Next(minigames.Count);
                if (riggedMinigame != null && minigames.IndexOf(riggedMinigame) >= 0) {
                    chosenMinigame = minigames.IndexOf(riggedMinigame);
                    riggedMinigame = null;
                }
                minigameStartTime = DateTime.UtcNow.AddSeconds(3);
                Console.WriteLine("Minigame chosen: " + chosenMinigame);
                ChoseMinigame(chosenMinigame);
                MultiplayerSingleton.Instance.Send(new MinigameStart { choice = chosenMinigame, gameStart = minigameStartTime.ToFileTimeUtc() });
            }
            Console.WriteLine("Host? " + GameData.Instance.gnetHost);

            Console.WriteLine("Begin minigame wait");
            while (GameData.Instance.minigame == null /*|| minigameStartTime.CompareTo(DateTime.UtcNow) < 0*/) {
                yield return null;
            }

            yield return 5f;
            Console.WriteLine("End minigame wait");

            Player player = level.Tracker.GetEntity<Player>();
            level.OnEndOfFrame += delegate {
                player.Speed = Vector2.Zero;
                Leader.StoreStrawberries(player.Leader);
                level.Remove(player);
                level.UnloadLevel();

                level.Session.Level = GameData.Instance.minigame.Name;
                level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(level.Bounds.Left, level.Bounds.Top));
                level.LoadLevel(Player.IntroTypes.None);
                level.Session.Audio.Music.Event = GameData.GetMinigameMusic(GameData.Instance.minigame.Name);

                Leader.RestoreStrawberries(player.Leader);
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

        private IEnumerator DieRollAnimation(int playerID, int[] rolls) {
            SetLeftButtonStatus(playerID, LeftButton.Modes.Inactive);
            SetRightButtonStatus(playerID, RightButton.Modes.Inactive);
            while (removingDieRolls) yield return null;
            foreach (int roll in rolls) {
                if (roll == 0) continue;
                DieNumber number = new DieNumber(this, roll - 1, status == BoardStatus.GAMESTART ? getRelativePlayerID(playerID) : numbersToDisplay.Count);
                level.Add(number);
                numbersToDisplay.Add(number);
                // Number of expected die rolls
                int numberOfSpaces = status == BoardStatus.GAMESTART ? GameData.Instance.playerNumber : rolls.Length;
                // new Vector2(8, 4) for text instead of graphics
                number.MoveNumber((rolls.Length == 1 ? leftButtons[playerID].Position : rightButtons[playerID].Position) + new Vector2(0, 12) + new Vector2(8, 4), Position + new Vector2(12 * 5 - 10 * (numberOfSpaces - 1) - 4 + 20 * number.posIndex, -24) + new Vector2(8, 4));
                yield return .25f;
            }
            yield return 3.25f;
            int rollSum = 0;
            foreach (int i in rolls) rollSum += i;
            RollDice(playerID, rollSum);
        }

        private IEnumerator RemoveDieRollsAnimation() {
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
            RollDice(playerID, false);
        }

        // Usually called only due to Celestenet messages
        public void RollDice(int playerID, int[] rolls) {
            if (playerID == GameData.Instance.realPlayerID) {
                MultiplayerSingleton.Instance.Send(new DieRoll { rolls = rolls });
            }
            Add(new Coroutine(DieRollAnimation(playerID, rolls)));
        }

        public void RollDice(int playerID, bool doubleDice) {
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
                if (doubleDice) {
                    rolls.Add(rand.Next(10) + 1);
                    GameData.Instance.players[playerID].items.Remove(GameData.Item.DOUBLEDICE);
                }
            }
            if (playerID == GameData.Instance.realPlayerID) {
                MultiplayerSingleton.Instance.Send(new DieRoll { rolls = rolls.ToArray() });
            }
            Add(new Coroutine(DieRollAnimation(playerID, rolls.ToArray())));
        }

        public void RollDice(int playerID, int roll) {
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
                    }
                    status = BoardStatus.WAITING;
                    ChangeTurn(turnOrder[0]);
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
                if (playerMovePath[i].destinations.Count == 1) {
                    playerMovePath.Add(playerMovePath[i].destinations[0]);
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
                if (space.ID != GameData.Instance.heartSpaceID && space.type == 'g' && greenSpaces.TryGetValue(space.greenSpaceEvent, out GreenSpaceEvent spaceEvent)) {
                    spaceEvent.RenderSubHUD(space);
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
                    foreach (BoardSpace dest in space.destinations) {
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
                    if(space.type == 'g' && greenSpaces.TryGetValue(space.greenSpaceEvent, out GreenSpaceEvent spaceEvent)) {
                        spaceEvent.Render(space);
                    } else if (spaceTextures.ContainsKey(space.type)) {
                        spaceTextures[space.type].DrawCentered(spacePos);
                    }
                } else {
                    heartTexture.DrawCentered(spacePos, Color.White, 0.75f);
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
            if (GameData.Instance.celestenetIDs.Contains(dieRoll.ID) && dieRoll.ID != MultiplayerSingleton.Instance.GetPlayerID()) {

                if (!MadelinePartyModule.Instance.level.Session.Level.Equals(MadelinePartyModule.MAIN_ROOM)) {
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

                        if (dieRoll.rolls.Length == 2)
                            GameData.Instance.players[GameData.Instance.playerSelectTriggers[dieRoll.ID]].items.Remove(GameData.Item.DOUBLEDICE);
                        Instance.RollDice(GameData.Instance.playerSelectTriggers[dieRoll.ID], dieRoll.rolls);
                    }
                }
            }
        }

        private static void HandlePlayerChoice(MPData data) {
            if (data is not PlayerChoice playerChoice) return;
            // If another player in our party has made a choice
            if (GameData.Instance.celestenetIDs.Contains(playerChoice.ID) && playerChoice.ID != MultiplayerSingleton.Instance.GetPlayerID()) {
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
            if (GameData.Instance.celestenetIDs.Contains(start.ID) && start.ID != MultiplayerSingleton.Instance.GetPlayerID()) {
                Instance?.ChoseMinigame(start.choice, start.gameStart);
            }
        }
    }
}