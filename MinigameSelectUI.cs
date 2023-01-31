using Microsoft.Xna.Framework;
using Monocle;
using System;
using Celeste;
using System.Collections.Generic;
using BrokemiaHelper;
using System.Xml.Linq;

namespace MadelineParty {
    [Tracked(false)]
    public class MinigameSelectUI : Entity {
        private const int minWidth = 650;
        private const int padding = 10;
        private const int borderWidth = 4;
        private const float wiggleAmount = 10;
        private const int countdownAmount = 5;
        private const int gap = 20;

        const int totalOptions = 4;

        private int width, height;
        private string[] options;
        private string[] optionsLocalized;
        private string statusText;
        private string skipPrompt;
        private string skippedText;
        private int selected = -1;
        private int actualSelection;
        private float lastSwitchDelay = 0.04f;
        private float switchDelay = 0.3f;
        private readonly Wiggler selectedWiggler = Wiggler.Create(1f, 4f);
        private int countdownNumber;
        private float countdownTimer = float.MaxValue;
        private bool settled;

        private int skipItemIdx;
        private int skippedBy = -1;

        public Action<string> OnSelect;

        private Level level;
        private Random rand;
        private List<string> unchoosable = new();
        private string startChoice;

        public MinigameSelectUI(string choice) {
            startChoice = choice;
            rand = new(choice.GetHashCode() + GameData.Instance.turn + (int)(GameData.Instance.tieBreakerSeed - int.MaxValue));
            unchoosable.Add(choice);
            AddTag(TagsExt.SubHUD);
            Depth = -10000;
            statusText = Dialog.Clean("MadelineParty_Minigame_Select_Choosing");
            skipPrompt = Dialog.Clean("MadelineParty_Minigame_Select_Skip_Prompt");
            skipItemIdx = GameData.Instance.players[GameData.Instance.realPlayerID].Items.IndexOf(GameData.items["Minigame Skip"]);
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            level = SceneAs<Level>();
            var options = GenerateOptions();
            actualSelection = rand.Next(totalOptions);
            options[actualSelection] = startChoice;
            UpdateOptions(options);
        }

        private string[] GenerateOptions() {
            // Generate the other options for the minigame select UI
            List<LevelData> decoyMinigames = GameData.Instance.GetAllUnplayedMinigames(level);
            decoyMinigames.RemoveAll(item => unchoosable.Contains(item.Name));
            
            var minigameOptions = new string[totalOptions];
            for (int i = 0; i < totalOptions; i++) {
                int decoyIdx = rand.Next(decoyMinigames.Count);
                minigameOptions[i] = decoyMinigames[decoyIdx].Name;
                decoyMinigames.RemoveAt(decoyIdx);
            }
            return minigameOptions;
        }

        public override void Update() {
            base.Update();
            if(switchDelay < 0) {
                switchDelay = lastSwitchDelay *= 1.4f;
                selected++;
                if(selected >= options.Length) {
                    selected = 0;
                }
                if (lastSwitchDelay > 0.6f && selected == actualSelection) {
                    switchDelay = lastSwitchDelay = float.MaxValue;
                    settled = true;
                    Add(selectedWiggler);
                    selectedWiggler.Start();
                    countdownTimer = -1;
                    countdownNumber = countdownAmount + 1;
                }
            }
            if(countdownTimer < 0) {
                countdownNumber--;
                if (countdownNumber <= 0) {
                    OnSelect?.Invoke(options[selected]);
                    countdownTimer = float.MaxValue;
                    statusText = Dialog.Clean("MadelineParty_Minigame_Select_Go");
                } else {
                    countdownTimer = 1;
                    statusText = countdownNumber.ToString();
                }
            }
            if(skipItemIdx >= 0 && settled && Input.MenuCancel.Pressed) {
                var item = GameData.Instance.players[GameData.Instance.realPlayerID].Items[skipItemIdx];
                GameData.Instance.players[GameData.Instance.realPlayerID].Items.RemoveAt(skipItemIdx);
                skipItemIdx = GameData.Instance.players[GameData.Instance.realPlayerID].Items.IndexOf(GameData.items["Minigame Skip"]);
                item.UseItem(GameData.Instance.realPlayerID);
            }
            countdownTimer -= Engine.DeltaTime;
            switchDelay -= Engine.DeltaTime;
        }

