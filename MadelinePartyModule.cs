using Microsoft.Xna.Framework;
using Celeste;
using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod;
using MonoMod.RuntimeDetour;
using Celeste.Mod.Ghost.Net;
using Monocle;
using System.Reflection;
using MadelineParty.Ghostnet;

namespace MadelineParty {
    public class MadelinePartyModule : EverestModule {

        // Only one alive module instance can exist at any given time.
        public static MadelinePartyModule Instance;
        public static string START_ROOM = "Game_PlayerNumberSelect";
        public static string MAIN_ROOM = "Game_MainRoom";

        private Level level;

        public static bool ghostnetInstalled, connectionSetup;
        public static bool ghostnetConnected {
            get {
                return Celeste.Mod.Ghost.Net.GhostNetModule.Instance.Client != null;
            }
        }
        private bool chatWasVisible;

        public MadelinePartyModule() {
            Instance = this;
        }

        // The secret code that goes in front of emotes to signify they're from this mod
        public const string emotePrefix = "g:mparty";

        // If you don't need to store any settings, => null
        public override Type SettingsType => null;

        // If you don't need to store any save data, => null
        public override Type SaveDataType => null;

        // Set up any hooks, event handlers and your mod in general here.
        // Load runs before Celeste itself has initialized properly.
        public override void Load() {
            // Stuff that runs orig(self) always
            /* ************************************************ */
            Everest.Events.Level.OnLoadEntity += Level_OnLoadEntity;
            Everest.Events.Level.OnLoadLevel += (level, playerIntro, isFromLoader) => {
                this.level = level;
                if (ghostnetConnected) {
                    GhostnetShowChatIfHidden();
                }
            };
            On.Celeste.LevelEnter.Go += (orig, session, fromSaveData) => {
                if (IsSIDMadelineParty(session.Area.GetSID()) && !session.Level.Equals(START_ROOM)) {
                    session.Level = START_ROOM;
                    session.RespawnPoint = session.GetSpawnPoint(Vector2.Zero);
                    //Player player = level.Tracker.GetEntity<Player>();
                    //sendToStart(player);
                }
                orig(session, fromSaveData);
            };
            On.Celeste.Player.Update += Player_Update;
            On.Monocle.StateMachine.Update += (orig, self) => {
                orig(self);
                //Console.WriteLine(self.Scene.Tracker.GetEntity<Celeste.Player>().StateMachine.State);
            };
            On.Celeste.Level.UnloadLevel += (orig, self) => {
                if (IsSIDMadelineParty(self.Session.Area.GetSID())) {
                    if (ghostnetConnected) {
                        GhostnetHideChat();
                    }
                    TextMenu menu = self.Entities.FindFirst<TextMenu>();
                    if (menu != null) {
                        self.PauseMainMenuOpen = false;
                        menu.RemoveSelf();
                        self.Paused = false;
                    }
                }
                orig(self);
            };
            // Ghostnet chat adds an overlay that stops pauseupdate things from updating
            On.Celeste.Level.Update += (orig, self) => {
                orig(self);
                if (IsSIDMadelineParty(self.Session.Area.GetSID()) && self.FrozenOrPaused && self.Overlay != null) {
                    bool disabled = MInput.Disabled;
                    MInput.Disabled = false;
                    if (!self.Paused) {
                        foreach (Entity item in self[Tags.FrozenUpdate]) {
                            if (item is IPauseUpdateGhostnetChat && item.Active) {
                                item.Update();
                            }
                        }
                    }
                    foreach (Entity item2 in self[Tags.PauseUpdate]) {
                        if (item2 is IPauseUpdateGhostnetChat && item2.Active) {
                            item2.Update();
                        }
                    }
                    MInput.Disabled = disabled;
                    if (self.Wipe != null) {
                        self.Wipe.Update(self);
                    }
                    if (self.HiresSnow != null) {
                        self.HiresSnow.Update(self);
                    }
                    self.Entities.UpdateLists();
                    FieldInfo updateHair = self.GetType().GetField("updateHair", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField);

                    foreach (Component component2 in self.Tracker.GetComponents<PlayerHair>()) {
                        if (component2.Active && component2.Entity.Active) {
                            (component2 as PlayerHair).AfterUpdate();
                        }
                    }
                    if (self.FrozenOrPaused) {
                        updateHair.SetValue(self, false);
                    }
                }
            };

            // Stuff that doesn't always run orig(self)
            /* ******************************************* */
            using (new DetourContext("MadelineParty") {
                After = { "*" }
            }) {

            }
        }

