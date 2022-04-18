using System;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.Client;
using Celeste.Mod.CelesteNet.DataTypes;
using Logger = Celeste.Mod.Logger;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty.CelesteNet {
    public class CelesteNetMadelinePartyComponent : CelesteNetGameComponent {
        public CelesteNetMadelinePartyComponent(CelesteNetClientContext context, Game game) : base(context, game) {
            Visible = false;
        }

        public void Handle(CelesteNetConnection con, PartyData data) {
            if (!MadelinePartyModule.IsSIDMadelineParty(MadelinePartyModule.Instance.level.Session.Area.GetSID())) return;
            Logger.Log("MadelineParty", "Recieved PartyData. My ID: " + Client.PlayerInfo.ID + " Player ID: " + data.Player.ID + " Looking for party of size " + data.lookingForParty);
            // Check if they want the same party size, our versions match, we aren't full up, they aren't in our party, and they aren't us
            if (data.lookingForParty == GameData.playerNumber && data.version.Equals(MadelinePartyModule.Instance.Metadata.VersionString) && GameData.celestenetIDs.Count < GameData.playerNumber - 1 && !GameData.celestenetIDs.Contains(data.Player.ID) && data.Player.ID != Client.PlayerInfo.ID) {
                // If they think they're the host and are broadcasting
                if (data.Player.ID == data.respondingTo && data.partyHost) {
                    Client.Send(new PartyData {
                        Player = CelesteNetClientModule.Instance?.Client?.PlayerInfo,
                        respondingTo = data.Player.ID,
                        lookingForParty = (byte)GameData.playerNumber,
                        partyHost = GameData.gnetHost
                    });
                    GameData.celestenetIDs.Add(data.Player.ID);

                    string joinMsg = data.Player.DisplayName + " has joined the party!";
                    Logger.Log("MadelineParty", joinMsg);
                    DataChat chat = new DataChat {
                        Text = joinMsg
                    };
                    Context.Chat.Log.Add(chat);
                    Context.Chat.LogSpecial.Add(chat);
                    if (GameData.currentPlayerSelection != null) {
                        Client.Send(new PartyData {
                            Player = CelesteNetClientModule.Instance?.Client?.PlayerInfo,
                            respondingTo = data.Player.ID,
                            playerSelectTrigger = GameData.currentPlayerSelection.playerID
                        });
                    }
                } else if (data.respondingTo == Client.PlayerInfo.ID) {
                    GameData.gnetHost = false;
                    GameData.celestenetIDs.Add(data.Player.ID);
                    string joinMsg = data.Player.DisplayName + " has joined the party!";
                    Logger.Log("MadelineParty", joinMsg);
                    DataChat chat = new DataChat {
                        Text = joinMsg
                    };
                    Context.Chat.Log.Add(chat);
                    Context.Chat.LogSpecial.Add(chat);
                }
            }

            // If the other player entered a player select trigger
            if (data.playerSelectTrigger != -2 && GameData.celestenetIDs.Contains(data.Player.ID) && (data.respondingTo == data.Player.ID || data.respondingTo == Client.PlayerInfo.ID)) {
                //if (chunk.playerSelectTrigger == -1)
                //{
                //    GameData.playerSelectTriggers.Remove(chunk.playerID);
                //}
                //else
                //{
                GameData.playerSelectTriggers[data.Player.ID] = data.playerSelectTrigger;
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
        }
    
        public void Handle(CelesteNetConnection con, DieRollData data) {
            // If another player in our party has rolled the dice and we're waiting on them for an action
            if (GameData.celestenetIDs.Contains(data.Player.ID) && data.Player.ID != Client.PlayerInfo.ID) {

                if (!MadelinePartyModule.Instance.level.Session.Level.Equals(MadelinePartyModule.MAIN_ROOM)) {
                    // Activate it once in the right room
                    // This is so players that roll before everyone shows up don't break everything
                    BoardController.delayedDieRoll = data;
                } else {
                    if (BoardController.Instance.isWaitingOnPlayer(GameData.playerSelectTriggers[data.Player.ID])) {
                        string rollString = "";
                        foreach (int i in data.rolls) {
                            rollString += i + ", ";
                        }
                        Logger.Log("MadelineParty", "Received die roll from player " + data.Player.ID + ". Rolls: " + rollString);

                        if (data.rolls.Length == 2)
                            GameData.players[GameData.playerSelectTriggers[data.Player.ID]].items.Remove(GameData.Item.DOUBLEDICE);
                        BoardController.Instance.RollDice(GameData.playerSelectTriggers[data.Player.ID], data.rolls);
                    }
                }
            }
        }

        public void Handle(CelesteNetConnection con, PlayerChoiceData data) {
            // If another player in our party has made a choice
            if (GameData.celestenetIDs.Contains(data.Player.ID) && data.Player.ID != Client.PlayerInfo.ID) {
                Logger.Log("MadelineParty", "Choice detected of type " + data.choiceType + " with value " + data.choice);
                switch (data.choiceType) {
                    case PlayerChoiceData.ChoiceType.HEART:
                        if (data.choice == 0) {
                            BoardController.Instance.BuyHeart();
                        } else {
                            BoardController.Instance.SkipHeart();
                        }
                        break;
                    case PlayerChoiceData.ChoiceType.ENTERSHOP:
                        if (data.choice == 0) {
                            BoardController.Instance.EnterShop();
                        } else {
                            BoardController.Instance.SkipShop();
                        }
                        break;
                    case PlayerChoiceData.ChoiceType.SHOPITEM:
                        if (data.choice == 0) {
                            BoardController.Instance.BuyItem();
                        } else {
                            BoardController.Instance.SkipItem();
                        }
                        break;
                    case PlayerChoiceData.ChoiceType.DIRECTION:
                        BoardController.Instance.ContinueMovementAfterIntersection((BoardController.Direction)data.choice);
                        break;
                    case PlayerChoiceData.ChoiceType.HEARTSPACEID:
                        GameData.heartSpaceID = data.choice;
                        break;
                    default:
                        Logger.Log("MadelineParty", "Unhandled choice (" + data.choiceType + ") from " + data.Player.FullName + "#" + data.Player.ID);
                        break;
                }

            }
        }

        public void Handle(CelesteNetConnection con, MinigameStartData data) {
            // If we've received information about a minigame starting from another player in our party
            if (GameData.celestenetIDs.Contains(data.Player.ID) && data.Player.ID != Client.PlayerInfo.ID) {
                BoardController.Instance.ChoseMinigame(data.choice, data.gameStart);
            }
        }

        public void Handle(CelesteNetConnection con, MinigameEndData data) {
            // If another player in our party has beaten a minigame
            if (GameData.celestenetIDs.Contains(data.Player.ID) && data.Player.ID != Client.PlayerInfo.ID) {
                GameData.minigameResults.Add(new Tuple<int, uint>(GameData.playerSelectTriggers[data.Player.ID], data.results));
                Logger.Log("MadelineParty", "Player " + data.Player.FullName + " has finished the minigame with a result of " + data.results);
            }
        }

        public void Handle(CelesteNetConnection con, MinigameStatusData data) {
            // If another player in our party is sending out a minigame status update
            if (GameData.celestenetIDs.Contains(data.Player.ID) && data.Player.ID != Client.PlayerInfo.ID) {
                GameData.minigameStatus[GameData.playerSelectTriggers[data.Player.ID]] = data.results;
                Logger.Log("MadelineParty", "Player " + data.Player.FullName + " has updated their minigame status with a result of " + data.results);
            }
        }

        public void Handle(CelesteNetConnection con, MinigameVector2Data data) {
            // If another player in our party is sending out minigame vector2 data
            if (GameData.celestenetIDs.Contains(data.Player.ID) && data.Player.ID != Client.PlayerInfo.ID) {
                MinigameEntity mge;
                if((mge = Engine.Scene?.Tracker.GetEntity<MinigameEntity>()) != null) {
                    mge.CelesteNetReceiveVector2(data.vec, data.extra);
                }
            }
        }

        public void Handle(CelesteNetConnection con, RandomSeedData data) {
            // If another player in our party is distributing the randomization seeds
            if (GameData.celestenetIDs.Contains(data.Player.ID) && data.Player.ID != Client.PlayerInfo.ID) {
                GameData.turnOrderSeed = data.turnOrderSeed;
                GameData.tieBreakerSeed = data.tieBreakerSeed;
                BoardController.generateTurnOrderRolls();
            }
        }
    }
}