        private void UpdateOptions(string[] options) {
            this.options = options;
            optionsLocalized = new string[options.Length];
            Vector2 maxName = Vector2.Zero;
            for (int i = 0; i < options.Length; i++) {
                optionsLocalized[i] = Dialog.Clean("MadelineParty_Minigame_Name_" + options[i]);
                maxName = Vector2.Max(ActiveFont.Measure(optionsLocalized[i]), maxName);
            }
            width = Math.Max((int)(maxName.X + padding * 2), minWidth);
            height = (int)(ActiveFont.LineHeight * (options.Length + 2) + padding * 2 + gap);
        }

        public override void Render() {
            base.Render();
            Draw.Rect(new(1920 / 2 - width / 2 - borderWidth, 1080 / 2 - height / 2 - borderWidth), width + borderWidth * 2, height + borderWidth * 2, Color.Black);
            Draw.Rect(new(1920 / 2 - width / 2, 1080 / 2 - height / 2), width, height, new Color(29, 18, 24));

            for(int i = 0; i < optionsLocalized.Length; i++) {
                ActiveFont.DrawOutline(optionsLocalized[i], new(1920 / 2, 1080 / 2 - height / 2 + padding + i * ActiveFont.LineHeight), new(0.5f, 0), Vector2.One, Color.White, 2, Color.Black);
            }
            
            ActiveFont.DrawOutline(statusText, new(1920 / 2, 1080 / 2 - height / 2 + padding + gap + (optionsLocalized.Length + 1) * ActiveFont.LineHeight), new(0.5f, 0), Vector2.One, new Color(1, 1, 0.5f), 2, Color.Black);

            float eased = Ease.CubeIn.Invoke(selectedWiggler.Value);
            if (settled && skipItemIdx >= 0) {
                ButtonUI.Render(new(1920 / 2, 1080 / 2 - height / 2 + padding + gap + (optionsLocalized.Length + 0.5f) * ActiveFont.LineHeight), skipPrompt, Input.MenuCancel, 1 + (eased - 0.5f) / 16);
            } else if(skippedBy >= 0) {
                ActiveFont.DrawOutline(skippedText, new(1920 / 2, 1080 / 2 - height / 2 + padding + gap + optionsLocalized.Length * ActiveFont.LineHeight), new(0.5f, 0), Vector2.One, Color.White, 2, Color.Black);
            }
            
            if (selected >= 0) {
                float wiggle = eased * wiggleAmount;
                Draw.Rect(new(1920 / 2 - width / 2 + padding / 2 - wiggle, 1080 / 2 - height / 2 + padding / 2 + selected * ActiveFont.LineHeight - wiggle), width - padding + wiggle * 2, ActiveFont.LineHeight + wiggle * 2, Color.Yellow * 0.5f);
            }
        }

        public void Reroll(int player) {
            var options = GenerateOptions();
            actualSelection = rand.Next(totalOptions);
            unchoosable.Add(options[actualSelection]);
            UpdateOptions(options);
            statusText = Dialog.Clean("MadelineParty_Minigame_Select_Choosing");

            selected = -1;
            lastSwitchDelay = 0.04f;
            switchDelay = 0.3f;
            selectedWiggler.StopAndClear();
            countdownTimer = float.MaxValue;
            settled = false;
            skippedBy = player;
            Dialog.Language.Dialog["MadelineParty_Minigame_Select_Just_Skipped_Player"] = GameData.Instance.GetPlayerName(player);
            skippedText = Dialog.Get(PersistentMiniTextbox.ProcessDialog("MadelineParty_Minigame_Select_Just_Skipped"));
            width = Math.Max((int)(ActiveFont.Measure(skippedText).X + padding * 2), width);
        }

    }
}
