using System;
using System.Collections;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod;
using Celeste.Mod.Ghost.Net;
using MadelineParty.Ghostnet;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty {
    public class BoardController : Entity, IPauseUpdateGhostnetChat {
        public enum BoardStatus {
            WAITING,
            GAMESTART,
            PLAYERMOVE,
        }

        public enum Direction {
            UP = 0,
            DOWN = 1,
            LEFT = 2,
            RIGHT = 3
        }

        public static string[] TokenPaths = { "madeline/normal00", "badeline/normal00", "theo/excited00", "granny/normal00" };

        private List<Vector2> playerMovePath = null;

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
        private static int[] oneToTen = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        private int playerTurn = 0;
        private int turnDisplay = -1;

        private List<LeftButton> leftButtons = new List<LeftButton>();
        private List<RightButton> rightButtons = new List<RightButton>();

        private List<GameScoreboard> scoreboards = new List<GameScoreboard>();

        public List<MTexture> diceNumbers;
        private List<DieNumber> numbersToDisplay = new List<DieNumber>();

        public static DieRollData delayedDieRoll;

        private DateTime minigameStartTime;

        public BoardStatus status = BoardStatus.GAMESTART;

        public static BoardController Instance;

        public BoardController(EntityData data) : base(data.Position) {
            Instance = this;
            boardDecals = new Decal[board.Length, board[0].Length];
            diceNumbers = GFX.Game.GetAtlasSubtextures("decals/madelineparty/dicenumbers/dice_");
            AddTag(TagsExt.SubHUD);
            AddTag(Tags.PauseUpdate);
            AddTag(Tags.FrozenUpdate);
        }

        // Maybe retrieve this from settings depending on if I want this mod to be moddable
        // b = blue space
        // r = red space
        // s = start
        // i = item shop
        public char[][] board = { new char[]{ 'b', 'r', 'b', 'r', 'r', 'b', ' ', ' ', ' ', ' ', ' ', ' ' },
                                   new char[]{ 'b', ' ', ' ', 'b', ' ', 'b', 'b', ' ', ' ', ' ', ' ', ' ' },
                                   new char[]{ 'b', ' ', ' ', 'b', ' ', ' ', 'b', ' ', ' ', ' ', ' ', ' ' },
                                   new char[]{ 'b', ' ', ' ', 'r', ' ', ' ', 'r', ' ', ' ', ' ', ' ', ' ' },
                                   new char[]{ 'r', 'r', ' ', 'i', ' ', ' ', 'b', ' ', ' ', ' ', ' ', ' ' },
                                   new char[]{ ' ', 'r', ' ', 'b', 'r', ' ', 'b', ' ', ' ', 'r', 'b', 'b' },
                                   new char[]{ ' ', 'b', 'b', ' ', 'r', 'b', 'b', 'b', 'r', 'b', ' ', 'b' },
                                   new char[]{ ' ', ' ', 'r', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', 'r' },
                                   new char[]{ 's', 'b', 'b', 'b', 'b', 'b', 'r', 'b', ' ', ' ', ' ', 'b' },
                                   new char[]{ ' ', ' ', ' ', ' ', ' ', ' ', ' ', 'b', ' ', ' ', ' ', 'b' },
                                   new char[]{ ' ', ' ', ' ', ' ', ' ', ' ', ' ', 'b', ' ', ' ', ' ', 'b' },
                                   new char[]{ ' ', ' ', ' ', ' ', ' ', ' ', ' ', 'b', 'b', 'r', 'r', 'r' } };

        private Decal[,] boardDecals;

        public char[][] directions = { new char[]{ '>', '>', '>', '.', '>', 'v', ' ', ' ', ' ', ' ', ' ', ' ' },
                                        new char[]{ '^', ' ', ' ', 'v', ' ', '>', 'v', ' ', ' ', ' ', ' ', ' ' },
                                        new char[]{ '^', ' ', ' ', 'v', ' ', ' ', 'v', ' ', ' ', ' ', ' ', ' ' },
                                        new char[]{ '^', ' ', ' ', 'v', ' ', ' ', 'v', ' ', ' ', ' ', ' ', ' ' },
                                        new char[]{ '^', '<', ' ', 'v', ' ', ' ', 'v', ' ', ' ', ' ', ' ', ' ' },
                                        new char[]{ ' ', '^', ' ', '>', 'v', ' ', 'v', ' ', ' ', '>', '>', 'v' },
                                        new char[]{ ' ', '^', '<', ' ', '>', '>', '>', '>', '>', '^', ' ', 'v' },
                                        new char[]{ ' ', ' ', '^', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', 'v' },
                                        new char[]{ '>', '>', '^', '<', '<', '<', '<', '<', ' ', ' ', ' ', 'v' },
                                        new char[]{ ' ', ' ', ' ', ' ', ' ', ' ', ' ', '^', ' ', ' ', ' ', 'v' },
                                        new char[]{ ' ', ' ', ' ', ' ', ' ', ' ', ' ', '^', ' ', ' ', ' ', 'v' },
                                        new char[]{ ' ', ' ', ' ', ' ', ' ', ' ', ' ', '^', '<', '<', '<', '<' } };

        public override void Added(Scene scene) {
            base.Added(scene);
            level = SceneAs<Level>();
            level.CanRetry = false;

            for (int i = 0; i < board.Length; i++) {
                for (int j = 0; j < board[i].Length; j++) {
                    switch (board[i][j]) {
                        case 'r':
                            level.Add(boardDecals[i, j] = new Decal("madelineparty/redspace", new Vector2(this.X + j * 10, this.Y + i * 10), new Vector2(1, 1), 0));
                            if (i > 0 && SpaceAccessibleFrom(i, j, i - 1, j)) {
                                level.Add(new Decal("madelineparty/verticalconnector", new Vector2(this.X + j * 10, this.Y + i * 10 - 5), new Vector2(1, 1), 1));
                            }
                            if (i < board.Length - 1 && SpaceAccessibleFrom(i, j, i + 1, j)) {
                                level.Add(new Decal("madelineparty/verticalconnector", new Vector2(this.X + j * 10, this.Y + i * 10 + 5), new Vector2(1, 1), 1));
                            }
                            if (j > 0 && SpaceAccessibleFrom(i, j, i, j - 1)) {
                                level.Add(new Decal("madelineparty/horizontalconnector", new Vector2(this.X + j * 10 - 5, this.Y + i * 10), new Vector2(1, 1), 1));
                            }
                            if (j < board[i].Length - 1 && SpaceAccessibleFrom(i, j, i, j + 1)) {
                                level.Add(new Decal("madelineparty/horizontalconnector", new Vector2(this.X + j * 10 + 5, this.Y + i * 10), new Vector2(1, 1), 1));
                            }
                            break;
                        case 'b':
                            level.Add(boardDecals[i, j] = new Decal("madelineparty/bluespace", new Vector2(this.X + j * 10, this.Y + i * 10), new Vector2(1, 1), 0));
                            if (i > 0 && SpaceAccessibleFrom(i, j, i - 1, j)) {
                                level.Add(new Decal("madelineparty/verticalconnector", new Vector2(this.X + j * 10, this.Y + i * 10 - 5), new Vector2(1, 1), 1));
                            }
                            if (i < board.Length - 1 && SpaceAccessibleFrom(i, j, i + 1, j)) {
                                level.Add(new Decal("madelineparty/verticalconnector", new Vector2(this.X + j * 10, this.Y + i * 10 + 5), new Vector2(1, 1), 1));
                            }
                            if (j > 0 && SpaceAccessibleFrom(i, j, i, j - 1)) {
                                level.Add(new Decal("madelineparty/horizontalconnector", new Vector2(this.X + j * 10 - 5, this.Y + i * 10), new Vector2(1, 1), 1));
                            }
                            if (j < board[i].Length - 1 && SpaceAccessibleFrom(i, j, i, j + 1)) {
                                level.Add(new Decal("madelineparty/horizontalconnector", new Vector2(this.X + j * 10 + 5, this.Y + i * 10), new Vector2(1, 1), 1));
                            }
                            break;
                        case 'i':
                            level.Add(boardDecals[i, j] = new Decal("madelineparty/shopspace", new Vector2(this.X + j * 10, this.Y + i * 10), new Vector2(1, 1), 0));
                            if (i > 0 && SpaceAccessibleFrom(i, j, i - 1, j)) {
                                level.Add(new Decal("madelineparty/verticalconnector", new Vector2(this.X + j * 10, this.Y + i * 10 - 5), new Vector2(1, 1), 1));
                            }
                            if (i < board.Length - 1 && SpaceAccessibleFrom(i, j, i + 1, j)) {
                                level.Add(new Decal("madelineparty/verticalconnector", new Vector2(this.X + j * 10, this.Y + i * 10 + 5), new Vector2(1, 1), 1));
                            }
                            if (j > 0 && SpaceAccessibleFrom(i, j, i, j - 1)) {
                                level.Add(new Decal("madelineparty/horizontalconnector", new Vector2(this.X + j * 10 - 5, this.Y + i * 10), new Vector2(1, 1), 1));
                            }
                            if (j < board[i].Length - 1 && SpaceAccessibleFrom(i, j, i, j + 1)) {
                                level.Add(new Decal("madelineparty/horizontalconnector", new Vector2(this.X + j * 10 + 5, this.Y + i * 10), new Vector2(1, 1), 1));
                            }
                            break;
                        case 's':
                            if (i > 0 && SpaceAccessibleFrom(i, j, i - 1, j)) {
                                level.Add(new Decal("madelineparty/verticalconnector", new Vector2(this.X + j * 10, this.Y + i * 10 - 5), new Vector2(1, 1), 1));
                            }
                            if (i < board.Length - 1 && SpaceAccessibleFrom(i, j, i + 1, j)) {
                                level.Add(new Decal("madelineparty/verticalconnector", new Vector2(this.X + j * 10, this.Y + i * 10 + 5), new Vector2(1, 1), 1));
                            }
                            if (j > 0 && SpaceAccessibleFrom(i, j, i, j - 1)) {
                                level.Add(new Decal("madelineparty/horizontalconnector", new Vector2(this.X + j * 10 - 5, this.Y + i * 10), new Vector2(1, 1), 1));
                            }
                            if (j < board[i].Length - 1 && SpaceAccessibleFrom(i, j, i, j + 1)) {
                                level.Add(new Decal("madelineparty/horizontalconnector", new Vector2(this.X + j * 10 + 5, this.Y + i * 10), new Vector2(1, 1), 1));
                            }
                            int tokensAdded = 0;
                            for (int k = 0; k < GameData.players.Length; k++) {
                                if (GameData.players[k] != null) {
                                    if (!GameData.gameStarted) {
                                        PlayerToken token = new PlayerToken(TokenPaths[GameData.players[k].TokenSelected], ScreenCoordsFromBoardCoords(new Vector2(j, i), new Vector2(-24, tokensAdded * 18)), new Vector2(.25f, .25f), -1, new Vector2(i, j));
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

                            break;
                    }
                }
            }

            // Replace the decal at the heart space
            Decal decal = boardDecals[(int)GameData.heartSpace.X, (int)GameData.heartSpace.Y];
            decal.RemoveSelf();
            boardDecals[(int)GameData.heartSpace.X, (int)GameData.heartSpace.Y] = new Decal("madelineparty/heartstill", decal.Position, new Vector2(.75f, .75f), decal.Depth);
            Scene.Add(boardDecals[(int)GameData.heartSpace.X, (int)GameData.heartSpace.Y]);
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
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

                if (delayedDieRoll != null) {
                    if (isWaitingOnPlayer(GameData.playerSelectTriggers[delayedDieRoll.playerID])) {
                        string rollString = "";
                        foreach (int i in delayedDieRoll.rolls) {
                            rollString += i + ", ";
                        }
                        Logger.Log("MadelineParty", "Delayed emote interpreted as die roll from player " + delayedDieRoll.playerID + ". Rolls: " + rollString);

                        if (delayedDieRoll.rolls.Length == 2)
                            GameData.players[GameData.playerSelectTriggers[delayedDieRoll.playerID]].items.Remove(GameData.Item.DOUBLEDICE);
                        RollDice(GameData.playerSelectTriggers[delayedDieRoll.playerID], delayedDieRoll.rolls);
                    }
                    delayedDieRoll = null;
                }
            }
            GameData.gameStarted = true;
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
                        playerMovePath = new List<Vector2> { playerTokens[movingPlayerID].currentSpace };
                        status = BoardStatus.WAITING;

                        // If we've set the left button to a direction yet
                        bool leftUsed = false;
                        if (SpaceAccessibleFrom(playerMovePath[playerMoveProgress].X, playerMovePath[playerMoveProgress].Y, playerMovePath[playerMoveProgress].X, playerMovePath[playerMoveProgress].Y - 1)) {
                            if (leftUsed) {
                                rightButtons[turnOrder[playerTurn]].SetCurrentMode(RightButton.Modes.Left);
                            } else {
                                leftButtons[turnOrder[playerTurn]].SetCurrentMode(LeftButton.Modes.Left);
                                leftUsed = true;
                            }
                        }
                        if (SpaceAccessibleFrom(playerMovePath[playerMoveProgress].X, playerMovePath[playerMoveProgress].Y, playerMovePath[playerMoveProgress].X, playerMovePath[playerMoveProgress].Y + 1)) {
                            if (leftUsed) {
                                rightButtons[turnOrder[playerTurn]].SetCurrentMode(RightButton.Modes.Right);
                            } else {
                                leftButtons[turnOrder[playerTurn]].SetCurrentMode(LeftButton.Modes.Right);
                                leftUsed = true;
                            }
                        }
                        if (SpaceAccessibleFrom(playerMovePath[playerMoveProgress].X, playerMovePath[playerMoveProgress].Y, playerMovePath[playerMoveProgress].X - 1, playerMovePath[playerMoveProgress].Y)) {
                            if (leftUsed) {
                                rightButtons[turnOrder[playerTurn]].SetCurrentMode(RightButton.Modes.Up);
                            } else {
                                leftButtons[turnOrder[playerTurn]].SetCurrentMode(LeftButton.Modes.Up);
                                leftUsed = true;
                            }
                        }
                        if (SpaceAccessibleFrom(playerMovePath[playerMoveProgress].X, playerMovePath[playerMoveProgress].Y, playerMovePath[playerMoveProgress].X + 1, playerMovePath[playerMoveProgress].Y)) {
                            if (leftUsed) {
                                rightButtons[turnOrder[playerTurn]].SetCurrentMode(RightButton.Modes.Down);
                            } else {
                                leftButtons[turnOrder[playerTurn]].SetCurrentMode(LeftButton.Modes.Down);
                            }
                        }
                        break;
                    }
                    Vector2 approaching = ScreenCoordsFromBoardCoords(SwapXY(playerMovePath[playerMoveProgress + 1]));
                    if (playerTokens[movingPlayerID].Position.Equals(approaching)) {
                        playerMoveProgress++;
                        playerTokens[movingPlayerID].currentSpace = playerMovePath[playerMoveProgress];

                        // If we're on the heart space
                        if (GameData.heartSpace == playerTokens[movingPlayerID].currentSpace && GameData.players[movingPlayerID].strawberries >= GameData.heartCost) {
                            status = BoardStatus.WAITING;
                            leftButtons[turnOrder[playerTurn]].SetCurrentMode(LeftButton.Modes.ConfirmHeartBuy);
                            rightButtons[turnOrder[playerTurn]].SetCurrentMode(RightButton.Modes.CancelHeartBuy);
                            scoreboards[turnOrder[playerTurn]].SetCurrentMode(GameScoreboard.Modes.BUYHEART);
                        }
                        // If we're at the item shop and have enough free space
                        else if (board[(int)playerTokens[movingPlayerID].currentSpace.X][(int)playerTokens[movingPlayerID].currentSpace.Y] == 'i' && GameData.players[movingPlayerID].items.Count < GameData.maxItems) {
                            status = BoardStatus.WAITING;
                            leftButtons[turnOrder[playerTurn]].SetCurrentMode(LeftButton.Modes.ConfirmShopEnter);
                            rightButtons[turnOrder[playerTurn]].SetCurrentMode(RightButton.Modes.CancelShopEnter);
                            scoreboards[turnOrder[playerTurn]].SetCurrentMode(GameScoreboard.Modes.ENTERSHOP);
                        } else if (playerMoveProgress == playerMoveDistance) {
                            playerMoveDistance = 0;
                            playerMovePath = null;
                            playerMoveProgress = 0;
                            status = BoardStatus.WAITING;
                            switch (board[(int)playerTokens[movingPlayerID].currentSpace.X][(int)playerTokens[movingPlayerID].currentSpace.Y]) {
                                case 'b':
                                    scoreboards[movingPlayerID].StrawberryChange(3);
                                    GameData.players[movingPlayerID].ChangeStrawberries(3);
                                    break;
                                case 'r':
                                    scoreboards[movingPlayerID].StrawberryChange(-3);
                                    GameData.players[movingPlayerID].ChangeStrawberries(-3);
                                    break;
                            }
                            EndTurn();
                        }
                        break;
                    }
                    playerTokens[movingPlayerID].Position = Calc.Approach(playerTokens[movingPlayerID].Position, approaching, 80f * Engine.DeltaTime);
                    break;
                default:
                    break;
            }
        }

        private void GhostNetSendPlayerChoice(PlayerChoiceData.ChoiceType type, int choice) {
            GhostNetModule.Instance.Client.Connection.SendManagement(new GhostNetFrame
            {
                EmoteConverter.convertPlayerChoiceToEmoteChunk(new PlayerChoiceData
                {
                    playerID = GhostNetModule.Instance.Client.PlayerID,
                    playerName = GhostNetModule.Instance.Client.PlayerName.Name,
                    choiceType = type,
                    choice = choice
                })
            }, true);
        }

        private void GhostNetSendDieRolls(int[] rolls) {
            GhostNetModule.Instance.Client.Connection.SendManagement(new GhostNetFrame
            {
                EmoteConverter.convertDieRollToEmoteChunk(new DieRollData
                {
                    playerID = GhostNetModule.Instance.Client.PlayerID,
                    playerName = GhostNetModule.Instance.Client.PlayerName.Name,
                    rolls = rolls
                })
            }, true);
        }

        private void GhostNetSendMinigameStart(int choice, long time = 0) {
            GhostNetModule.Instance.Client.Connection.SendManagement(new GhostNetFrame
            {
                EmoteConverter.convertMinigameStartToEmoteChunk(new MinigameStartData
                {
                    playerID = GhostNetModule.Instance.Client.PlayerID,
                    playerName = GhostNetModule.Instance.Client.PlayerName.Name,
                    choice = choice,
                    gameStart = time
                })
            }, true);
        }

        public void SkipItem() {
            // Only send out data if we are the player that skipped the item
            if (turnOrder[playerTurn] == GameData.realPlayerID && MadelinePartyModule.ghostnetConnected) {
                GhostNetSendPlayerChoice(PlayerChoiceData.ChoiceType.SHOPITEM, 1);
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
            if (turnOrder[playerTurn] == GameData.realPlayerID && MadelinePartyModule.ghostnetConnected) {
                GhostNetSendPlayerChoice(PlayerChoiceData.ChoiceType.SHOPITEM, 0);
            }
            GameData.Item itemBought = GameData.shopContents[shopItemViewing];
            GameData.players[turnOrder[playerTurn]].items.Add(itemBought);
            shopItemViewing = 0;
            scoreboards[turnOrder[playerTurn]].SetCurrentMode(GameScoreboard.Modes.NORMAL, GameData.shopContents[shopItemViewing]);
            leftButtons[turnOrder[playerTurn]].SetCurrentMode(LeftButton.Modes.Inactive);
            rightButtons[turnOrder[playerTurn]].SetCurrentMode(RightButton.Modes.Inactive);

            scoreboards[turnOrder[playerTurn]].StrawberryChange(-GameData.itemPrices[itemBought], .08f);
            GameData.players[turnOrder[playerTurn]].ChangeStrawberries(-GameData.itemPrices[itemBought]);
            AfterChoice();
        }

        public void SkipShop() {
            // Only send out data if we are the player that skipped the shop
            if (turnOrder[playerTurn] == GameData.realPlayerID && MadelinePartyModule.ghostnetConnected) {
                GhostNetSendPlayerChoice(PlayerChoiceData.ChoiceType.ENTERSHOP, 1);
            }
            scoreboards[turnOrder[playerTurn]].SetCurrentMode(GameScoreboard.Modes.NORMAL);
            leftButtons[turnOrder[playerTurn]].SetCurrentMode(LeftButton.Modes.Inactive);

            AfterChoice();
        }

        public void EnterShop() {
            // Only send out data if we are the player that entered the shop
            if (turnOrder[playerTurn] == GameData.realPlayerID && MadelinePartyModule.ghostnetConnected) {
                GhostNetSendPlayerChoice(PlayerChoiceData.ChoiceType.ENTERSHOP, 0);
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
            if (turnOrder[playerTurn] == GameData.realPlayerID && MadelinePartyModule.ghostnetConnected) {
                GhostNetSendPlayerChoice(PlayerChoiceData.ChoiceType.HEART, 1);
            }
            scoreboards[turnOrder[playerTurn]].SetCurrentMode(GameScoreboard.Modes.NORMAL);
            leftButtons[turnOrder[playerTurn]].SetCurrentMode(LeftButton.Modes.Inactive);

            AfterChoice();
        }

        public void BuyHeart() {
            // Only send out data if we are the player that bought the heart
            if (turnOrder[playerTurn] == GameData.realPlayerID && MadelinePartyModule.ghostnetConnected) {
                GhostNetSendPlayerChoice(PlayerChoiceData.ChoiceType.HEART, 0);
            }
            scoreboards[turnOrder[playerTurn]].StrawberryChange(-GameData.heartCost, .08f);
            GameData.players[turnOrder[playerTurn]].ChangeStrawberries(-GameData.heartCost);
            GameData.players[turnOrder[playerTurn]].hearts++;
            scoreboards[turnOrder[playerTurn]].SetCurrentMode(GameScoreboard.Modes.NORMAL);
            rightButtons[turnOrder[playerTurn]].SetCurrentMode(RightButton.Modes.Inactive);

            // Replace the heart decal
            Decal decal = boardDecals[(int)GameData.heartSpace.X, (int)GameData.heartSpace.Y];
            decal.RemoveSelf();
            string replacement = "madelineparty/";
            switch (board[(int)playerTokens[movingPlayerID].currentSpace.X][(int)playerTokens[movingPlayerID].currentSpace.Y]) {
                case 'b':
                    replacement += "bluespace";
                    break;
                case 'r':
                    replacement += "redspace";
                    break;
            }
            boardDecals[(int)GameData.heartSpace.X, (int)GameData.heartSpace.Y] = new Decal(replacement, decal.Position, new Vector2(1, 1), decal.Depth);
            Scene.Add(boardDecals[(int)GameData.heartSpace.X, (int)GameData.heartSpace.Y]);

            List<Vector2> possibleHeartSpaces = new List<Vector2>();

            for (int i = 0; i < board.Length; i++) {
                for (int j = 0; j < board[i].Length; j++) {
                    if (board[i][j] != 'i' && directions[i][j] != '.' && IsLandableSpace(board[i][j]) && !SpaceAtOrNearStart(i, j) && (i != (int)GameData.heartSpace.X || j != (int)GameData.heartSpace.Y)) {
                        possibleHeartSpaces.Add(new Vector2(i, j));
                    }
                }
            }
            GameData.heartSpace = new Vector2(-1, -1);
            // Only send out data if we are the player that bought the heart
            if (turnOrder[playerTurn] == GameData.realPlayerID) {
                GameData.heartSpace = possibleHeartSpaces[rand.Next(possibleHeartSpaces.Count)];
                if (MadelinePartyModule.ghostnetConnected) {
                    GhostNetSendPlayerChoice(PlayerChoiceData.ChoiceType.HEARTX, (int)GameData.heartSpace.X);
                    GhostNetSendPlayerChoice(PlayerChoiceData.ChoiceType.HEARTY, (int)GameData.heartSpace.Y);
                }
            }
            Add(new Coroutine(WaitForNewHeartSpaceCoroutine()));
        }

        private IEnumerator WaitForNewHeartSpaceCoroutine() {
            while (GameData.heartSpace.X < 0 || GameData.heartSpace.Y < 0) {
                yield return null;
            }
            // Replace the decal at the heart space
            Decal decal = boardDecals[(int)GameData.heartSpace.X, (int)GameData.heartSpace.Y];
            decal.RemoveSelf();
            boardDecals[(int)GameData.heartSpace.X, (int)GameData.heartSpace.Y] = new Decal("madelineparty/heartstill", decal.Position, new Vector2(.75f, .75f), decal.Depth);
            base.Scene.Add(boardDecals[(int)GameData.heartSpace.X, (int)GameData.heartSpace.Y]);

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
                if (GameData.heartSpace != playerTokens[movingPlayerID].currentSpace) {
                    switch (board[(int)playerTokens[movingPlayerID].currentSpace.X][(int)playerTokens[movingPlayerID].currentSpace.Y]) {
                        case 'b':
                            scoreboards[movingPlayerID].StrawberryChange(3);
                            GameData.players[movingPlayerID].ChangeStrawberries(3);
                            break;
                        case 'r':
                            scoreboards[movingPlayerID].StrawberryChange(-3);
                            GameData.players[movingPlayerID].ChangeStrawberries(-3);
                            break;
                    }
                }
                EndTurn();
            }
        }

        // Returns if the space is or is adjacent to a start space
        private bool SpaceAtOrNearStart(int i, int j) {
            return board[i][j] == 's' || (i > 0 && board[i - 1][j] == 's') || (i < board.Length - 1 && board[i + 1][j] == 's')
                || (j > 0 && board[i][j - 1] == 's') || (j < board[i].Length - 1 && board[i][j + 1] == 's');
        }

        public void ContinueMovementAfterIntersection(Direction chosen) {
            // Only send out data if we are the player that chose the direction
            if (turnOrder[playerTurn] == GameData.realPlayerID && MadelinePartyModule.ghostnetConnected) {
                GhostNetSendPlayerChoice(PlayerChoiceData.ChoiceType.DIRECTION, (int)chosen);
            }
            leftButtons[turnOrder[playerTurn]].SetCurrentMode(LeftButton.Modes.Inactive);
            rightButtons[turnOrder[playerTurn]].SetCurrentMode(RightButton.Modes.Inactive);
            switch (chosen) {
                case Direction.UP:
                    playerMovePath.Add(new Vector2(playerMovePath[playerMoveProgress].X - 1, playerMovePath[playerMoveProgress].Y));
                    break;
                case Direction.DOWN:
                    playerMovePath.Add(new Vector2(playerMovePath[playerMoveProgress].X + 1, playerMovePath[playerMoveProgress].Y));
                    break;
                case Direction.LEFT:
                    playerMovePath.Add(new Vector2(playerMovePath[playerMoveProgress].X, playerMovePath[playerMoveProgress].Y - 1));
                    break;
                case Direction.RIGHT:
                    playerMovePath.Add(new Vector2(playerMovePath[playerMoveProgress].X, playerMovePath[playerMoveProgress].Y + 1));
                    break;
            }

            for (int i = 1; i < playerMoveDistance; i++) {
                // Which ways the player can go from here
                bool up = false, down = false, left = false, right = false;
                if (SpaceAccessibleFrom(playerMovePath[i].X, playerMovePath[i].Y, playerMovePath[i].X - 1, playerMovePath[i].Y)) {
                    left = true;
                }
                if (SpaceAccessibleFrom(playerMovePath[i].X, playerMovePath[i].Y, playerMovePath[i].X + 1, playerMovePath[i].Y)) {
                    right = true;
                }
                if (SpaceAccessibleFrom(playerMovePath[i].X, playerMovePath[i].Y, playerMovePath[i].X, playerMovePath[i].Y - 1)) {
                    up = true;
                }
                if (SpaceAccessibleFrom(playerMovePath[i].X, playerMovePath[i].Y, playerMovePath[i].X, playerMovePath[i].Y + 1)) {
                    down = true;
                }

                if (directions[(int)playerMovePath[i].X][(int)playerMovePath[i].Y] != '.') {
                    if (left) {
                        playerMovePath.Add(new Vector2(playerMovePath[i].X - 1, playerMovePath[i].Y));
                    } else if (right) {
                        playerMovePath.Add(new Vector2(playerMovePath[i].X + 1, playerMovePath[i].Y));
                    } else if (up) {
                        playerMovePath.Add(new Vector2(playerMovePath[i].X, playerMovePath[i].Y - 1));
                    } else if (down) {
                        playerMovePath.Add(new Vector2(playerMovePath[i].X, playerMovePath[i].Y + 1));
                    }
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
            if (GameData.gnetHost) {
                List<LevelData> minigames = level.Session.MapData.Levels.FindAll((obj) => obj.Name.StartsWith("z_Minigame", StringComparison.InvariantCulture));
                minigames.RemoveAll((obj) => GameData.playedMinigames.Contains(obj.Name));
                int chosenMinigame = rand.Next(minigames.Count);
                minigameStartTime = DateTime.UtcNow.AddSeconds(3);
                Console.WriteLine("Minigame chosen: " + chosenMinigame);
                ChoseMinigame(chosenMinigame);
                if (MadelinePartyModule.ghostnetConnected) {
                    GhostNetSendMinigameStart(chosenMinigame, minigameStartTime.ToFileTimeUtc());
                }
            }
            Console.WriteLine("Host? " + GameData.gnetHost);

            Console.WriteLine("Begin minigame wait"); // TODO Minigame synchronization
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

        public bool SpaceAccessibleFrom(float fromX, float fromY, float toX, float toY) {
            return SpaceAccessibleFrom((int)fromX, (int)fromY, (int)toX, (int)toY);
        }

        // Anything called X here is actually Y but I ignore that for simplicity???
        public bool SpaceAccessibleFrom(int fromX, int fromY, int toX, int toY) {
            // If trying to go out of bounds or into an invalid space
            if (toX < 0 || toX >= board.Length || toY < 0 || toY >= board[toX].Length) return false;
            if (!IsLandableSpace(board[toX][toY])) return false;

            // Same row
            if (toX == fromX) {
                // Heading left
                if (toY == fromY - 1) {
                    return directions[fromX][fromY] == '<' || (directions[fromX][fromY] == '.' && directions[toX][toY] != '>');
                }
                // Heading right
                else if (toY == fromY + 1) {
                    return directions[fromX][fromY] == '>' || (directions[fromX][fromY] == '.' && directions[toX][toY] != '<');
                }
                // Staying still
                else {
                    return true;
                }
            }
            // Same column
            else if (toY == fromY) {
                // Heading up
                if (toX == fromX - 1) {
                    return directions[fromX][fromY] == '^' || (directions[fromX][fromY] == '.' && directions[toX][toY] != 'v');
                }
                // Heading down
                else if (toX == fromX + 1) {
                    return directions[fromX][fromY] == 'v' || (directions[fromX][fromY] == '.' && directions[toX][toY] != '^');
                }
                // Staying still
                else {
                    return true;
                }
            }

            return false;
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
                number.MoveNumber((rolls.Length == 1 ? leftButtons[playerID].Position : rightButtons[playerID].Position) + new Vector2(0, 12) + new Vector2(8, 4), Position + new Vector2(board[0].Length * 5 - 10 * (numberOfSpaces - 1) - 4 + 20 * number.posIndex, -24) + new Vector2(8, 4));
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

        // Usually called only due to Ghostnet messages
        public void RollDice(int playerID, int[] rolls) {
            if (MadelinePartyModule.ghostnetConnected && playerID == GameData.realPlayerID) {
                GhostNetSendDieRolls(rolls);
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
            if (MadelinePartyModule.ghostnetConnected && playerID == GameData.realPlayerID) {
                GhostNetSendDieRolls(rolls.ToArray());
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
            Vector2 startingSpace = new Vector2(playerTokens[playerID].currentSpace.X, playerTokens[playerID].currentSpace.Y);
            playerMovePath = new List<Vector2>
            {
                startingSpace
            };
            for (int i = 0; i < playerMoveDistance; i++) {
                // Which ways the player can go from here
                bool up = false, down = false, left = false, right = false;
                if (SpaceAccessibleFrom(playerMovePath[i].X, playerMovePath[i].Y, playerMovePath[i].X - 1, playerMovePath[i].Y)) {
                    left = true;
                }
                if (SpaceAccessibleFrom(playerMovePath[i].X, playerMovePath[i].Y, playerMovePath[i].X + 1, playerMovePath[i].Y)) {
                    right = true;
                }
                if (SpaceAccessibleFrom(playerMovePath[i].X, playerMovePath[i].Y, playerMovePath[i].X, playerMovePath[i].Y - 1)) {
                    up = true;
                }
                if (SpaceAccessibleFrom(playerMovePath[i].X, playerMovePath[i].Y, playerMovePath[i].X, playerMovePath[i].Y + 1)) {
                    down = true;
                }

                if (directions[(int)playerMovePath[i].X][(int)playerMovePath[i].Y] != '.') {
                    if (left) {
                        playerMovePath.Add(new Vector2(playerMovePath[i].X - 1, playerMovePath[i].Y));
                    } else if (right) {
                        playerMovePath.Add(new Vector2(playerMovePath[i].X + 1, playerMovePath[i].Y));
                    } else if (up) {
                        playerMovePath.Add(new Vector2(playerMovePath[i].X, playerMovePath[i].Y - 1));
                    } else if (down) {
                        playerMovePath.Add(new Vector2(playerMovePath[i].X, playerMovePath[i].Y + 1));
                    }
                } else {
                    break;
                }
            }

            status = BoardStatus.PLAYERMOVE;
        }

        public override void Render() {
            base.Render();

            ActiveFont.DrawOutline("Turn " + (turnDisplay == -1 ? GameData.turn : turnDisplay) + "/" + GameData.maxTurns, (Position - level.LevelOffset) * 6 + new Vector2(board[0].Length * 5 - 4, board.Length * 10 + 10) * 6, new Vector2(0.5f, 0.5f), Vector2.One, Color.Blue, 2f, Color.Black);
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
    }
}