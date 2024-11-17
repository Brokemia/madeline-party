using Celeste;
using MadelineParty.Minigame;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MadelineParty {
    static class Extensions {

		public static void Teleport(this Level self, string levelName, Vector2? spawnPoint = null) {
            self.Teleport(levelName, () => spawnPoint);
        }

        public static void Teleport(this Level self, string levelName, Func<Vector2?> spawnPointFunc) {
			Glitch.Value = 0f;
			Engine.TimeRate = 1f;
			Distort.Anxiety = 0f;
			Distort.GameRate = 1f;
			Audio.SetMusicParam("fade", 1f);
			self.ParticlesBG.Clear();
			self.Particles.Clear();
			self.ParticlesFG.Clear();
			TrailManager.Clear();
			if(self.Tracker.GetEntity<Player>() is Player player) {
				self.Remove(player);
            }
			self.UnloadLevel();
			GC.Collect();
			GC.WaitForPendingFinalizers();
			self.Session.Level = levelName;
			Vector2 spawnPoint = spawnPointFunc.Invoke() ?? self.GetSpawnPoint(new Vector2(self.Bounds.Left, self.Bounds.Top));
            self.Session.RespawnPoint = spawnPoint;
			self.LoadLevel(Player.IntroTypes.None);
			self.strawberriesDisplay.DrawLerp = 0f;
			WindController windController = self.Entities.FindFirst<WindController>();
			if (windController != null) {
				windController.SnapWind();
			} else {
				self.Wind = Vector2.Zero;
			}
		}

		public static Vector2 GetMinigameSpawnPoint(this Level level, MinigamePersistentData data, int playerID) {
			// Get spawnpoints for the player's role
			var roles = data.GetRoles(playerID);
            List<Vector2> possibleSpawns = [];
			foreach (MinigameSpawnpoint spawn in level.Tracker.GetEntities<MinigameSpawnpoint>()) {
				if (spawn.Roles.Intersect(roles).Any()) {
					possibleSpawns.Add(spawn.Position);
				}
			}

			// Default back to using normal spawnpoints
			if (!possibleSpawns.Any()) {
                possibleSpawns = level.Session.LevelData.Spawns;
            }
			
			// Try to space out players. This is kind of janky for spawn points with roles
            return possibleSpawns[playerID % possibleSpawns.Count];
		}
	}
}
