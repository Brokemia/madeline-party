using Microsoft.Xna.Framework;
using Celeste;
using System;
using System.Linq;
using Celeste.Mod;
using MadelineParty.Multiplayer;
using MadelineParty.Multiplayer.General;
using System.Collections.Generic;
using Monocle;

// TODO minigames for most bounces of oshiro, seekers, snowballs, etc...
// TODO survive the longest minigames

namespace MadelineParty {
    public class MadelinePartyModule : EverestModule {

        // Only one alive module instance can exist at any given time.
        public static MadelinePartyModule Instance;
        public static string START_ROOM = "Game_PlayerNumberSelect";
        public static string MAIN_ROOM = "Game_MainRoom";

        public Level level;

        public MadelinePartyModule() {
            Instance = this;
        }

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
            Everest.Events.Level.OnLoadLevel += (level, playerIntro, isFromLoader) => this.level = level;
            DreamBlockRNGSyncer.Load();
            MinigameInfinityTrigger.Load();
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
            On.Celeste.Level.UnloadLevel += (orig, self) => {
                if (IsSIDMadelineParty(self.Session.Area.GetSID())) {
                    TextMenu menu = self.Entities.FindFirst<TextMenu>();
                    if (menu != null) {
                        self.PauseMainMenuOpen = false;
                        menu.RemoveSelf();
                        self.Paused = false;
                    }
                }
                orig(self);
            };
            MinigameSwitchGatherer.Load();
            BoardController.Load();
            TiebreakerController.Load();
            PersistentMiniTextbox.Load();

            MultiplayerSingleton.Instance.RegisterHandler<Party>(HandleParty);
            MultiplayerSingleton.Instance.RegisterHandler<MinigameEnd>(HandleMinigameEnd);
            MultiplayerSingleton.Instance.RegisterHandler<MinigameStatus>(HandleMinigameStatus);
            MultiplayerSingleton.Instance.RegisterHandler<RandomSeed>(HandleRandomSeed);
        }

        public static bool IsSIDMadelineParty(string sid) {
            return sid.StartsWith("Brokemia/MadelineParty/madelineparty");
        }

        private float disconnectLeniency = 2f;

        void Player_Update(On.Celeste.Player.orig_Update orig, Player self) {
            Level l = self.SceneAs<Level>();
            orig(self);

            if (l != null && IsSIDMadelineParty(l.Session.Area.GetSID())) {
                if (MultiplayerSingleton.Instance.BackendInstalled()) {
                    // If the player disconnects from a multiplayer game
                    if (GameData.Instance.playerNumber > 1 && !MultiplayerSingleton.Instance.BackendConnected()) {
                        disconnectLeniency -= Engine.RawDeltaTime;
                        if (disconnectLeniency < 0) {
                            Player player = level.Tracker.GetEntity<Player>();
                            sendToStart(player);
                            disconnectLeniency = 2f;
                        }
                    } else {
                        disconnectLeniency = 2f;
                    }
                }

                if (!l.Session.Level.Equals(START_ROOM) && (!l.Session.Level.Equals("Game_Lobby") || GameData.Instance.playerNumber == -1)) {
                    if (GameData.Instance.players.All((data) => data == null)) {
                        Player player = l.Tracker.GetEntity<Player>();
                        sendToStart(player);
                    } else {
                        // FIXME Use new Multiplayer
                        //foreach (uint id in GameData.Instance.celestenetIDs) {
                        //    if (playerInStartRoom(id)) {
                        //        Player player = l.Tracker.GetEntity<Player>();
                        //        sendToStart(player);
                        //    }
                        //}
                    }
                }

                if(l.Entities.AmountOf<MinigameFinishTrigger>() == 0) {
                    l.CanRetry = false;
                }
            }

        }

