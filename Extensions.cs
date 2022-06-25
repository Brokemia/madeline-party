using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

namespace MadelineParty {
    static class Extensions {

		public static T OrDefault<S, T>(this Dictionary<S, T> self, S key, T defaultValue) {
            if(self.TryGetValue(key, out T value)) {
                return value;
            }
            return defaultValue;
        }

        public static T OrDefault<S, T>(this Dictionary<S, object> self, S key, T defaultValue) {
            if (self.TryGetValue(key, out object value)) {
                return (T)value;
            }
            return defaultValue;
        }

        public static void Teleport(this Level self, string levelName, Vector2? spawnPoint = null) {
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
			if (spawnPoint == null) {
				spawnPoint = self.GetSpawnPoint(new Vector2(self.Bounds.Left, self.Bounds.Top));
			}
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

	}
}