        private void GhostnetHideChat() {
            string input = GhostNetModule.Instance.Client.ChatInput;
            chatWasVisible = GhostNetModule.Instance.Client.ChatVisible;
            if (chatWasVisible) {
                GhostNetModule.Instance.Client.ChatVisible = false;
                GhostNetModule.Instance.Client.ChatInput = input;
            }
        }

        private void GhostnetShowChatIfHidden() {
            GhostNetModule.Instance.Client.ChatVisible |= chatWasVisible;
        }

        public static bool IsSIDMadelineParty(string sid) {
            return sid.Equals("Brokemia/MadelineParty/madelineparty");
        }

        void Player_Update(On.Celeste.Player.orig_Update orig, Player self) {
            Level l = self.SceneAs<Level>();
            orig(self);

            if (l != null && IsSIDMadelineParty(l.Session.Area.GetSID())) {
                if (ghostnetInstalled) {
                    GhostnetUpdate();
                }

                if (!l.Session.Level.Equals(START_ROOM) && (!l.Session.Level.Equals("Game_Lobby") || GameData.playerNumber == -1)) {
                    if (GameData.players.All((data) => data == null)) {
                        Player player = l.Tracker.GetEntity<Player>();
                        sendToStart(player);
                    } else {
                        foreach (uint id in GameData.ghostnetIDs) {
                            if (GhostNetModule.Instance?.Client?.PlayerMap[id]?.Level?.Equals(START_ROOM) ?? true) {
                                Player player = l.Tracker.GetEntity<Player>();
                                sendToStart(player);
                            }
                        }
                    }
                }

                if(l.Entities.AmountOf<MinigameFinishTrigger>() == 0) {
                    l.CanRetry = false;
                }
            }

        }

        private void sendToStart(Player p) {
            level.OnEndOfFrame += delegate {
                Leader.StoreStrawberries(p.Leader);
                level.Remove(p);
                level.UnloadLevel();

                level.Session.Level = START_ROOM;
                level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(level.Bounds.Left, level.Bounds.Top));
                level.LoadLevel(Player.IntroTypes.None);

                Leader.RestoreStrawberries(level.Tracker.GetEntity<Player>().Leader);
            };
        }

        private void GhostnetUpdate() {
            if (ghostnetConnected && !connectionSetup) {
                GhostnetConnectionSetup();
            } else if (!ghostnetConnected && connectionSetup) {
                connectionSetup = false;
            }

            // If the player disconnects from a multiplayer game
            if (GameData.playerNumber > 1 && !ghostnetConnected) {
                Player player = level.Tracker.GetEntity<Player>();
                sendToStart(player);
            }
        }

        bool Level_OnLoadEntity(Celeste.Level l, Celeste.LevelData levelData, Vector2 offset, Celeste.EntityData entityData) {
            if (entityData.Name.StartsWith("madelineparty/", StringComparison.InvariantCulture)) {
                switch (entityData.Name.Substring("madelineparty/".Length)) {
                    case "leftbutton":
                        l.Add(new LeftButton(entityData, offset));
                        return true;
                    case "rightbutton":
                        l.Add(new RightButton(entityData, offset));
                        return true;
                    case "boardcontroller":
                        l.Add(new BoardController(entityData));
                        return true;
                    case "playernumberselect":
                        l.Add(new PlayerNumberSelect(entityData, offset));
                        return true;
                    case "playerselecttrigger":
                        l.Add(new PlayerSelectTrigger(entityData, offset));
                        return true;
                    case "gamescoreboard":
                        l.Add(new GameScoreboard(entityData, offset));
                        return true;
                    case "minigamefinishtrigger":
                        l.Add(new MinigameFinishTrigger(entityData, offset));
                        return true;
                    case "playerrankingcontroller":
                        l.Add(new PlayerRankingController());
                        return true;
                    case "gameendcontroller":
                        l.Add(new GameEndController());
                        return true;
                    case "theominigamecontroller":
                        l.Add(new MinigameTheoMover(entityData, offset));
                        return true;
                }
            }
            return false;
        }


