using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Celeste;
using Celeste.Mod;
using Celeste.Mod.CelesteNet.Client;
using MadelineParty.CelesteNet;
using MadelineParty.GreenSpace;
using Microsoft.Xna.Framework;
using Monocle;
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
                        foreach (int id in destIDs_DONTUSE) {
                            _destinations.Add(boardSpaces.Find(m => m.ID == id));
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
            public TurnDisplay() {
                AddTag(TagsExt.SubHUD);
            }

            public override void Render() {
                base.Render();
                string text = "Turn " + (Instance.turnDisplay == -1 ? GameData.turn : Instance.turnDisplay) + "/" + GameData.maxTurns;
                ActiveFont.DrawOutline(text, new Vector2(Celeste.Celeste.TargetWidth / 2, Celeste.Celeste.TargetHeight - 6 * 16), new Vector2(0.5f, 0.5f), Vector2.One, Color.Blue, 2f, Color.Black);
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

        public static string[] TokenPaths = { "madeline/normal00", "badeline/normal00", "theo/excited00", "granny/normal00" };

        private List<BoardSpace> playerMovePath = null;

        // The distance the player will be moving
        private int playerMoveDistance = 0;
        private int playerMoveProgress = 0;
        private int movingPlayerID = 0;

        // The index of the item in the shop being viewed
        private int shopItemViewing = 0;

        private Level level;

        Random rand = new Random();

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

        private List<LeftButton> leftButtons = new List<LeftButton>();
        private List<RightButton> rightButtons = new List<RightButton>();

        private List<GameScoreboard> scoreboards = new List<GameScoreboard>();

        public List<MTexture> diceNumbers;
        private List<DieNumber> numbersToDisplay = new List<DieNumber>();

        public static DieRollData delayedDieRoll;

        private DateTime minigameStartTime;

        public static LevelData riggedMinigame = null;

        public BoardStatus status = BoardStatus.GAMESTART;

        public static BoardController Instance;

        public static Dictionary<char, MTexture> spaceTextures = new Dictionary<char, MTexture> {
            ['r'] = GFX.Game["decals/madelineparty/redspace"],
            ['b'] = GFX.Game["decals/madelineparty/bluespace"],
            ['g'] = GFX.Game["decals/madelineparty/greenspace"],
            ['i'] = GFX.Game["decals/madelineparty/shopspace"]
        };
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

        public static List<BoardSpace> boardSpaces = new List<BoardSpace>();

        private static Dictionary<string, GreenSpaceEvent> greenSpaces;

        static BoardController() {
            //boardSpaces.Add(new BoardSpace() { ID = 0, type = 's', x = 16, y = 52, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 1, } });
            //boardSpaces.Add(new BoardSpace() { ID = 1, type = 'b', x = 33, y = 42, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 2, } });
            //boardSpaces.Add(new BoardSpace() { ID = 2, type = 'b', x = 45, y = 23, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 3, 10, } });
            //boardSpaces.Add(new BoardSpace() { ID = 3, type = 'g', x = 78, y = 22, heartSpace = true, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 4, } });
            //boardSpaces.Add(new BoardSpace() { ID = 4, type = 'g', x = 106, y = 25, heartSpace = true, greenSpaceEvent = "tentacleDrag", destIDs_DONTUSE = new List<int> { 5, } });
            //boardSpaces.Add(new BoardSpace() { ID = 5, type = 'g', x = 117, y = 48, heartSpace = true, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 6, } });
            //boardSpaces.Add(new BoardSpace() { ID = 6, type = 'g', x = 106, y = 67, heartSpace = true, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 7, } });
            //boardSpaces.Add(new BoardSpace() { ID = 7, type = 'g', x = 78, y = 74, heartSpace = true, greenSpaceEvent = "gondola", destIDs_DONTUSE = new List<int> { 8, } });
            //boardSpaces.Add(new BoardSpace() { ID = 8, type = 'g', x = 51, y = 76, heartSpace = true, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 9, } });
            //boardSpaces.Add(new BoardSpace() { ID = 9, type = 'b', x = 37, y = 61, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 1, } });
            //boardSpaces.Add(new BoardSpace() { ID = 10, type = 'b', x = 26, y = -4, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 11, } });
            //boardSpaces.Add(new BoardSpace() { ID = 11, type = 'i', x = 63, y = -5, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 4, } });
            // Wiggler's Garden
            boardSpaces.Add(new BoardSpace() { ID = 0, type = 's', x = 53, y = 92, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 1, } });
            boardSpaces.Add(new BoardSpace() { ID = 1, type = 'b', x = 52, y = 107, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 2, } });
            boardSpaces.Add(new BoardSpace() { ID = 2, type = 'b', x = 33, y = 101, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 16, 3, } });
            boardSpaces.Add(new BoardSpace() { ID = 3, type = 'r', x = 20, y = 91, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 4, } });
            boardSpaces.Add(new BoardSpace() { ID = 4, type = 'b', x = 3, y = 82, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 5, } });
            boardSpaces.Add(new BoardSpace() { ID = 5, type = 'b', x = -5, y = 61, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 6, } });
            boardSpaces.Add(new BoardSpace() { ID = 6, type = 'i', x = -10, y = 43, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 7, } });
            boardSpaces.Add(new BoardSpace() { ID = 7, type = 'g', x = 3, y = 29, heartSpace = true, greenSpaceEvent = "tentacleDrag", destIDs_DONTUSE = new List<int> { 8, } });
            boardSpaces.Add(new BoardSpace() { ID = 8, type = 'b', x = 11, y = 19, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 9, 10, } });
            boardSpaces.Add(new BoardSpace() { ID = 9, type = 'b', x = 11, y = 1, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 20, } });
            boardSpaces.Add(new BoardSpace() { ID = 10, type = 'b', x = 29, y = 21, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 11, } });
            boardSpaces.Add(new BoardSpace() { ID = 11, type = 'b', x = 52, y = 21, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 12, } });
            boardSpaces.Add(new BoardSpace() { ID = 12, type = 'r', x = 72, y = 22, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 13, } });
            boardSpaces.Add(new BoardSpace() { ID = 13, type = 'g', x = 76, y = 42, heartSpace = true, greenSpaceEvent = "tentacleDrag", destIDs_DONTUSE = new List<int> { 14, } });
            boardSpaces.Add(new BoardSpace() { ID = 14, type = 'r', x = 62, y = 54, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 19, } });
            boardSpaces.Add(new BoardSpace() { ID = 15, type = 'r', x = 23, y = 41, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 8, } });
            boardSpaces.Add(new BoardSpace() { ID = 16, type = 'b', x = 33, y = 83, heartSpace = false, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 17, } });
            boardSpaces.Add(new BoardSpace() { ID = 17, type = 'r', x = 30, y = 69, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 18, } });
            boardSpaces.Add(new BoardSpace() { ID = 18, type = 'g', x = 36, y = 57, heartSpace = false, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 15, } });
            boardSpaces.Add(new BoardSpace() { ID = 19, type = 'b', x = 50, y = 60, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 18, } });
            boardSpaces.Add(new BoardSpace() { ID = 20, type = 'g', x = 27, y = -3, heartSpace = true, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 21, } });
            boardSpaces.Add(new BoardSpace() { ID = 21, type = 'g', x = 44, y = -6, heartSpace = true, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 22, } });
            boardSpaces.Add(new BoardSpace() { ID = 22, type = 'g', x = 63, y = -8, heartSpace = true, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 23, } });
            boardSpaces.Add(new BoardSpace() { ID = 23, type = 'b', x = 93, y = -1, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 24, } });
            boardSpaces.Add(new BoardSpace() { ID = 24, type = 'b', x = 108, y = 22, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 25, } });
            boardSpaces.Add(new BoardSpace() { ID = 25, type = 'b', x = 107, y = 38, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 28, } });
            boardSpaces.Add(new BoardSpace() { ID = 26, type = 'b', x = 105, y = 68, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 29, } });
            boardSpaces.Add(new BoardSpace() { ID = 27, type = 'g', x = 86, y = 50, heartSpace = true, greenSpaceEvent = "seeker", destIDs_DONTUSE = new List<int> { 14, } });
            boardSpaces.Add(new BoardSpace() { ID = 28, type = 'b', x = 105, y = 50, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 26, 27, } });
            boardSpaces.Add(new BoardSpace() { ID = 29, type = 'b', x = 96, y = 84, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 30, } });
            boardSpaces.Add(new BoardSpace() { ID = 30, type = 'r', x = 76, y = 86, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 31, } });
            boardSpaces.Add(new BoardSpace() { ID = 31, type = 'b', x = 68, y = 101, heartSpace = true, greenSpaceEvent = "", destIDs_DONTUSE = new List<int> { 1, } });
        }

        public static void LoadContent() {
            var spaces = from assembly in AppDomain.CurrentDomain.GetAssemblies()
                         from type in assembly.GetTypesSafe()
                         where type.IsDefined(typeof(GreenSpaceAttribute), false) && typeof(GreenSpaceEvent).IsAssignableFrom(type)
                         select type.GetConstructor(new Type[0]).Invoke(new object[0]) as GreenSpaceEvent;
            greenSpaces = spaces.ToDictionary(space => space.GetType().GetCustomAttributes<GreenSpaceAttribute>().First().id);
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
                    for (int k = 0; k < GameData.players.Length; k++) {
                        if (GameData.players[k] != null) {
                            if (!GameData.gameStarted) {
                                PlayerToken token = new PlayerToken(k, TokenPaths[GameData.players[k].TokenSelected], space.screenPosition + new Vector2(0, tokensAdded * 18), new Vector2(.25f, .25f), -1, space);
                                playerTokens[k] = token;
                                GameData.players[k].token = token;
                            } else {
                                playerTokens[k] = GameData.players[k].token;
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
                if (GameData.players[i] != null && !GameData.gameStarted) {
                    int p = i;
                    Alarm.Set(this, 0.5f, delegate {
                        SetDice(p);
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
            if (GameData.gameStarted) {
                List<PlayerData> temp = new List<PlayerData>(GameData.players);
                temp.Sort();
                int playersGoneThrough = 0;
                for (int i = 0; i < temp.Count; i++) {
                    if (temp[i] == null) continue;
                    turnOrder[playersGoneThrough] = temp[i].TokenSelected;
                    playersGoneThrough++;
                }
                status = BoardStatus.WAITING;
                Alarm.Set(this, 0.5f, delegate {
                    SetDice(turnOrder[0]);
                });
                if (GameData.players[turnOrder[0]].items.Contains(GameData.Item.DOUBLEDICE)) {
                    Alarm.Set(this, 0.5f, delegate {
                        SetDoubleDice(turnOrder[0]);
                    });
                }

                if(MadelinePartyModule.CelesteNetConnected()) {
                    CelesteNetHandleDelayedRoll();
                }
            }
            GameData.gameStarted = true;
        }

        private void CelesteNetHandleDelayedRoll() {
            if (delayedDieRoll != null) {
                if (isWaitingOnPlayer(GameData.playerSelectTriggers[delayedDieRoll.Player.ID])) {
                    string rollString = "";
                    foreach (int i in delayedDieRoll.rolls) {
                        rollString += i + ", ";
                    }
                    Logger.Log("MadelineParty", "Delayed emote interpreted as die roll from player " + delayedDieRoll.Player.ID + ". Rolls: " + rollString);

                    if (delayedDieRoll.rolls.Length == 2)
                        GameData.players[GameData.playerSelectTriggers[delayedDieRoll.Player.ID]].items.Remove(GameData.Item.DOUBLEDICE);
                    RollDice(GameData.playerSelectTriggers[delayedDieRoll.Player.ID], delayedDieRoll.rolls);
                }
                delayedDieRoll = null;
            }
        }

        private void SetDice(int player) {
            leftButtons[player].SetCurrentMode(LeftButton.Modes.Dice);
        }

        private void SetDoubleDice(int player) {
            rightButtons[player].SetCurrentMode(RightButton.Modes.DoubleDice);
        }

        private Vector2 ScreenCoordsFromBoardCoords(Vector2 boardCoords) {
            return ScreenCoordsFromBoardCoords(boardCoords, new Vector2(0, 0));
        }

        private Vector2 ScreenCoordsFromBoardCoords(Vector2 boardCoords, Vector2 offsetInPxls) {
            return new Vector2((this.X - level.LevelOffset.X + boardCoords.X * 10) * 6 + offsetInPxls.X, (this.Y - level.LevelOffset.Y + boardCoords.Y * 10) * 6 + offsetInPxls.Y);
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
                        playerMovePath = new List<BoardSpace> { playerTokens[movingPlayerID].currentSpace };
                        status = BoardStatus.WAITING;

                        if (playerMovePath[playerMoveProgress].destinations.Count > 2) {
                            throw new NotSupportedException("Intersections with more than two places to go are not supported");
                        }
                        bool leftUsed = false;
                        foreach (BoardSpace dest in playerMovePath[playerMoveProgress].destinations) {
                            Direction dir = getCardinalDirection(playerMovePath[playerMoveProgress].x, playerMovePath[playerMoveProgress].y, dest.x, dest.y);

                            if (leftUsed) {
                                rightButtons[turnOrder[playerTurn]].SetCurrentMode((RightButton.Modes)Enum.Parse(typeof(RightButton.Modes), dir.ToString()));
                            } else {
                                leftButtons[turnOrder[playerTurn]].SetCurrentMode((LeftButton.Modes)Enum.Parse(typeof(LeftButton.Modes), dir.ToString()));
                                leftUsed = true;
                            }
                        }
                        break;
                    }
                    // If we're not at an intersection
                    BoardSpace approaching = playerMovePath[playerMoveProgress + 1];
                    // Check if we've hit our next space
                    if (playerTokens[movingPlayerID].Position.Equals(approaching.screenPosition)) {
                        playerMoveProgress++;
                        playerTokens[movingPlayerID].currentSpace = playerMovePath[playerMoveProgress];

                        // If we're on the heart space
                        if (GameData.heartSpaceID == playerTokens[movingPlayerID].currentSpace.ID && GameData.players[movingPlayerID].strawberries >= GameData.heartCost) {
                            status = BoardStatus.WAITING;
                            leftButtons[turnOrder[playerTurn]].SetCurrentMode(LeftButton.Modes.ConfirmHeartBuy);
                            rightButtons[turnOrder[playerTurn]].SetCurrentMode(RightButton.Modes.CancelHeartBuy);
                            scoreboards[turnOrder[playerTurn]].SetCurrentMode(GameScoreboard.Modes.BUYHEART);
                        }
                        // If we're at the item shop and have enough free space
                        else if (playerTokens[movingPlayerID].currentSpace.type == 'i' && GameData.players[movingPlayerID].items.Count < GameData.maxItems) {
                            status = BoardStatus.WAITING;
                            leftButtons[turnOrder[playerTurn]].SetCurrentMode(LeftButton.Modes.ConfirmShopEnter);
                            rightButtons[turnOrder[playerTurn]].SetCurrentMode(RightButton.Modes.CancelShopEnter);
                            scoreboards[turnOrder[playerTurn]].SetCurrentMode(GameScoreboard.Modes.ENTERSHOP);
                        } else if (playerMoveProgress == playerMoveDistance) { // Check if we've hit our destination
                            playerMoveDistance = 0;
                            playerMovePath = null;
                            playerMoveProgress = 0;
                            status = BoardStatus.WAITING;
                            HandleSpaceAction(EndTurn);
                        }
                        break;
                    }
                    playerTokens[movingPlayerID].Position = Calc.Approach(playerTokens[movingPlayerID].Position, approaching.screenPosition, 80f * Engine.DeltaTime);
                    break;
            }
        }

        private void HandleSpaceAction(Action next) {
            // Don't do an action if we're on the heart space
            if(GameData.heartSpaceID == playerTokens[movingPlayerID].currentSpace.ID) {
                next();
            }
            switch (playerTokens[movingPlayerID].currentSpace.type) {
                case 'b':
                    ChangeStrawberries(movingPlayerID, 3);
                    next();
                    break;
                case 'r':
                    ChangeStrawberries(movingPlayerID, -3);
                    next();
                    break;
                case 'g':
                    DoGreenSpace(playerTokens[movingPlayerID].currentSpace, next);
                    break;
                default:
                    next();
                    break;
            }
        }

        public void ChangeStrawberries(int playerID, int amt, float changeSpeed = 0.25f) {
            scoreboards[playerID].StrawberryChange(amt, changeSpeed);
            GameData.players[playerID].ChangeStrawberries(amt);
        }

        private void CelesteNetSendPlayerChoice(int type, int choice) {
            CelesteNetClientModule.Instance.Client?.Send(new PlayerChoiceData {
                Player = CelesteNetClientModule.Instance.Client.PlayerInfo,
                choiceType = type,
                choice = choice
            });
        }

        private void CelesteNetSendDieRolls(int[] rolls) {
            CelesteNetClientModule.Instance.Client?.Send(new DieRollData {
                Player = CelesteNetClientModule.Instance.Client.PlayerInfo,
                rolls = rolls
            });
        }

        private void CelesteNetSendMinigameStart(int choice, long time = 0) {
            CelesteNetClientModule.Instance.Client?.Send(new MinigameStartData {
                Player = CelesteNetClientModule.Instance.Client.PlayerInfo,
                choice = choice,
                gameStart = time
            });
        }

        public void SkipItem() {
            // Only send out data if we are the player that skipped the item
            if (turnOrder[playerTurn] == GameData.realPlayerID && MadelinePartyModule.CelesteNetConnected()) {
                CelesteNetSendPlayerChoice(PlayerChoiceData.SHOPITEM, 1);
            }
            shopItemViewing++;
            if (shopItemViewing < GameData.shopContents.Count) {
                scoreboards[turnOrder[playerTurn]].SetCurrentMode(GameScoreboard.Modes.BUYITEM, GameData.shopContents[shopItemViewing]);
                if (GameData.players[turnOrder[playerTurn]].strawberries >= GameData.itemPrices[GameData.shopContents[shopItemViewing]]) {
                    leftButtons[turnOrder[playerTurn]].SetCurrentMode(LeftButton.Modes.ConfirmItemBuy);
                } else {
                    leftButtons[turnOrder[playerTurn]].SetCurrentMode(LeftButton.Modes.Inactive);
                }
            } else {
                scoreboards[turnOrder[playerTurn]].SetCurrentMode(GameScoreboard.Modes.NORMAL);
                leftButtons[turnOrder[playerTurn]].SetCurrentMode(LeftButton.Modes.Inactive);
                rightButtons[turnOrder[playerTurn]].SetCurrentMode(RightButton.Modes.Inactive);
                AfterChoice();
            }
        }

        public void BuyItem() {
            // Only send out data if we are the player that bought the item
            if (turnOrder[playerTurn] == GameData.realPlayerID && MadelinePartyModule.CelesteNetConnected()) {
                CelesteNetSendPlayerChoice(PlayerChoiceData.SHOPITEM, 0);
            }
            GameData.Item itemBought = GameData.shopContents[shopItemViewing];
            GameData.players[turnOrder[playerTurn]].items.Add(itemBought);
            shopItemViewing = 0;
            scoreboards[turnOrder[playerTurn]].SetCurrentMode(GameScoreboard.Modes.NORMAL, GameData.shopContents[shopItemViewing]);
            leftButtons[turnOrder[playerTurn]].SetCurrentMode(LeftButton.Modes.Inactive);
            rightButtons[turnOrder[playerTurn]].SetCurrentMode(RightButton.Modes.Inactive);

            ChangeStrawberries(turnOrder[playerTurn], -GameData.itemPrices[itemBought], .08f);
            AfterChoice();
        }

        public void SkipShop() {
            // Only send out data if we are the player that skipped the shop
            if (turnOrder[playerTurn] == GameData.realPlayerID && MadelinePartyModule.CelesteNetConnected()) {
                CelesteNetSendPlayerChoice(PlayerChoiceData.ENTERSHOP, 1);
            }
            scoreboards[turnOrder[playerTurn]].SetCurrentMode(GameScoreboard.Modes.NORMAL);
            leftButtons[turnOrder[playerTurn]].SetCurrentMode(LeftButton.Modes.Inactive);

            AfterChoice();
        }

        public void EnterShop() {
            // Only send out data if we are the player that entered the shop
            if (turnOrder[playerTurn] == GameData.realPlayerID && MadelinePartyModule.CelesteNetConnected()) {
                CelesteNetSendPlayerChoice(PlayerChoiceData.ENTERSHOP, 0);
            }
            shopItemViewing = 0;
            scoreboards[turnOrder[playerTurn]].SetCurrentMode(GameScoreboard.Modes.BUYITEM, GameData.shopContents[shopItemViewing]);
            rightButtons[turnOrder[playerTurn]].SetCurrentMode(RightButton.Modes.CancelItemBuy);
            Console.WriteLine(GameData.players[turnOrder[playerTurn]].strawberries + " " + GameData.itemPrices[GameData.shopContents[shopItemViewing]]);
            if (GameData.players[turnOrder[playerTurn]].strawberries >= GameData.itemPrices[GameData.shopContents[shopItemViewing]]) {
                leftButtons[turnOrder[playerTurn]].SetCurrentMode(LeftButton.Modes.ConfirmItemBuy);
            } else {
                leftButtons[turnOrder[playerTurn]].SetCurrentMode(LeftButton.Modes.Inactive);
            }
        }

        public void SkipHeart() {
            // Only send out data if we are the player that skipped the heart
            if (turnOrder[playerTurn] == GameData.realPlayerID && MadelinePartyModule.CelesteNetConnected()) {
                CelesteNetSendPlayerChoice(PlayerChoiceData.HEART, 1);
            }
            scoreboards[turnOrder[playerTurn]].SetCurrentMode(GameScoreboard.Modes.NORMAL);
            leftButtons[turnOrder[playerTurn]].SetCurrentMode(LeftButton.Modes.Inactive);

            AfterChoice();
        }

        public void BuyHeart() {
            // Only send out data if we are the player that bought the heart
            if (turnOrder[playerTurn] == GameData.realPlayerID && MadelinePartyModule.CelesteNetConnected()) {
                CelesteNetSendPlayerChoice(PlayerChoiceData.HEART, 0);
            }
            ChangeStrawberries(turnOrder[playerTurn], -GameData.heartCost, 0.08f);
            GameData.players[turnOrder[playerTurn]].hearts++;
            scoreboards[turnOrder[playerTurn]].SetCurrentMode(GameScoreboard.Modes.NORMAL);
            rightButtons[turnOrder[playerTurn]].SetCurrentMode(RightButton.Modes.Inactive);

            GameData.heartSpaceID = -1;
            // Only send out data if we are the player that bought the heart
            if (turnOrder[playerTurn] == GameData.realPlayerID) {
                List<BoardSpace> possibleHeartSpaces = boardSpaces.FindAll((s) => s.heartSpace && GameData.players[turnOrder[playerTurn]].token.currentSpace.ID != s.ID);
                GameData.heartSpaceID = possibleHeartSpaces[rand.Next(possibleHeartSpaces.Count)].ID;
                if (MadelinePartyModule.CelesteNetConnected()) {
                    CelesteNetSendPlayerChoice(PlayerChoiceData.HEARTSPACEID, (int)GameData.heartSpaceID);
                }
            }
            Add(new Coroutine(WaitForNewHeartSpaceCoroutine()));
        }

        private IEnumerator WaitForNewHeartSpaceCoroutine() {
            while (GameData.heartSpaceID < 0) {
                yield return null;
            }

            AfterChoice();
        }

        private void AfterChoice() {
            status = BoardStatus.PLAYERMOVE;
            leftButtons[turnOrder[playerTurn]].SetCurrentMode(LeftButton.Modes.Inactive);
            rightButtons[turnOrder[playerTurn]].SetCurrentMode(RightButton.Modes.Inactive);

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
            if (turnOrder[playerTurn] == GameData.realPlayerID && MadelinePartyModule.CelesteNetConnected()) {
                CelesteNetSendPlayerChoice(PlayerChoiceData.DIRECTION, (int)chosen);
            }
            leftButtons[turnOrder[playerTurn]].SetCurrentMode(LeftButton.Modes.Inactive);
            rightButtons[turnOrder[playerTurn]].SetCurrentMode(RightButton.Modes.Inactive);
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
            if (playerTurn >= GameData.playerNumber) {
                playerTurn = 0;
                turnDisplay = GameData.turn;
                GameData.turn++;
                Add(new Coroutine(InitiateMinigame()));
            } else {
                leftButtons[turnOrder[playerTurn]].SetCurrentMode(LeftButton.Modes.Dice);
                if (GameData.players[turnOrder[playerTurn]].items.Contains(GameData.Item.DOUBLEDICE)) {
                    rightButtons[turnOrder[playerTurn]].SetCurrentMode(RightButton.Modes.DoubleDice);
                }
            }
        }

        public void ChoseMinigame(int chosen, long startTime = 0) {
            List<LevelData> minigames = level.Session.MapData.Levels.FindAll((obj) => obj.Name.StartsWith("z_Minigame", StringComparison.InvariantCulture));
            minigames.RemoveAll((obj) => GameData.playedMinigames.Contains(obj.Name));
            GameData.minigame = minigames[chosen];
            GameData.playedMinigames.Add(GameData.minigame.Name);
            minigameStartTime = DateTime.FromFileTimeUtc(startTime);
        }

        public IEnumerator InitiateMinigame() {
            hackfixRespawn = false; //FIXME hackfix
            if (GameData.gnetHost) {
                List<LevelData> minigames = level.Session.MapData.Levels.FindAll((obj) => obj.Name.StartsWith("z_Minigame", StringComparison.InvariantCulture));
                minigames.RemoveAll((obj) => GameData.playedMinigames.Contains(obj.Name));
                int chosenMinigame = rand.Next(minigames.Count);
                if (riggedMinigame != null) {
                    chosenMinigame = minigames.IndexOf(riggedMinigame);
                    riggedMinigame = null;
                }
                minigameStartTime = DateTime.UtcNow.AddSeconds(3);
                Console.WriteLine("Minigame chosen: " + chosenMinigame);
                ChoseMinigame(chosenMinigame);
                if (MadelinePartyModule.CelesteNetConnected()) {
                    CelesteNetSendMinigameStart(chosenMinigame, minigameStartTime.ToFileTimeUtc());
                }
            }
            Console.WriteLine("Host? " + GameData.gnetHost);

            Console.WriteLine("Begin minigame wait");
            while (GameData.minigame == null /*|| minigameStartTime.CompareTo(DateTime.UtcNow) < 0*/) {
                yield return null;
            }

            yield return 3f;
            Console.WriteLine("End minigame wait");

            Player player = level.Tracker.GetEntity<Player>();
            level.OnEndOfFrame += delegate {
                player.Speed = Vector2.Zero;
                Leader.StoreStrawberries(player.Leader);
                level.Remove(player);
                level.UnloadLevel();

                level.Session.Level = GameData.minigame.Name;
                level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(level.Bounds.Left, level.Bounds.Top));
                level.LoadLevel(Player.IntroTypes.None);

                Leader.RestoreStrawberries(player.Leader);
            };
        }

        public bool IsLandableSpace(char space) {
            return space != ' ';
        }

        // Returns which number player this is, ignoring unpicked characters
        private int getRelativePlayerID(int absPlayerID) {
            int currPlayer = 0;
            for (int i = 0; i < GameData.players.Length; i++) {
                if (i == absPlayerID) {
                    return currPlayer;
                }
                if (GameData.players[i] != null) {
                    currPlayer++;
                }
            }
            return currPlayer;
        }

        private IEnumerator DieRollAnimation(int playerID, int[] rolls) {
            leftButtons[playerID].SetCurrentMode(LeftButton.Modes.Inactive);
            rightButtons[playerID].SetCurrentMode(RightButton.Modes.Inactive);
            while (removingDieRolls) yield return null;
            foreach (int roll in rolls) {
                if (roll == 0) continue;
                DieNumber number = new DieNumber(this, roll - 1, status == BoardStatus.GAMESTART ? getRelativePlayerID(playerID) : numbersToDisplay.Count);
                level.Add(number);
                numbersToDisplay.Add(number);
                // Number of expected die rolls
                int numberOfSpaces = status == BoardStatus.GAMESTART ? GameData.playerNumber : rolls.Length;
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
            if (MadelinePartyModule.CelesteNetConnected() && playerID == GameData.realPlayerID) {
                CelesteNetSendDieRolls(rolls);
            }
            Add(new Coroutine(DieRollAnimation(playerID, rolls)));
        }

        public void RollDice(int playerID, bool doubleDice) {
            List<int> rolls = new List<int>();
            if (status == BoardStatus.GAMESTART) {
                rolls.Add(turnOrderRolls[playerID]);
            } else {
                rolls.Add(rand.Next(10) + 1);
                if (doubleDice) {
                    rolls.Add(rand.Next(10) + 1);
                    GameData.players[playerID].items.Remove(GameData.Item.DOUBLEDICE);
                }
            }
            if (MadelinePartyModule.CelesteNetConnected() && playerID == GameData.realPlayerID) {
                CelesteNetSendDieRolls(rolls.ToArray());
            }
            Add(new Coroutine(DieRollAnimation(playerID, rolls.ToArray())));
        }

        public void RollDice(int playerID, int roll) {
            leftButtons[playerID].SetCurrentMode(LeftButton.Modes.Inactive);
            rightButtons[playerID].SetCurrentMode(RightButton.Modes.Inactive);
            if (status == BoardStatus.GAMESTART) {
                GameData.players[playerID].StartingRoll = roll;
                Console.WriteLine("Roll: " + playerID + " " + roll);
                diceRolled++;
                if (diceRolled >= GameData.playerNumber) {
                    Add(new Coroutine(RemoveDieRollsAnimation()));
                    diceRolled = 0;
                    List<PlayerData> temp = new List<PlayerData>(GameData.players);
                    temp.Sort();
                    int playersGoneThrough = 0;
                    for (int i = 0; i < temp.Count; i++) {
                        if (temp[i] == null) continue;
                        turnOrder[playersGoneThrough] = temp[i].TokenSelected;
                        Logger.Log("MadelineParty", "Turn order log: Player ID " + turnOrder[playersGoneThrough] + " with a roll of " + temp[i].StartingRoll);
                        playersGoneThrough++;
                    }
                    status = BoardStatus.WAITING;
                    leftButtons[turnOrder[0]].SetCurrentMode(LeftButton.Modes.Dice);
                    if (GameData.players[turnOrder[0]].items.Contains(GameData.Item.DOUBLEDICE)) {
                        rightButtons[turnOrder[0]].SetCurrentMode(RightButton.Modes.DoubleDice);
                    }
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

        public void SubHUDRender() {
            foreach(BoardSpace space in boardSpaces) {
                if(space.ID != GameData.heartSpaceID && space.type == 'g' && greenSpaces.TryGetValue(space.greenSpaceEvent, out GreenSpaceEvent spaceEvent)) {
                    spaceEvent.RenderSubHUD(space);
                }
            }
        }

        public override void Render() {
            base.Render();

            foreach (BoardSpace space in boardSpaces) {
                Vector2 spacePos = Position + new Vector2(space.x, space.y);
                foreach (BoardSpace dest in space.destinations) {
                    Draw.Line(spacePos, Position + new Vector2(dest.x, dest.y), Color.White);
                }
            }

            foreach (BoardSpace space in boardSpaces) {
                Vector2 spacePos = Position + new Vector2(space.x, space.y);
                if (space.ID != GameData.heartSpaceID) {
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
            Random rand = new Random((int)GameData.turnOrderSeed);
            for (int i = 0; i < 4; i++) {
                turnOrderRolls[i] = list[rand.Next(list.Count)];
                list.Remove(turnOrderRolls[i]);
            }
        }

        // Whether one of the two buttons for the player specified is active
        public bool isWaitingOnPlayer(int playerID) {
            return leftButtons[playerID].GetCurrentMode() != LeftButton.Modes.Inactive || rightButtons[playerID].GetCurrentMode() != RightButton.Modes.Inactive;
        }

        public void DoGreenSpace(BoardSpace space, Action next) {
            if(greenSpaces.TryGetValue(space.greenSpaceEvent, out GreenSpaceEvent spaceEvent)) {
                spaceEvent.RunGreenSpace(this, space, next ?? (() => { }));
            }
        }
    }
}