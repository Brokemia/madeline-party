using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace MadelineParty {
	public abstract class MinigameDisplay : Entity {
		public float CompleteTimer;

		public const int GuiChapterHeight = 58;

		public const int GuiFileHeight = 78;

		private static float numberWidth;

		private static float spacerWidth;

		protected MTexture bg = GFX.Gui["strawberryCountBG"];

		protected MTexture timerBg = GFX.Gui["madelineparty/timerBG"];

		public float DrawLerp;

		protected Wiggler wiggler;

		protected MinigameEntity minigame;

		public float finalTime = -1;

		public MinigameDisplay(MinigameEntity minigame) {
			Tag = Tags.HUD | Tags.Global | Tags.PauseUpdate | Tags.TransitionUpdate;
			Depth = -100;
			Y = 60f;
			CalculateBaseSizes();
			Add(wiggler = Wiggler.Create(0.5f, 4f));
			this.minigame = minigame;
		}

		public static void CalculateBaseSizes() {
			PixelFont font = Dialog.Languages["english"].Font;
			float fontFaceSize = Dialog.Languages["english"].FontFaceSize;
			PixelFontSize pixelFontSize = font.Get(fontFaceSize);
			for (int i = 0; i < 10; i++) {
				float x = pixelFontSize.Measure(i.ToString()).X;
				if (x > numberWidth) {
					numberWidth = x;
				}
			}
			spacerWidth = pixelFontSize.Measure('.').X;
		}

		public override void Update() {
			if (minigame.completed) {
				if (CompleteTimer == 0f) {
					wiggler.Start();
				}
				CompleteTimer += Engine.DeltaTime;
			}
			DrawLerp = Calc.Approach(DrawLerp, 1, Engine.DeltaTime * 4f);
			base.Update();
		}

		public override void Render() {
			if (DrawLerp > 0f) {
				float y = -56f * Ease.CubeIn(1f - DrawLerp);
				Level level = Scene as Level;
				Session session = level.Session;

				TimeSpan timeSpan2 = TimeSpan.FromTicks((long)((finalTime > 0 ? finalTime : level.RawTimeActive - MinigameEntity.startTime) * 10000000));
				string timeString = timeSpan2.ToString("mm\\:ss\\.fff");
				timerBg.Draw(new Vector2(816, y));
				DrawTime(new Vector2(816 + 16f, y + 52f), timeString, 1f + wiggler.Value * 0.15f, session.StartedFromBeginning, level.Completed, session.BeatBestTime);
			}
		}

		public static void DrawTime(Vector2 position, string timeString, float scale = 1f, bool valid = true, bool finished = false, bool bestTime = false, float alpha = 1f) {
			PixelFont font = Dialog.Languages["english"].Font;
			float fontFaceSize = Dialog.Languages["english"].FontFaceSize;
			float num = scale;
			float num2 = position.X;
			float num3 = position.Y;
			Color color = Color.White * alpha;
			Color color2 = Color.LightGray * alpha;
			if (!valid) {
				color = Calc.HexToColor("918988") * alpha;
				color2 = Calc.HexToColor("7a6f6d") * alpha;
			} else if (bestTime) {
				color = Calc.HexToColor("fad768") * alpha;
				color2 = Calc.HexToColor("cfa727") * alpha;
			} else if (finished) {
				color = Calc.HexToColor("6ded87") * alpha;
				color2 = Calc.HexToColor("43d14c") * alpha;
			}
			for (int i = 0; i < timeString.Length; i++) {
				char c = timeString[i];
				if (c == '.') {
					num = scale * 0.7f;
					num3 -= 5f * scale;
				}
				Color color3 = (c == ':' || c == '.' || num < scale) ? color2 : color;
				float num4 = (((c == ':' || c == '.') ? spacerWidth : numberWidth) + 4f) * num;
				font.DrawOutline(fontFaceSize, c.ToString(), new Vector2(num2 + num4 / 2f, num3), new Vector2(0.5f, 1f), Vector2.One * num, color3, 2f, Color.Black);
				num2 += num4;
			}
		}

		public static float GetTimeWidth(string timeString, float scale = 1f) {
			float num = scale;
			float num2 = 0f;
			foreach (char c in timeString) {
				if (c == '.') {
					num = scale * 0.7f;
				}
				float num3 = (((c == ':' || c == '.') ? spacerWidth : numberWidth) + 4f) * num;
				num2 += num3;
			}
			return num2;
		}
	}
}