        //private bool playerInStartRoom(uint id) {
        //    if (CelesteNetClientModule.Instance?.Client?.Data != null && CelesteNetClientModule.Instance.Client.Data.TryGetRef(id, out DataPlayerState state)) {
        //        Console.WriteLine(state + " " + state.Level);
        //        return state?.Level?.Equals(START_ROOM) ?? true;
        //    }
        //    return false;
        //}

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
            GameData.Instance.itemPrices[GameData.Item.DOUBLEDICE] = 10;
        }

        // Optional, do anything requiring either the Celeste or mod content here.
        [Obsolete]
        public override void LoadContent() {
            MultiplayerSingleton.Instance.LoadContent();
            BoardController.LoadContent();
        }

        // Unload the entirety of your mod's content, remove any event listeners and undo all hooks.
        public override void Unload() {
            Everest.Events.Level.OnLoadEntity -= Level_OnLoadEntity;
            On.Celeste.Player.Update -= Player_Update;
            MinigameSwitchGatherer.Unload();
            PersistentMiniTextbox.Unload();
        }

        private void HandleParty(MPData data) {
            if(data is not Party party) return;
            if (!IsSIDMadelineParty(level.Session.Area.GetSID())) return;
            Logger.Log("MadelineParty", "Recieved PartyData. My ID: " + MultiplayerSingleton.Instance.GetPlayerID() + " Player ID: " + party.ID + " Looking for party of size " + party.lookingForParty);
            if (party.lookingForParty == GameData.Instance.playerNumber // if they want the same party size
                && party.version.Equals(Metadata.VersionString) // and our versions match
                && GameData.Instance.celestenetIDs.Count < GameData.Instance.playerNumber - 1 // and we aren't full up
                && !GameData.Instance.celestenetIDs.Contains(party.ID) // and they aren't in our party
                && party.ID != MultiplayerSingleton.Instance.GetPlayerID()) { // and they aren't us

                string joinMsg = party.DisplayName + " has joined the party!";
                // If they think they're the host and are broadcasting
                if (party.respondingTo < 0 && party.partyHost) {
                    // Tell them that they aren't the host and are instead joining our party
                    MultiplayerSingleton.Instance.Send(new Party {
                        respondingTo = (int)party.ID,
                        lookingForParty = (byte)GameData.Instance.playerNumber,
                        partyHost = GameData.Instance.gnetHost
                    });

                    GameData.Instance.celestenetIDs.Add(party.ID);

                    
                    Logger.Log("MadelineParty", joinMsg);
                    MultiplayerSingleton.Instance.SendChat(joinMsg);

                    if (GameData.Instance.currentPlayerSelection != null) {
                        MultiplayerSingleton.Instance.Send(new Party {
                            respondingTo = (int)party.ID,
                            playerSelectTrigger = GameData.Instance.currentPlayerSelection.playerID
                        });
                    }
                } else if (party.respondingTo == MultiplayerSingleton.Instance.GetPlayerID()) {
                    GameData.Instance.gnetHost = false;
                    GameData.Instance.celestenetIDs.Add(party.ID);

                    Logger.Log("MadelineParty", joinMsg);
                    MultiplayerSingleton.Instance.SendChat(joinMsg);
                }
            }

            // If the other player entered a player select trigger
            if (party.playerSelectTrigger != -2 && GameData.Instance.celestenetIDs.Contains(party.ID) && (party.respondingTo < 0 || party.respondingTo == MultiplayerSingleton.Instance.GetPlayerID())) {
                Logger.Log("MadelineParty", "Player ID: " + party.ID + " entered player select trigger " + party.playerSelectTrigger);
                GameData.Instance.playerSelectTriggers[party.ID] = party.playerSelectTrigger;
                if (GameData.Instance.currentPlayerSelection != null) {
                    // -1 so it doesn't count me as a player
                    int left = GameData.Instance.playerNumber - 1;
                    foreach (KeyValuePair<uint, int> kvp1 in GameData.Instance.playerSelectTriggers) {
                        // Check if another player is trying to choose the same spot
                        bool duplicate = false;
                        foreach (KeyValuePair<uint, int> kvp2 in GameData.Instance.playerSelectTriggers) {
                            duplicate |= (kvp2.Key != kvp1.Key && kvp2.Value == kvp1.Value);
                        }
                        if (!duplicate && kvp1.Value != -1 && kvp1.Value != GameData.Instance.currentPlayerSelection.playerID) {
                            left--;
                        }
                    }

                    if (left <= 0) {
                        GameData.Instance.currentPlayerSelection.AllTriggersOccupied();
                    }
                }
            }
        }

        private void HandleMinigameEnd(MPData data) {
            if (data is not MinigameEnd end) return;
            // If another player in our party has beaten a minigame
            if (GameData.Instance.celestenetIDs.Contains(end.ID) && end.ID != MultiplayerSingleton.Instance.GetPlayerID()) {
                GameData.Instance.minigameResults.Add(new Tuple<int, uint>(GameData.Instance.playerSelectTriggers[end.ID], end.results));
                Logger.Log("MadelineParty", "Player " + end.DisplayName + " has finished the minigame with a result of " + end.results);
            }
        }

        private void HandleMinigameStatus(MPData data) {
            if (data is not MinigameStatus status) return;
            // If another player in our party is sending out a minigame status update
            if (GameData.Instance.celestenetIDs.Contains(status.ID) && status.ID != MultiplayerSingleton.Instance.GetPlayerID()) {
                GameData.Instance.minigameStatus[GameData.Instance.playerSelectTriggers[status.ID]] = status.results;
                Logger.Log("MadelineParty", "Player " + status.DisplayName + " has updated their minigame status with a result of " + status.results);
            }
        }

        private void HandleRandomSeed(MPData data) {
            if (data is not RandomSeed seed) return;
            // If another player in our party is distributing the randomization seeds
            if (GameData.Instance.celestenetIDs.Contains(seed.ID) && seed.ID != MultiplayerSingleton.Instance.GetPlayerID()) {
                GameData.Instance.turnOrderSeed = seed.turnOrderSeed;
                GameData.Instance.tieBreakerSeed = seed.tieBreakerSeed;
                BoardController.generateTurnOrderRolls();
            }
        }

    }
}