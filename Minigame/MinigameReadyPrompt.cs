using Celeste;
using MadelineParty.Multiplayer;
using MadelineParty.Multiplayer.General;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadelineParty.Minigame {
    public class MinigameReadyPrompt : Entity {
        private const int width = 1000, height = 800;
        private const int borderWidth = 4;
        private const int nameTopPadding = 70;
        private const int readyVertPadding = 175;
        private const int readyChecksPadding = 60;
        private const int checkHorizPadding = 80;
        private const float buttonScale = 0.75f;
        private string name, tagline, description, readyText;
        private MTexture readyCheck, unreadyCheck;
        private Dictionary<int, bool> readyStatus = new();
        private Dictionary<int, PlayerToken> tokens = new();

        public MinigameReadyPrompt() {
            AddTag(TagsExt.SubHUD);
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            MultiplayerSingleton.Instance.RegisterUniqueHandler<MinigameReady>("ReadyPrompt", HandleReady);
            string lvl = SceneAs<Level>().Session.Level;
            name = Dialog.Clean("MadelineParty_Minigame_Name_" + lvl);
            tagline = Dialog.Clean("MadelineParty_Minigame_Tagline_" + lvl);
            description = Dialog.Clean("MadelineParty_Minigame_Description_" + lvl);
            readyText = Dialog.Clean("MadelineParty_Minigame_Ready");
            readyCheck = GFX.Gui["madelineparty/ready_checked"];
            unreadyCheck = GFX.Gui["madelineparty/ready_unchecked"];
            int i = 0;
            foreach (var p in GameData.Instance.players) {
                if (p != null) {
                    readyStatus[p.TokenSelected] = false;
                    if (p.TokenSelected != GameData.Instance.realPlayerID) {
                        AddToken(p.TokenSelected, i);
                        i++;
                    }
                }
            }
        }

        private void AddToken(int player, int index) {
            Scene.Add(tokens[player] = new PlayerToken(player, BoardController.TokenPaths[player],
                new(1920 / 2 /* center it */ - (readyCheck.Width * buttonScale / 2 + checkHorizPadding / 2) * (readyStatus.Count - 2) /* to left */ + (readyCheck.Width * buttonScale + checkHorizPadding) * index /* shift right */ + readyCheck.Height / 4, (1080 + height) / 2 - readyChecksPadding + readyCheck.Width / 4), 
                new Vector2(.3f), -900000000, new()));
        }

        //int added = 0;
        public override void Update() {
            base.Update();
            // Wait until the screen wipe is done to allow readying
            if (Input.MenuConfirm.Pressed && !readyStatus[GameData.Instance.realPlayerID] && SceneAs<Level>().Wipe is not ScreenWipe { Completed: false }) {
                readyStatus[GameData.Instance.realPlayerID] = true;
                MultiplayerSingleton.Instance.Send(new MinigameReady { player = GameData.Instance.realPlayerID });
                CheckReady();
            }
            //if (Input.MenuCancel.Released) {
            //    readyStatus[added] = false;
            //    added++;
            //    int i = 0;
            //    foreach (var kvp in readyStatus) {
            //        if (kvp.Key == GameData.Instance.realPlayerID) continue;
            //        if(tokens.ContainsKey(kvp.Key))
            //            tokens[kvp.Key].RemoveSelf();
            //        AddToken(kvp.Key > 3 ? 0 : kvp.Key, i);
            //        i++;
            //    }
            //}
        }

        private void CheckReady() {
            foreach(var ready in readyStatus.Values) {
                if (!ready) return;
            }

            RemoveSelf();
            Level level = SceneAs<Level>();
            MinigameEntity.startTime = level.RawTimeActive;
            Player player = level.Tracker.GetEntity<Player>();
            player.Die(Vector2.Zero, true, false);
            // FIXME change to just start MinigameEntity instead once hackfix can be removed
        }

        public override void Render() {
            string lvl = SceneAs<Level>().Session.Level;
            // TODO remove for performance maybe
            name = Dialog.Clean("MadelineParty_Minigame_Name_" + lvl);
            tagline = Dialog.Clean("MadelineParty_Minigame_Tagline_" + lvl);
            description = Dialog.Clean("MadelineParty_Minigame_Description_" + lvl);
            readyText = Dialog.Clean("MadelineParty_Minigame_Ready");
            base.Render();
            Draw.Rect(new(1920 / 2 - width / 2 - borderWidth, 1080 / 2 - height / 2 - borderWidth), width + borderWidth * 2, height + borderWidth * 2, Color.Black);
            Draw.Rect(new(1920 / 2 - width / 2, 1080 / 2 - height / 2), width, height, new Color(29, 18, 24));
            ActiveFont.DrawOutline(name, new(1920 / 2, 1080 / 2 - height / 2 + nameTopPadding), new(0.5f), new(1.5f), Color.White, 2, Color.Black);
            ActiveFont.DrawOutline(tagline, new(1920 / 2, 1080 / 2 - height / 2 + nameTopPadding * 2), new(0.5f), new(0.9f), Color.White, 2, Color.Black);
            ActiveFont.Draw(description, new(1920 / 2, 1080 / 2 - height / 2 + nameTopPadding * 3), new(0.5f, 0), new(0.75f), Color.White);

            ActiveFont.DrawOutline(readyText, new(1920 / 2 - (unreadyCheck.Width * buttonScale + 20) / 2, 1080 / 2 + height / 2 - readyVertPadding * (readyStatus.Count == 1 ? 0.5f : 1)), new(0.5f), Vector2.One, Color.White, 2, Color.Black);
            ((readyStatus.TryGetValue(GameData.Instance.realPlayerID, out bool ready) && ready) ? readyCheck : unreadyCheck).DrawCentered(new(1920 / 2 + (20 + ActiveFont.Measure(readyText).X) / 2, 1080 / 2 + height / 2 - readyVertPadding * (readyStatus.Count == 1 ? 0.5f : 1)), Color.White, buttonScale);
            int i = 0;
            foreach (var kvp in readyStatus) {
                if (kvp.Key == GameData.Instance.realPlayerID) continue;
                (kvp.Value ? readyCheck : unreadyCheck).DrawCentered(
                    new(1920 / 2 /* center it */ - (readyCheck.Width * buttonScale / 2 + checkHorizPadding / 2) * (readyStatus.Count - 2) /* to left */ + (readyCheck.Width * buttonScale + checkHorizPadding) * i /* shift right */, (1080 + height) / 2 - readyChecksPadding),
                    Color.White, buttonScale);
                i++;
            }
        }

        private void HandleReady(MPData data) {
            if (data is not MinigameReady ready) return;
            readyStatus[ready.player] = true;
            CheckReady();
        }
    }
}