        // Optional, initialize anything after Celeste has initialized itself properly.
        public override void Initialize() {
            // Set the shop prices
            GameData.itemPrices[GameData.Item.DOUBLEDICE] = 10;
        }

        // Optional, do anything requiring either the Celeste or mod content here.
        [Obsolete]
        public override void LoadContent() { // TODO Do a better check
            ghostnetInstalled |= Everest.Modules.Any((mod) => mod.GetType().Name.Equals("GhostNetModule"));
        }

        public void GhostnetConnectionSetup() {
            if (ghostnetConnected && !connectionSetup) {
                Logger.Log("MadelineParty", "Setting up Ghostnet Connection");
                GhostNetModule.Instance.Client.OnHandle += (GhostNetConnection connection, GhostNetFrame frame) => {
                    if (frame.Has<ChunkMEmote>()) {
                        List<GhostNetEmote> remove = new List<GhostNetEmote>();
                        lock (level.Entities) {
                            foreach (GhostNetEmote e in level.Entities.FindAll<GhostNetEmote>()) {
                                if (e.Value.StartsWith(emotePrefix, StringComparison.InvariantCulture)) {
                                    remove.Add(e);
                                }
                            }
                            level.Entities.Remove(remove);
                        }

                        if (frame.Get<ChunkMEmote>().Value.StartsWith(emotePrefix, StringComparison.InvariantCulture)) {
                            frame.HHead.PlayerID = uint.MaxValue;
                        }
                    }
                };
                
                GhostNetModule.Instance.Client.Connection.OnReceiveManagement += (GhostNetConnection connection, System.Net.IPEndPoint arg2, GhostNetFrame frame) => {
                    // Remove the emote if it was handled by madeline party
                    if (HandleManagement(connection, arg2, frame)) {
                        frame.Remove<ChunkMEmote>();
                    }
                };
                connectionSetup = true;
            }
        }

