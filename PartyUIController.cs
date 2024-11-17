using Celeste;
using Celeste.Mod.Entities;
using MadelineParty.Board;
using MadelineParty.Multiplayer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadelineParty {
    [CustomEntity("madelineparty/partyUIController")]
    public class PartyUIController : Entity {
        /*
         * --------------------------------
         * Need X more Board Mode players |
         *                                |
         * You                            |
         * Waiting...                     |
         * Waiting...                     |
         * Waiting...                     |
         * --------------------------------
         * 
         */

        private const int minWidth = 350;
        private const int borderWidth = 4;
        private const int topDistance = 270;
        private const int padding = 10;
        private const int playerNameSeparation = 5;

        private string lookingForOneText, lookingForPluralText, allPlayersText, waitingText;

        public PartyUIController() {
            Depth = -2000;
            AddTag(TagsExt.SubHUD);
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            if (GameData.Instance.playerNumber <= 1) {
                RemoveSelf();
                return;
            }
            var modeName = Dialog.Clean("MadelineParty_Menu_Mode_" + ModeManager.Instance.Mode);
            lookingForOneText = Dialog.Clean("MadelineParty_Party_Need_One_More")
                .Replace("((mode))", modeName);
            lookingForPluralText = Dialog.Clean("MadelineParty_Party_Need_Multiple_More")
                .Replace("((mode))", modeName);
            allPlayersText = Dialog.Clean("MadelineParty_Party_All_Players_Joined")
                .Replace("((mode))", modeName);
            waitingText = Dialog.Clean("MadelineParty_Party_Waiting");
        }

        public override void Render() {
            base.Render();
            var neededPlayers = GameData.Instance.playerNumber - 1 - GameData.Instance.celestenetIDs.Count;
            var filledLookingForText = neededPlayers switch {
                0 => allPlayersText,
                1 => lookingForOneText,
                _ => lookingForPluralText
            };
            filledLookingForText = filledLookingForText.Replace("((count))", neededPlayers.ToString());
            var width = Calc.Max(minWidth, (int)(ActiveFont.Measure(filledLookingForText).X / 2) + padding * 2, (int)(ActiveFont.Measure(waitingText).X / 2) + padding * 2);
            var height = padding * 2 + ActiveFont.LineHeight / 2 + (GameData.Instance.playerNumber + 0.5f) * (playerNameSeparation * 2 + ActiveFont.LineHeight / 2);

            Draw.Rect(new(-borderWidth, topDistance - borderWidth), width + borderWidth * 2, height + borderWidth * 2, Color.Black);
            Draw.Rect(new(0, topDistance), width, height, new Color(29, 18, 24));

            ActiveFont.Draw(filledLookingForText, new(padding, topDistance + borderWidth + padding), Vector2.Zero, new(0.5f), Color.White);

            for (int i = 0; i < GameData.Instance.playerNumber; i++) {
                string playerName = null;
                Color nameColor = Color.White;
                uint playerID = uint.MaxValue;
                MTexture tokenTex = null;

                if (i == 0) {
                    playerID = MultiplayerSingleton.Instance.CurrentPlayerID();
                    playerName = MultiplayerSingleton.Instance.GetPlayer(playerID).Name;
                    nameColor = new Color(1, 1, 0.7f);
                    if (GameData.Instance.currentPlayerSelection != null) {
                        tokenTex = GFX.Gui[PlayerToken.GetFullPath(BoardController.TokenPaths[GameData.Instance.currentPlayerSelection.playerID]) + "00"];
                    }
                } else if (i - 1 < GameData.Instance.celestenetIDs.Count) {
                    playerID = GameData.Instance.celestenetIDs[i - 1];
                    playerName = MultiplayerSingleton.Instance.GetPlayer(playerID).Name;
                    if (GameData.Instance.playerSelectTriggers.TryGetValue(playerID, out int trigger) && trigger >= 0) {
                        tokenTex = GFX.Gui[PlayerToken.GetFullPath(BoardController.TokenPaths[trigger]) + "00"];
                    }
                }

                var textTop = topDistance + borderWidth + padding + ActiveFont.LineHeight / 2 + (i + 0.5f) * (playerNameSeparation * 2 + ActiveFont.LineHeight / 2);
                tokenTex?.DrawJustified(new(padding, textTop + ActiveFont.LineHeight / 4), new Vector2(0, 0.5f), Color.White, 0.25f);

                ActiveFont.Draw(playerName ?? waitingText, new(padding * 2 + 40, textTop),
                    Vector2.Zero, new(0.5f), playerName == null ? Color.Gray : nameColor);
            }
        }

    }
}
