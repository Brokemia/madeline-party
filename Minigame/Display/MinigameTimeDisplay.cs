using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace MadelineParty {
    public class MinigameTimeDisplay : MinigameDisplay {
        public float CountdownTime { get; set; } = 30;

        private static float spacerWidth;

        private static float numberWidth;

        private bool countDown;

        public MinigameTimeDisplay(MinigameEntity minigame, bool countDown = false) : base(minigame) {
            this.countDown = countDown;
            CalculateBaseSizes();
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

        public override void Render() {
            base.Render();
            if(!countDown) {
                if (DrawLerp > 0f) {
                    float y = -56f * Ease.CubeIn(1f - DrawLerp);
                    Level level = Scene as Level;

                    TimeSpan timeSpan2 = TimeSpan.FromTicks((long)((finalTime > 0 ? finalTime : level.RawTimeActive - minigame.Data.StartTime) * 10000000));
                    string timeString = timeSpan2.ToString("mm\\:ss\\.fff");
                    timerBg.Draw(new Vector2(816, y));
                    DrawTime(new Vector2(816 + 16f, y + 52f), timeString, 1f + wiggler.Value * 0.15f, true, minigame.completed, false);
                }
            } else if (DrawLerp > 0f) {
                float timerY = -56f * Ease.CubeIn(1f - DrawLerp);
                Level level = Scene as Level;

                TimeSpan timeSpan = TimeSpan.FromTicks((long)(((minigame.completed || minigame.Data.StartTime < 0) ? 0 : CountdownTime - (level.RawTimeActive - minigame.Data.StartTime)) * 10000000));
                string timeString = timeSpan.ToString("ss\\.fff");
                timerBg.Draw(new Vector2(816, timerY));
                DrawTime(new Vector2(816 + 64f, timerY + 52f), timeString, 1f + wiggler.Value * 0.15f, true, minigame.completed, false);
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