        // Returns true if a madeline party emote was successfully found
        bool HandleManagement(GhostNetConnection connection, System.Net.IPEndPoint endPoint, GhostNetFrame frame) {
            if (!IsSIDMadelineParty(level.Session.Area.GetSID())) return false;
            ChunkMEmote emoteChunk = frame.Get<ChunkMEmote>();
            if (emoteChunk == null) return false;
            Logger.Log("MadelineParty", "Emote found: " + emoteChunk.Value);

            MadelinePartyChunk chunk = EmoteConverter.convertEmoteValueToChunk(emoteChunk.Value);
            if (chunk != null) {
                Logger.Log("MadelineParty", "Emote interpreted as MadelinePartyChunk. Player ID: " + chunk.playerID + " Looking for party of size " + chunk.lookingForParty);
                // Check if they want the same party size, we aren't full up, they aren't in our party, and they aren't us
                if (chunk.lookingForParty == GameData.playerNumber && chunk.version.Equals(Metadata.VersionString) && GameData.ghostnetIDs.Count < GameData.playerNumber - 1 && !GameData.ghostnetIDs.Contains(chunk.playerID) && chunk.playerID != GhostNetModule.Instance.Client.PlayerID) {
                    // If they think they're the host and are broadcasting
                    if (chunk.playerID == chunk.respondingTo && chunk.partyHost) {
                        Celeste.Mod.Ghost.Net.GhostNetModule.Instance.Client.Connection.SendManagement(new Celeste.Mod.Ghost.Net.GhostNetFrame
                        {
                                    EmoteConverter.convertPartyChunkToEmoteChunk(new MadelinePartyChunk
                                    {
                                        playerID = Celeste.Mod.Ghost.Net.GhostNetModule.Instance.Client.PlayerID,
                                        playerName = GhostNetModule.Instance.Client.PlayerName.Name,
                                        respondingTo = chunk.playerID,
                                        lookingForParty = (byte)GameData.playerNumber,
                                        partyHost = GameData.gnetHost
                                    })
                                }, true);
                        GameData.ghostnetIDs.Add(chunk.playerID);
                        Logger.Log("MadelineParty", chunk.playerName + "#" + chunk.playerID + " has joined the party!");
                        GhostNetClient.ChatLine line = new GhostNetClient.ChatLine(uint.MaxValue, GhostNetModule.Instance.Client.PlayerID, "**SERVER**", "", chunk.playerName + "#" + chunk.playerID + " has joined the party!", Color.Green);
                        Celeste.Mod.Ghost.Net.GhostNetModule.Instance.Client.ChatLog.Insert(0, line);
                        if (GameData.currentPlayerSelection != null) {
                            Celeste.Mod.Ghost.Net.GhostNetModule.Instance.Client.Connection.SendManagement(new Celeste.Mod.Ghost.Net.GhostNetFrame
                            {
                                        EmoteConverter.convertPartyChunkToEmoteChunk(new MadelinePartyChunk
                                        {
                                            playerID = Celeste.Mod.Ghost.Net.GhostNetModule.Instance.Client.PlayerID,
                                            playerName = Celeste.Mod.Ghost.Net.GhostNetModule.Instance.Client.PlayerName.Name,
                                            respondingTo = chunk.playerID,
                                            playerSelectTrigger = GameData.currentPlayerSelection.playerID
                                        })
                                    }, true);
                        }
                    } else if (chunk.respondingTo == Celeste.Mod.Ghost.Net.GhostNetModule.Instance.Client.PlayerID) {
                        GameData.gnetHost = false;
                        GameData.ghostnetIDs.Add(chunk.playerID);
                        Logger.Log("MadelineParty", chunk.playerName + "#" + chunk.playerID + " has joined the party!");
                        GhostNetClient.ChatLine line = new GhostNetClient.ChatLine(uint.MaxValue, GhostNetModule.Instance.Client.PlayerID, "**SERVER**", "", chunk.playerName + "#" + chunk.playerID + " has joined the party!", Color.Green);
                        Celeste.Mod.Ghost.Net.GhostNetModule.Instance.Client.ChatLog.Insert(0, line);
                    }
                }

                // If the other player entered a player select trigger
                if (chunk.playerSelectTrigger != -2 && GameData.ghostnetIDs.Contains(chunk.playerID) && (chunk.respondingTo == chunk.playerID || chunk.respondingTo == GhostNetModule.Instance.Client.PlayerID)) {
                    //if (chunk.playerSelectTrigger == -1)
                    //{
                    //    GameData.playerSelectTriggers.Remove(chunk.playerID);
                    //}
                    //else
                    //{
                    GameData.playerSelectTriggers[chunk.playerID] = chunk.playerSelectTrigger;
                    if (GameData.currentPlayerSelection != null) {
                        // -1 so it doesn't count me as a player
                        int left = GameData.playerNumber - 1;
                        foreach (KeyValuePair<uint, int> kvp1 in GameData.playerSelectTriggers) {
                            // Check if another player is trying to choose the same spot
                            bool duplicate = false;
                            foreach (KeyValuePair<uint, int> kvp2 in GameData.playerSelectTriggers) {
                                duplicate |= (kvp2.Key != kvp1.Key && kvp2.Value == kvp1.Value);
                            }
                            if (!duplicate && kvp1.Value != -1 && kvp1.Value != GameData.currentPlayerSelection.playerID) {
                                left--;
                            }
                        }

                        if (left <= 0) {
                            GameData.currentPlayerSelection.AllTriggersOccupied();
                        }
                    }
                    //}
                }

                return true;
            }

            PlayerChoiceData choiceData = EmoteConverter.convertEmoteValueToPlayerChoice(emoteChunk.Value);
            // If another player in our party has made a choice
            if (choiceData != null && GameData.ghostnetIDs.Contains(choiceData.playerID) && choiceData.playerID != GhostNetModule.Instance.Client.PlayerID) {
                Logger.Log("MadelineParty", "Choice detected of type " + choiceData.choiceType + " with value " + choiceData.choice);
                switch (choiceData.choiceType) {
                    case PlayerChoiceData.ChoiceType.HEART:
                        if (choiceData.choice == 0) {
                            BoardController.Instance.BuyHeart();
                        } else {
                            BoardController.Instance.SkipHeart();
                        }
                        break;
                    case PlayerChoiceData.ChoiceType.ENTERSHOP:
                        if (choiceData.choice == 0) {
                            BoardController.Instance.EnterShop();
                        } else {
                            BoardController.Instance.SkipShop();
                        }
                        break;
                    case PlayerChoiceData.ChoiceType.SHOPITEM:
                        if (choiceData.choice == 0) {
                            BoardController.Instance.BuyItem();
                        } else {
                            BoardController.Instance.SkipItem();
                        }
                        break;
                    case PlayerChoiceData.ChoiceType.DIRECTION:
                        BoardController.Instance.ContinueMovementAfterIntersection((BoardController.Direction)choiceData.choice);
                        break;
                    case PlayerChoiceData.ChoiceType.HEARTX:
                        GameData.heartSpace.X = choiceData.choice;
                        break;
                    case PlayerChoiceData.ChoiceType.HEARTY:
                        GameData.heartSpace.Y = choiceData.choice;
                        break;
                    default:
                        Logger.Log("MadelineParty", "Unhandled choice (" + choiceData.choiceType + ") from " + choiceData.playerName + "#" + choiceData.playerID);
                        break;
                }

                return true;
            }

            MinigameStartData startData = EmoteConverter.convertEmoteValueToMinigameStart(emoteChunk.Value);
            // If we've received information about a minigame starting from another player in our party
            if (startData != null && GameData.ghostnetIDs.Contains(startData.playerID) && startData.playerID != GhostNetModule.Instance.Client.PlayerID) {
                BoardController.Instance.ChoseMinigame(startData.choice, startData.gameStart);

                return true;
            }

            MinigameEndData endData = EmoteConverter.convertEmoteValueToMinigameEnd(emoteChunk.Value);
            // If another player in our party has beaten a minigame
            if (endData != null && GameData.ghostnetIDs.Contains(endData.playerID) && endData.playerID != GhostNetModule.Instance.Client.PlayerID) {
                GameData.minigameResults.Add(new Tuple<int, uint>(GameData.playerSelectTriggers[endData.playerID], endData.results));
                Logger.Log("MadelineParty", "Player " + endData.playerName + "#" + endData.playerID + " has finished the minigame with a result of " + endData.results);

                return true;
            }

            DieRollData dieRollData = EmoteConverter.convertEmoteValueToDieRoll(emoteChunk.Value);
            // If another player in our party has rolled the dice and we're waiting on them for an action
            if (dieRollData != null && GameData.ghostnetIDs.Contains(dieRollData.playerID) && dieRollData.playerID != GhostNetModule.Instance.Client.PlayerID) {

                if (!level.Session.Level.Equals(MAIN_ROOM)) {
                    // Activate it once in the right room
                    // This is so players that roll before everyone shows up don't break everything
                    BoardController.delayedDieRoll = dieRollData;
                } else {
                    if (BoardController.Instance.isWaitingOnPlayer(GameData.playerSelectTriggers[dieRollData.playerID])) {
                        string rollString = "";
                        foreach (int i in dieRollData.rolls) {
                            rollString += i + ", ";
                        }
                        Logger.Log("MadelineParty", "Emote interpreted as die roll from player " + dieRollData.playerID + ". Rolls: " + rollString);

                        if (dieRollData.rolls.Length == 2)
                            GameData.players[GameData.playerSelectTriggers[dieRollData.playerID]].items.Remove(GameData.Item.DOUBLEDICE);
                        BoardController.Instance.RollDice(GameData.playerSelectTriggers[dieRollData.playerID], dieRollData.rolls);
                    }
                }

                return true;
            }

            RandomSeedData randomSeedData = EmoteConverter.convertEmoteValueToRandomSeed(emoteChunk.Value);
            // If another player in our party is distributing the randomization seeds
            if (randomSeedData != null && GameData.ghostnetIDs.Contains(randomSeedData.playerID) && randomSeedData.playerID != GhostNetModule.Instance.Client.PlayerID) {
                GameData.turnOrderSeed = randomSeedData.turnOrderSeed;
                GameData.tieBreakerSeed = randomSeedData.tieBreakerSeed;
                BoardController.generateTurnOrderRolls();

                return true;
            }
            return false;
        }

        // Unload the entirety of your mod's content, remove any event listeners and undo all hooks.
        public override void Unload() {
            Everest.Events.Level.OnLoadEntity -= Level_OnLoadEntity;
            On.Celeste.Player.Update -= Player_Update;
        }

    }
}