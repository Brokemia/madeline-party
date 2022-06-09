using System;
using System.Collections.Generic;
using Celeste;
using MadelineParty.Multiplayer;
using MadelineParty.Multiplayer.General;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty {
    public class PlayerSelectTrigger : Trigger, IComparable {
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

        public override void OnEnter(Player player) {
            base.OnEnter(player);
            occupied = true;
            GameData.currentPlayerSelection = this;
            MultiplayerSingleton.Instance.Send(new Party { respondingTo = -1, playerSelectTrigger = playerID });

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

        private void MultiplayerOccupiedAction() {
            GameData.players[playerID] = new PlayerData(playerID, MultiplayerSingleton.Instance.GetPlayerID());
            foreach (KeyValuePair<uint, int> pair in GameData.playerSelectTriggers) {
                if (pair.Value != playerID && pair.Value >= 0) {
                    GameData.players[pair.Value] = new PlayerData(pair.Value, pair.Key);
                }
            }
            // Host determines the random seeds for the game
            // Seeds are determined in advance to avoid duplicate rolls when it matters
            if (GameData.gnetHost) {
                MultiplayerSingleton.Instance.Send(new RandomSeed { turnOrderSeed = GameData.turnOrderSeed, tieBreakerSeed = GameData.tieBreakerSeed });
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
            if (MultiplayerSingleton.Instance.BackendConnected()) {
                MultiplayerOccupiedAction();
            } else {
                GameData.players[playerID] = new PlayerData(playerID);
            }
            Player player = level.Tracker.GetEntity<Player>();
            level.OnEndOfFrame += delegate {
                GameData.currentPlayerSelection = null;
                Leader.StoreStrawberries(player.Leader);
                level.Remove(player);
                level.UnloadLevel();

                level.Session.Level = "Game_SettingsConfig";
                if(GameData.gnetHost) {
                    level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(level.Bounds.Left, level.Bounds.Top));
                } else {
                    level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(level.Bounds.Left, level.Bounds.Bottom));
                }

                level.LoadLevel(Player.IntroTypes.None);

                Leader.RestoreStrawberries(player.Leader);
            };
        }

        public override void OnLeave(Player player) {
            base.OnLeave(player);
            if (GameData.realPlayerID == -1) {
                occupied = false;
                MultiplayerSingleton.Instance.Send(new Party { respondingTo = -1, playerSelectTrigger = -1 });
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
                    PlayerToken token = new PlayerToken(i, BoardController.TokenPaths[i], ScreenCoordsFromGameCoords(Position, new Vector2(Width, Height) * 3), new Vector2(.25f, .25f), -900000000, new BoardController.BoardSpace());
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
