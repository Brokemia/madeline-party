using System;
using System.Collections;
using System.Linq;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty {
    public class GameScoreboard : Entity, IComparable {
        private class ScoreboardText : Entity {
            private GameScoreboard parent;

            public ScoreboardText(GameScoreboard parent) {
                this.parent = parent;
                AddTag(TagsExt.SubHUD);
                AddTag(Tags.PauseUpdate);
                AddTag(Tags.FrozenUpdate);
                Depth = parent.Depth;
            }

            public override void Render() {
                base.Render();
                if (parent.currentMode != Modes.ENTERSHOP) {
                    if (parent == null || GameData.players[parent.PlayerID] == null) return;
                    string strawberryText = ((parent.tempStrawberries == -1) ? GameData.players[parent.PlayerID].strawberries : parent.tempStrawberries) + "";
                    string heartText = GameData.players[parent.PlayerID].hearts + "";
                    if (parent.currentMode == Modes.BUYHEART) {
                        ActiveFont.DrawOutline("-" + GameData.heartCost, (parent.Position - parent.level.LevelOffset) * 6 + new Vector2(8, 34) * 6 - parent.level.ShakeVector * 6, new Vector2(0.5f, 0.5f), Vector2.One, Color.Red, 2f, Color.Black);
                        ActiveFont.DrawOutline("+1", (parent.Position - parent.level.LevelOffset) * 6 + new Vector2(0x1E, 34) * 6 - parent.level.ShakeVector * 6, new Vector2(0.5f, 0.5f), Vector2.One, Color.LimeGreen, 2f, Color.Black);
                    } else if (parent.currentMode == Modes.BUYITEM) {
                        ActiveFont.DrawOutline("-" + GameData.itemPrices[parent.itemBeingBought], (parent.Position - parent.level.LevelOffset) * 6 + new Vector2(8, 34) * 6 - parent.level.ShakeVector * 6, new Vector2(0.5f, 0.5f), Vector2.One, Color.Red, 2f, Color.Black);
                        ActiveFont.DrawOutline("+1", (parent.Position - parent.level.LevelOffset) * 6 + new Vector2(0x1E, 34) * 6 - parent.level.ShakeVector * 6, new Vector2(0.5f, 0.5f), Vector2.One, Color.LimeGreen, 2f, Color.Black);
                        heartText = GameData.players[parent.PlayerID].items.Count((i) => i == parent.itemBeingBought) + "";
                    } else if (parent.currentMode == Modes.BUYARBITRARY) {
                        ActiveFont.DrawOutline("-" + parent.arbitraryCost, (parent.Position - parent.level.LevelOffset) * 6 + new Vector2(8, 34) * 6 - parent.level.ShakeVector * 6, new Vector2(0.5f, 0.5f), Vector2.One, Color.Red, 2f, Color.Black);
                        ActiveFont.DrawOutline("+" + parent.arbitraryPurchaseAmount, (parent.Position - parent.level.LevelOffset) * 6 + new Vector2(0x1E, 34) * 6 - parent.level.ShakeVector * 6, new Vector2(0.5f, 0.5f), Vector2.One, Color.LimeGreen, 2f, Color.Black);
                    }
                    ActiveFont.DrawOutline(strawberryText, (parent.Position - parent.level.LevelOffset) * 6 + new Vector2(8, 26) * 6 - parent.level.ShakeVector * 6, new Vector2(0.5f, 0.5f), Vector2.One, Color.White, 2f, Color.Black);
                    if (parent.currentMode != Modes.BUYARBITRARY) {
                        ActiveFont.DrawOutline(heartText, (parent.Position - parent.level.LevelOffset) * 6 + new Vector2(29.5f, 26) * 6 - parent.level.ShakeVector * 6, new Vector2(0.5f, 0.5f), Vector2.One, Color.White, 2f, Color.Black);
                    }
                }

                // Display items
                for (int i = 0; i < GameData.players[parent.playerID].items.Count; i++) {
                    switch (GameData.players[parent.playerID].items[i]) {
                        case GameData.Item.DOUBLEDICE:
                            parent.doubleDiceTexture.Draw((parent.Position - parent.level.LevelOffset) * 6 + new Vector2(0x0C + i * 0x24, 0x0C) - parent.level.ShakeVector * 6, Vector2.Zero, Color.White, new Vector2(2, 2));
                            break;
                    }
                }
                // Display items in the shop
                if (parent.currentMode == Modes.ENTERSHOP) {
                    for (int i = 0; i < GameData.shopContents.Count; i++) {
                        switch (GameData.shopContents[i]) {
                            case GameData.Item.DOUBLEDICE:
                                parent.doubleDiceTexture.Draw((parent.Position - parent.level.LevelOffset) * 6 + new Vector2(0x12 * 6 + 2 + i * 0x24 - (GameData.shopContents.Count - 1) * 18, 0x20 * 6) - parent.level.ShakeVector * 6, Vector2.Zero, Color.White, new Vector2(2, 2));
                                break;
                        }
                    }
                }
            }
        }

        public enum Modes {
            NORMAL,
            BUYITEM, // TODO Maybe refactor BUYITEM code to use BUYARBITRARY
            ENTERSHOP,
            BUYHEART,
            BUYARBITRARY
        }

        private Level level;

        private int playerID;
        public int PlayerID {
            get {
                return playerID == -1 && level != null ? GetTokenID() : playerID;
            }
        }

        public Vector2 Dimensions = new Vector2(40, 40);

        private MTexture baseTexture, arrowTexture, shopTexture, doubleDiceTexture;
        private Sprite strawberrySprite, heartSprite;

        private ScoreboardText text;

        private int tempStrawberries = -1;
        private float strawberryChangeSpeed = .25f;

        private Modes currentMode = Modes.NORMAL;
        private GameData.Item itemBeingBought = GameData.Item.DOUBLEDICE;

        private MTexture arbitraryPurchase;
        private int arbitraryPurchaseAmount;
        private int arbitraryCost;

        public GameScoreboard(Vector2 pos, int playerID) : base(pos) {
            Depth = -10000;
            baseTexture = GFX.Game["objects/madelineparty/scoreboardbase"];
            arrowTexture = GFX.Game["objects/madelineparty/scoreboardarrow"];
            shopTexture = GFX.Game["decals/madelineparty/shopspace"];
            doubleDiceTexture = GFX.Game["decals/madelineparty/doubledice"];
            this.playerID = playerID;
            AddTag(Tags.PauseUpdate);
            AddTag(Tags.FrozenUpdate);
        }

        public GameScoreboard(EntityData data, Vector2 offset) : this(data.Position + offset, data.Int("playerID", -1)) {

        }

        public override void Added(Scene scene) {
            base.Added(scene);
            level = SceneAs<Level>();
            if (GameData.players[PlayerID] != null) {
                level.Add(text = new ScoreboardText(this));
                strawberrySprite = GFX.SpriteBank.Create("strawberry");
                Add(strawberrySprite);
                strawberrySprite.Rate = 1f;
                strawberrySprite.Position += new Vector2(0x08, 0x10);
                strawberrySprite.Play("idle");

                heartSprite = GFX.SpriteBank.Create("heartgem0");
                Add(heartSprite);
                heartSprite.Play("spin");
                heartSprite.Position += new Vector2(0x1D, 0x10);
                heartSprite.Scale = new Vector2(.75f, .75f);
            }
        }

        public override void Render() {
            baseTexture.Draw(Position);
            if (currentMode == Modes.BUYHEART)
                arrowTexture.Draw(Position + new Vector2(0x0F, 0x0C));
            if (currentMode == Modes.ENTERSHOP)
                shopTexture.Draw(Position + new Vector2(0x0C, 0x0C), Vector2.Zero, Color.White, 2f);
            if (currentMode == Modes.BUYITEM) {
                arrowTexture.Draw(Position + new Vector2(0x0F, 0x0C));
                switch (itemBeingBought) {
                    case GameData.Item.DOUBLEDICE:
                        doubleDiceTexture.Draw(Position + new Vector2(0x16, 0x06));
                        break;
                }

            }
            if(currentMode == Modes.BUYARBITRARY) {
                arrowTexture.Draw(Position + new Vector2(0x0F, 0x0C));
                arbitraryPurchase?.Draw(Position + new Vector2(0x16, 0x06));
            }
            base.Render();
        }

        public void StrawberryChange(int changeBy, float changeSpeed) {
            strawberryChangeSpeed = changeSpeed;
            tempStrawberries = GameData.players[playerID].strawberries;
            Add(new Coroutine(StrawberryChangeRoutine(changeBy)));
        }

        private IEnumerator StrawberryChangeRoutine(int changeBy) {
            for (int i = 0; i < Math.Abs(changeBy) && tempStrawberries >= 0; i++) {
                yield return strawberryChangeSpeed;
                tempStrawberries += Math.Sign(changeBy);
            }

            tempStrawberries = -1;
        }

        // Get the ID of the token of the player using it
        private int GetTokenID() {
            if (X < level.LevelOffset.X + level.Bounds.Width / 2 && Y < level.LevelOffset.Y + level.Bounds.Height / 2) {
                return 0;
            }
            if (X > level.LevelOffset.X + level.Bounds.Width / 2 && Y < level.LevelOffset.Y + level.Bounds.Height / 2) {
                return 1;
            }
            if (X < level.LevelOffset.X + level.Bounds.Width / 2 && Y > level.LevelOffset.Y + level.Bounds.Height / 2) {
                return 2;
            }
            if (X > level.LevelOffset.X + level.Bounds.Width / 2 && Y > level.LevelOffset.Y + level.Bounds.Height / 2) {
                return 3;
            }

            return 0;
        }

        public void BuyArbitrary(MTexture buyingTex, int strawberryCost, int buyingCount = 1) {
            arbitraryPurchase = buyingTex;
            arbitraryCost = strawberryCost;
            arbitraryPurchaseAmount = buyingCount;
            SetCurrentMode(Modes.BUYARBITRARY);
        }

        // Also sets the item to display that we're buying
        public void SetCurrentMode(Modes mode, GameData.Item item) {
            SetCurrentMode(mode);
            itemBeingBought = item;
        }

        public void SetCurrentMode(Modes mode) {
            currentMode = mode;
            strawberrySprite.Visible = mode != Modes.ENTERSHOP;
            if (mode == Modes.BUYITEM || mode == Modes.ENTERSHOP || mode == Modes.BUYARBITRARY) {
                heartSprite.Visible = false;
            } else {
                strawberrySprite.Visible = true;
                heartSprite.Visible = true;
            }
        }

        public int CompareTo(object obj) {
            if (obj == null) return 1;
            return obj is GameScoreboard other ? PlayerID.CompareTo(other.PlayerID) : 1;
        }
    }
}
