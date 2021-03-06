﻿using System;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod.Ghost.Net;
using MadelineParty.Ghostnet;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty {
    public class PlayerSelectTrigger : Trigger, IComparable, IPauseUpdateGhostnetChat {
        private Level level;
        private List<PlayerSelectTrigger> otherChoices = new List<PlayerSelectTrigger>();
        public bool occupied;
        public int playerID {
            private set;
            get;
        } = 0;

        public PlayerSelectTrigger(EntityData data, Vector2 offset)
            : base(data, offset) {
            AddTag(Tags.PauseUpdate);
            AddTag(Tags.FrozenUpdate);
        }

        private void GhostNetSendOnEnterExit(int status) {
            Celeste.Mod.Ghost.Net.GhostNetModule.Instance.Client.Connection.SendManagement(new Celeste.Mod.Ghost.Net.GhostNetFrame
                {
                    EmoteConverter.convertPartyChunkToEmoteChunk(new MadelinePartyChunk
                    {
                        playerID = Celeste.Mod.Ghost.Net.GhostNetModule.Instance.Client.PlayerID,
                        playerName = Celeste.Mod.Ghost.Net.GhostNetModule.Instance.Client.PlayerName.Name,
                        respondingTo = Celeste.Mod.Ghost.Net.GhostNetModule.Instance.Client.PlayerID,
                        playerSelectTrigger = status
                    })
                }, true);
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);
            occupied = true;
            GameData.currentPlayerSelection = this;
            if (MadelinePartyModule.ghostnetConnected) {
                GhostNetSendOnEnterExit(playerID);
            }

            // -1 so it doesn't count me as a player
            int left = GameData.playerNumber - 1;
            foreach (KeyValuePair<uint, int> kvp1 in GameData.playerSelectTriggers) {
                // Check if another player is trying to choose the same spot
                bool duplicate = false;
                foreach (KeyValuePair<uint, int> kvp2 in GameData.playerSelectTriggers) {
                    duplicate |= (kvp2.Key != kvp1.Key && kvp2.Value == kvp1.Value);
                }
                if (!duplicate && kvp1.Value != -1 && kvp1.Value != playerID) {
                    left--;
                }
            }

            if (left <= 0) {
                AllTriggersOccupied();
            }
        }

        private void GhostNetOccupiedAction() {
            GameData.players[playerID] = new PlayerData(playerID, GhostNetModule.Instance.Client.PlayerID);
            foreach (KeyValuePair<uint, int> pair in GameData.playerSelectTriggers) {
                if (pair.Value != playerID && pair.Value >= 0) {
                    GameData.players[pair.Value] = new PlayerData(pair.Value, pair.Key);
                }
            }
            // Host determines the random seeds for the game
            // Seeds are determined in advance to avoid duplicate rolls when it matters
            if (GameData.gnetHost) {
                GhostNetModule.Instance.Client.Connection.SendManagement(new GhostNetFrame
                {
                    EmoteConverter.convertRandomSeedToEmoteChunk(new RandomSeedData
                    {
                        playerID = GhostNetModule.Instance.Client.PlayerID,
                        playerName = GhostNetModule.Instance.Client.PlayerName.Name,
                        turnOrderSeed = GameData.turnOrderSeed,
                        tieBreakerSeed = GameData.tieBreakerSeed
                    })
                }, true);
            }
        }

        // Only called on the Trigger that the player at this computer has selected
        public void AllTriggersOccupied() {
            // Store playerID
            GameData.realPlayerID = playerID;
            Random rand = new Random();
            // If not the host, the seeds will be changed by a recieved communication
            GameData.turnOrderSeed = (uint)rand.Next(2, 100000);
            GameData.tieBreakerSeed = (uint)rand.Next(2, 100000);
            BoardController.generateTurnOrderRolls();
            if (MadelinePartyModule.ghostnetConnected) {
                GhostNetOccupiedAction();
            } else {
                GameData.players[playerID] = new PlayerData(playerID);
            }
            Player player = level.Tracker.GetEntity<Player>();
            level.OnEndOfFrame += delegate {
                GameData.currentPlayerSelection = null;
                Leader.StoreStrawberries(player.Leader);
                level.Remove(player);
                level.UnloadLevel();

                level.Session.Level = "Game_MainRoom";
                switch (playerID) {
                    case 0:
                        level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(level.Bounds.Left, level.Bounds.Top));
                        break;
                    case 1:
                        level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(level.Bounds.Right, level.Bounds.Top));
                        break;
                    case 2:
                        level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(level.Bounds.Left, level.Bounds.Bottom));
                        break;
                    case 3:
                        level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(level.Bounds.Right, level.Bounds.Bottom));
                        break;
                }
                level.LoadLevel(Player.IntroTypes.None);

                Leader.RestoreStrawberries(player.Leader);
            };
        }

        public override void OnLeave(Player player) {
            base.OnLeave(player);
            if (GameData.realPlayerID == -1) {
                occupied = false;
                if (MadelinePartyModule.ghostnetConnected) {
                    GhostNetSendOnEnterExit(-1);
                }
                GameData.currentPlayerSelection = null;
            }
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            level = SceneAs<Level>();
            level.CanRetry = false;

            otherChoices = scene.Entities.FindAll<PlayerSelectTrigger>();
            otherChoices.Sort();
            for (int i = 0; i < otherChoices.Count; i++) {
                if (otherChoices[i] == this) {
                    PlayerToken token = new PlayerToken(BoardController.TokenPaths[i], ScreenCoordsFromGameCoords(this.Position, new Vector2(this.Width, this.Height) * 3), new Vector2(.25f, .25f), -900000000, new Vector2(0, 0));
                    level.Add(token);
                    playerID = i;
                }
            }
            otherChoices.Remove(this);
        }

        private Vector2 ScreenCoordsFromGameCoords(Vector2 gameCoords) {
            return ScreenCoordsFromGameCoords(gameCoords, new Vector2(0, 0));
        }

        private Vector2 ScreenCoordsFromGameCoords(Vector2 gameCoords, Vector2 offsetInPxls) {
            return new Vector2((-level.LevelOffset.X + gameCoords.X) * 6 + offsetInPxls.X, (-level.LevelOffset.Y + gameCoords.Y) * 6 + offsetInPxls.Y);
        }

        public int CompareTo(object obj) {
            if (obj == null) return 1;
            return obj is PlayerSelectTrigger other ? X.CompareTo(other.X) : 1;
        }
    }

}
