using Celeste;
using Celeste.Mod;
using Celeste.Mod.Helpers;
using Monocle;
using MonoMod;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Xml;
using Celeste.Mod.Meta;
using Microsoft.Xna.Framework;
using System.Text.RegularExpressions;
using Celeste.Mod.UI;
using Microsoft.Xna.Framework.Graphics;
using Celeste.Mod.Entities;

namespace MadelineParty.SubHud {
    [CustomEntity("madelineparty/subHudLevelForwarder")]
    [Tracked]
    public class SubHudLevelForwarder : Entity {
        private static SubHudLevel subHudLevel;

        private static VirtualRenderTarget ResortDustLarge, TempALarge;

        private Level Level;

        private static bool updatingSubHudLevel;

        public SubHudLevelForwarder() {
            AddTag(TagsExt.SubHUD);
            Add(new BeforeRenderHook(BeforeRender));
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            Level = SceneAs<Level>();
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            ChangeSubHudLevel(Level.Session.Level);
        }

        public void BeforeUpdate() {
            subHudLevel?.BeforeUpdate();
        }

        public override void Update() {
            base.Update();
            updatingSubHudLevel = true;
            subHudLevel?.Update();
            updatingSubHudLevel = false;
        }

        public void AfterUpdate() {
            subHudLevel?.AfterUpdate();
        }

        public void BeforeRender() {
            subHudLevel?.BeforeRender();
        }

        public override void Render() {
            base.Render();

            if (subHudLevel != null) {
                SubHudRenderer.EndRender();
                var matrix = SubHudRenderer.DrawToBuffer ? Matrix.Identity : Engine.ScreenMatrix;
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null, matrix * Matrix.CreateScale(2) * Matrix.CreateTranslation(new(-subHudLevel.Bounds.Location.ToVector2() * 2 - Level.ShakeVector * 6, 0)));
                
                var oldTempA = GameplayBuffers.TempA;
                var oldResortDust = GameplayBuffers.ResortDust;
                GameplayBuffers.TempA = TempALarge;
                GameplayBuffers.ResortDust = ResortDustLarge;
                subHudLevel.Render();
                GameplayBuffers.TempA = oldTempA;
                GameplayBuffers.ResortDust = oldResortDust;
                
                SubHudRenderer.EndRender();
                SubHudRenderer.BeginRender();
            }
        }

        public void AfterRender() {
            subHudLevel?.AfterRender();
        }

        public static void Load() {
            On.Celeste.LevelEnter.Go += LevelEnter_Go;
            On.Celeste.DustEdges.BeforeRender += DustEdges_BeforeRender;
            On.Celeste.Audio.CreateInstance += Audio_CreateInstance;
            On.Celeste.Level.AfterRender += Level_AfterRender;
            On.Monocle.Scene.BeforeUpdate += Scene_BeforeUpdate;
            On.Monocle.Scene.AfterUpdate += Scene_AfterUpdate;
            On.Celeste.Mod.Everest.Events.Player.Spawn += Player_Spawn;
        }

        // Stop Death Tracker from breaking because LevelLoader.StartLevel never was called
        private static void Player_Spawn(On.Celeste.Mod.Everest.Events.Player.orig_Spawn orig, Player player) {
            if (player.Scene is not SubHudLevel) {
                orig(player);
            }
        }

        private static void Scene_AfterUpdate(On.Monocle.Scene.orig_AfterUpdate orig, Scene self) {
            orig(self);
            if (self is Level) {
                self.Tracker.GetEntities<SubHudLevelForwarder>().ForEach(e => (e as SubHudLevelForwarder).AfterUpdate());
            }
        }

        private static void Scene_BeforeUpdate(On.Monocle.Scene.orig_BeforeUpdate orig, Scene self) {
            orig(self);
            if (self is Level) {
                self.Tracker.GetEntities<SubHudLevelForwarder>().ForEach(e => (e as SubHudLevelForwarder).BeforeUpdate());
            }
        }

        private static void Level_AfterRender(On.Celeste.Level.orig_AfterRender orig, Level self) {
            orig(self);
            self.Tracker.GetEntities<SubHudLevelForwarder>().ForEach(e => (e as SubHudLevelForwarder).AfterRender());
        }

        private static FMOD.Studio.EventInstance Audio_CreateInstance(On.Celeste.Audio.orig_CreateInstance orig, string path, Vector2? position) {
            if(updatingSubHudLevel)
                return null;
            return orig(path, position);
        }

        private static void DustEdges_BeforeRender(On.Celeste.DustEdges.orig_BeforeRender orig, DustEdges self) {
            if (self.Scene is SubHudLevel) {
                List<Component> components = self.Scene.Tracker.GetComponents<DustEdge>();
                self.hasDust = components.Count > 0;
                if (!self.hasDust) {
                    return;
                }
                Engine.Graphics.GraphicsDevice.SetRenderTarget(TempALarge);
                Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, (self.Scene as Level).Camera.Matrix * Matrix.CreateScale(3));
                foreach (Component item in components) {
                    DustEdge dustEdge = item as DustEdge;
                    if (dustEdge.Visible && dustEdge.Entity.Visible) {
                        dustEdge.RenderDust();
                    }
                }
                Draw.SpriteBatch.End();
                if (self.DustNoiseFrom == null || self.DustNoiseFrom.IsDisposed) {
                    self.CreateTextures();
                }
                Vector2 vector = self.FlooredCamera();
                Engine.Graphics.GraphicsDevice.SetRenderTarget(ResortDustLarge);
                Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);
                Engine.Graphics.GraphicsDevice.Textures[1] = self.DustNoiseFrom.Texture_Safe;
                Engine.Graphics.GraphicsDevice.Textures[2] = self.DustNoiseTo.Texture_Safe;
                GFX.FxDust.Parameters["colors"].SetValue(DustStyles.Get(self.Scene).EdgeColors);
                GFX.FxDust.Parameters["noiseEase"].SetValue(self.noiseEase);
                GFX.FxDust.Parameters["noiseFromPos"].SetValue(self.noiseFromPos + new Vector2(vector.X / 960f, vector.Y / 540f));
                GFX.FxDust.Parameters["noiseToPos"].SetValue(self.noiseToPos + new Vector2(vector.X / 960f, vector.Y / 540f));
                GFX.FxDust.Parameters["pixel"].SetValue(new Vector2(1f / 960f, 1f / 540f));
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, GFX.FxDust, Matrix.Identity);
                Draw.SpriteBatch.Draw((RenderTarget2D)TempALarge, Vector2.Zero, Color.White);
                Draw.SpriteBatch.End();
            } else {
                orig(self);
            }
        }

        private static void LevelEnter_Go(On.Celeste.LevelEnter.orig_Go orig, Session session, bool fromSaveData) {
            if (MadelinePartyModule.IsSIDMadelineParty(session.Area.SID) && AreaData.Get(session.Area.SID + "-Deco") is { } areaData) {
                var subHudSession = new Session(new AreaKey(areaData.ID));
                LoadLevel(subHudSession);

                subHudLevel.LoadLevel(Player.IntroTypes.None, isFromLoader: true);

                ResortDustLarge ??= VirtualContent.CreateRenderTarget("MadelineParty-Large-ResortDust", 960, 540);
                TempALarge ??= VirtualContent.CreateRenderTarget("MadelineParty-Large-TempA", 960, 540);
            }
            orig(session, fromSaveData);
        }

        public void ChangeSubHudLevel(string level) {
            subHudLevel.UnloadLevel();
            subHudLevel.Session.Level = level;
            subHudLevel.LoadLevel(Player.IntroTypes.None, isFromLoader: false);
            foreach (Entity entity in subHudLevel.Entities) {
                entity.SceneBegin(subHudLevel);
            }
            subHudLevel.Camera.Zoom = 1 / 3f;
            subHudLevel.Camera.Position = subHudLevel.Bounds.Location.ToVector2();
            subHudLevel.Tracker.GetEntity<Player>().StateMachine.State = Player.StFrozen;
        }

        #region Modified Level Loading

        private static void LoadLevel(Session session) {
            SurfaceIndex.TileToIndex = new Dictionary<char, int> {
                { '1', 3 },
                { '3', 4 },
                { '4', 7 },
                { '5', 8 },
                { '6', 8 },
                { '7', 8 },
                { '8', 8 },
                { '9', 13 },
                { 'a', 8 },
                { 'b', 23 },
                { 'c', 8 },
                { 'd', 8 },
                { 'e', 8 },
                { 'f', 8 },
                { 'g', 8 },
                { 'G', 8 },
                { 'h', 33 },
                { 'i', 4 },
                { 'j', 8 },
                { 'k', 3 },
                { 'l', 25 },
                { 'm', 44 },
                { 'n', 40 },
                { 'o', 43 }
            };
            SurfaceIndex.IndexToCustomPath.Clear();
            string text = "";
            try {
                MapMeta meta = AreaData.Get(session).GetMeta();
                text = meta?.BackgroundTiles;
                if (string.IsNullOrEmpty(text)) {
                    text = Path.Combine("Graphics", "BackgroundTiles.xml");
                }
                GFX.BGAutotiler = new Autotiler(text);
                text = meta?.ForegroundTiles;
                if (string.IsNullOrEmpty(text)) {
                    text = Path.Combine("Graphics", "ForegroundTiles.xml");
                }
                GFX.FGAutotiler = new Autotiler(text);
                text = meta?.AnimatedTiles;
                if (string.IsNullOrEmpty(text)) {
                    text = Path.Combine("Graphics", "AnimatedTiles.xml");
                }
                GFX.AnimatedTilesBank = new AnimatedTilesBank();
                foreach (XmlElement item in Calc.LoadContentXML(text).GetElementsByTagName("sprite")) {
                    if (item != null) {
                        GFX.AnimatedTilesBank.Add(item.Attr("name"), item.AttrFloat("delay", 0f), item.AttrVector2("posX", "posY", Vector2.Zero), item.AttrVector2("origX", "origY", Vector2.Zero), GFX.Game.GetAtlasSubtextures(item.Attr("path")));
                    }
                }
                GFX.SpriteBank = new SpriteBank(GFX.Game, Path.Combine("Graphics", "Sprites.xml"));
                text = meta?.Sprites;
                if (!string.IsNullOrEmpty(text)) {
                    SpriteBank spriteBank = GFX.SpriteBank;
                    foreach (KeyValuePair<string, SpriteData> spriteDatum in new SpriteBank(GFX.Game, getModdedSpritesXml(text)).SpriteData) {
                        string key = spriteDatum.Key;
                        SpriteData value = spriteDatum.Value;
                        if (spriteBank.SpriteData.TryGetValue(key, out var value2)) {
                            IDictionary animations = value2.Sprite.GetAnimations();
                            foreach (DictionaryEntry item2 in (IDictionary)value.Sprite.GetAnimations()) {
                                animations[item2.Key] = item2.Value;
                            }
                            value2.Sources.AddRange(value.Sources);
                            value2.Sprite.Stop();
                            if (value.Sprite.CurrentAnimationID != "") {
                                value2.Sprite.Play(value.Sprite.CurrentAnimationID);
                            }
                        } else {
                            spriteBank.SpriteData[key] = value;
                        }
                    }
                }
                PlayerSprite.ClearFramesMetadata();
                PlayerSprite.CreateFramesMetadata("player");
                PlayerSprite.CreateFramesMetadata("player_no_backpack");
                PlayerSprite.CreateFramesMetadata("badeline");
                PlayerSprite.CreateFramesMetadata("player_badeline");
                PlayerSprite.CreateFramesMetadata("player_playback");
                text = meta?.Portraits;
                if (string.IsNullOrEmpty(text)) {
                    text = Path.Combine("Graphics", "Portraits.xml");
                }
                GFX.PortraitsSpriteBank = new SpriteBank(GFX.Portraits, text);
            } catch (Exception ex) {
                string text2 = session?.Area.GetSID() ?? "NULL";
                if (LevelEnter.ErrorMessage == null) {
                    if (ex is XmlException) {
                        //LevelEnter.ErrorMessage = Dialog.Get("postcard_xmlerror").Replace("((path))", text);
                        Logger.Log(LogLevel.Warn, "LevelLoader", "Failed parsing " + text);
                    } else if (ex.TypeInStacktrace(typeof(Autotiler))) {
                       // LevelEnter.ErrorMessage = Dialog.Get("postcard_tilexmlerror").Replace("((path))", text);
                        Logger.Log(LogLevel.Warn, "LevelLoader", "Failed parsing tileset tag in " + text);
                    } else {
                        //LevelEnter.ErrorMessage = Dialog.Get("postcard_levelloadfailed").Replace("((sid))", text2);
                    }
                }
                Logger.Log(LogLevel.Warn, "LevelLoader", "Failed loading " + text2);
                ex.LogDetailed();
            }
            subHudLevel = new();
            LoadingProcess(session);
        }

        private static XmlDocument getModdedSpritesXml(string path) {
            XmlDocument vanillaSpritesXml = Calc.orig_LoadContentXML(Path.Combine("Graphics", "Sprites.xml"));
            XmlDocument modSpritesXml = Calc.LoadContentXML(path);
            return SpriteBank.GetSpriteBankExcludingVanillaCopyPastes(vanillaSpritesXml, modSpritesXml, path);
        }

        private static void LoadingProcess(Session session) {
            MapData mapData = session.MapData;
            AreaData areaData = AreaData.Get(session);
            subHudLevel.Add(subHudLevel.GameplayRenderer = new GameplayRenderer());
            subHudLevel.Add(subHudLevel.Lighting = new LightingRenderer());
            subHudLevel.Bloom = new BloomRenderer();
            subHudLevel.Add(subHudLevel.Displacement = new DisplacementRenderer());
            subHudLevel.Background = new BackdropRenderer();
            subHudLevel.Foreground = new BackdropRenderer();
            subHudLevel.Add(new DustEdges());
            subHudLevel.Add(new WaterSurface());
            subHudLevel.Add(new MirrorSurfaces());
            subHudLevel.Add(new GlassBlockBg());
            subHudLevel.Add(new LightningRenderer());
            subHudLevel.Add(new SeekerBarrierRenderer());
            //Level.Add(Level.SubHudRenderer = new SubHudRenderer());
            //Level.Add(Level.HudRenderer = new HudRenderer());
            //if (session.Area.ID == 9) {
            //    Level.Add(new IceTileOverlay());
            //}
            subHudLevel.BaseLightingAlpha = (subHudLevel.Lighting.Alpha = areaData.DarknessAlpha);
            //subHudLevel.Bloom.Base = areaData.BloomBase;
            //subHudLevel.Bloom.Strength = areaData.BloomStrength;
            subHudLevel.RendererList.UpdateLists();
            subHudLevel.FormationBackdrop = new FormationBackdrop();
            subHudLevel.Camera = subHudLevel.GameplayRenderer.Camera;
            subHudLevel.Session = session;
            subHudLevel.Particles = new ParticleSystem(-8000, 400);
            subHudLevel.Particles.Tag = Tags.Global;
            subHudLevel.Add(subHudLevel.Particles);
            subHudLevel.ParticlesBG = new ParticleSystem(8000, 400);
            subHudLevel.ParticlesBG.Tag = Tags.Global;
            subHudLevel.Add(subHudLevel.ParticlesBG);
            subHudLevel.ParticlesFG = new ParticleSystem(-50000, 800);
            subHudLevel.ParticlesFG.Tag = Tags.Global;
            subHudLevel.ParticlesFG.Add(new MirrorReflection());
            subHudLevel.Add(subHudLevel.ParticlesFG);
            
            Rectangle tileBounds = mapData.TileBounds;
            GFX.FGAutotiler.LevelBounds.Clear();
            var virtualMap = new VirtualMap<char>(tileBounds.Width, tileBounds.Height, '0');
            var virtualMap2 = new VirtualMap<char>(tileBounds.Width, tileBounds.Height, '0');
            var virtualMap3 = new VirtualMap<bool>(tileBounds.Width, tileBounds.Height, emptyValue: false);
            Regex regex = new("\\r\\n|\\n\\r|\\n|\\r");
            foreach (LevelData level in mapData.Levels) {
                int left = level.TileBounds.Left;
                int top = level.TileBounds.Top;
                string[] array = regex.Split(level.Bg);
                for (int i = top; i < top + array.Length; i++) {
                    for (int j = left; j < left + array[i - top].Length; j++) {
                        virtualMap[j - tileBounds.X, i - tileBounds.Y] = array[i - top][j - left];
                    }
                }
                string[] array2 = regex.Split(level.Solids);
                for (int k = top; k < top + array2.Length; k++) {
                    for (int l = left; l < left + array2[k - top].Length; l++) {
                        virtualMap2[l - tileBounds.X, k - tileBounds.Y] = array2[k - top][l - left];
                    }
                }
                for (int m = level.TileBounds.Left; m < level.TileBounds.Right; m++) {
                    for (int n = level.TileBounds.Top; n < level.TileBounds.Bottom; n++) {
                        virtualMap3[m - tileBounds.Left, n - tileBounds.Top] = true;
                    }
                }
                GFX.FGAutotiler.LevelBounds.Add(new Rectangle(level.TileBounds.X - tileBounds.X, level.TileBounds.Y - tileBounds.Y, level.TileBounds.Width, level.TileBounds.Height));
            }
            foreach (Rectangle item in mapData.Filler) {
                for (int num = item.Left; num < item.Right; num++) {
                    for (int num2 = item.Top; num2 < item.Bottom; num2++) {
                        char c = '0';
                        if (item.Top - tileBounds.Y > 0) {
                            char c2 = virtualMap2[num - tileBounds.X, item.Top - tileBounds.Y - 1];
                            if (c2 != '0') {
                                c = c2;
                            }
                        }
                        if (c == '0' && item.Left - tileBounds.X > 0) {
                            char c3 = virtualMap2[item.Left - tileBounds.X - 1, num2 - tileBounds.Y];
                            if (c3 != '0') {
                                c = c3;
                            }
                        }
                        if (c == '0' && item.Right - tileBounds.X < tileBounds.Width - 1) {
                            char c4 = virtualMap2[item.Right - tileBounds.X, num2 - tileBounds.Y];
                            if (c4 != '0') {
                                c = c4;
                            }
                        }
                        if (c == '0' && item.Bottom - tileBounds.Y < tileBounds.Height - 1) {
                            char c5 = virtualMap2[num - tileBounds.X, item.Bottom - tileBounds.Y];
                            if (c5 != '0') {
                                c = c5;
                            }
                        }
                        if (c == '0') {
                            c = '1';
                        }
                        virtualMap2[num - tileBounds.X, num2 - tileBounds.Y] = c;
                        virtualMap3[num - tileBounds.X, num2 - tileBounds.Y] = true;
                    }
                }
            }
            foreach (LevelData level2 in mapData.Levels) {
                for (int num3 = level2.TileBounds.Left; num3 < level2.TileBounds.Right; num3++) {
                    int top2 = level2.TileBounds.Top;
                    char value = virtualMap[num3 - tileBounds.X, top2 - tileBounds.Y];
                    for (int num4 = 1; num4 < 4 && !virtualMap3[num3 - tileBounds.X, top2 - tileBounds.Y - num4]; num4++) {
                        virtualMap[num3 - tileBounds.X, top2 - tileBounds.Y - num4] = value;
                    }
                    top2 = level2.TileBounds.Bottom - 1;
                    char value2 = virtualMap[num3 - tileBounds.X, top2 - tileBounds.Y];
                    for (int num5 = 1; num5 < 4 && !virtualMap3[num3 - tileBounds.X, top2 - tileBounds.Y + num5]; num5++) {
                        virtualMap[num3 - tileBounds.X, top2 - tileBounds.Y + num5] = value2;
                    }
                }
                for (int num6 = level2.TileBounds.Top - 4; num6 < level2.TileBounds.Bottom + 4; num6++) {
                    int left2 = level2.TileBounds.Left;
                    char value3 = virtualMap[left2 - tileBounds.X, num6 - tileBounds.Y];
                    for (int num7 = 1; num7 < 4 && !virtualMap3[left2 - tileBounds.X - num7, num6 - tileBounds.Y]; num7++) {
                        virtualMap[left2 - tileBounds.X - num7, num6 - tileBounds.Y] = value3;
                    }
                    left2 = level2.TileBounds.Right - 1;
                    char value4 = virtualMap[left2 - tileBounds.X, num6 - tileBounds.Y];
                    for (int num8 = 1; num8 < 4 && !virtualMap3[left2 - tileBounds.X + num8, num6 - tileBounds.Y]; num8++) {
                        virtualMap[left2 - tileBounds.X + num8, num6 - tileBounds.Y] = value4;
                    }
                }
            }
            foreach (LevelData level3 in mapData.Levels) {
                for (int num9 = level3.TileBounds.Left; num9 < level3.TileBounds.Right; num9++) {
                    int top3 = level3.TileBounds.Top;
                    if (virtualMap2[num9 - tileBounds.X, top3 - tileBounds.Y] == '0') {
                        for (int num10 = 1; num10 < 8; num10++) {
                            virtualMap3[num9 - tileBounds.X, top3 - tileBounds.Y - num10] = true;
                        }
                    }
                    top3 = level3.TileBounds.Bottom - 1;
                    if (virtualMap2[num9 - tileBounds.X, top3 - tileBounds.Y] == '0') {
                        for (int num11 = 1; num11 < 8; num11++) {
                            virtualMap3[num9 - tileBounds.X, top3 - tileBounds.Y + num11] = true;
                        }
                    }
                }
            }
            foreach (LevelData level4 in mapData.Levels) {
                for (int num12 = level4.TileBounds.Left; num12 < level4.TileBounds.Right; num12++) {
                    int top4 = level4.TileBounds.Top;
                    char value5 = virtualMap2[num12 - tileBounds.X, top4 - tileBounds.Y];
                    for (int num13 = 1; num13 < 4 && !virtualMap3[num12 - tileBounds.X, top4 - tileBounds.Y - num13]; num13++) {
                        virtualMap2[num12 - tileBounds.X, top4 - tileBounds.Y - num13] = value5;
                    }
                    top4 = level4.TileBounds.Bottom - 1;
                    char value6 = virtualMap2[num12 - tileBounds.X, top4 - tileBounds.Y];
                    for (int num14 = 1; num14 < 4 && !virtualMap3[num12 - tileBounds.X, top4 - tileBounds.Y + num14]; num14++) {
                        virtualMap2[num12 - tileBounds.X, top4 - tileBounds.Y + num14] = value6;
                    }
                }
                for (int num15 = level4.TileBounds.Top - 4; num15 < level4.TileBounds.Bottom + 4; num15++) {
                    int left3 = level4.TileBounds.Left;
                    char value7 = virtualMap2[left3 - tileBounds.X, num15 - tileBounds.Y];
                    for (int num16 = 1; num16 < 4 && !virtualMap3[left3 - tileBounds.X - num16, num15 - tileBounds.Y]; num16++) {
                        virtualMap2[left3 - tileBounds.X - num16, num15 - tileBounds.Y] = value7;
                    }
                    left3 = level4.TileBounds.Right - 1;
                    char value8 = virtualMap2[left3 - tileBounds.X, num15 - tileBounds.Y];
                    for (int num17 = 1; num17 < 4 && !virtualMap3[left3 - tileBounds.X + num17, num15 - tileBounds.Y]; num17++) {
                        virtualMap2[left3 - tileBounds.X + num17, num15 - tileBounds.Y] = value8;
                    }
                }
            }
            Vector2 position = new Vector2(tileBounds.X, tileBounds.Y) * 8f;
            Calc.PushRandom(mapData.LoadSeed);
            BackgroundTiles backgroundTiles = null;
            SolidTiles solidTiles = null;
            subHudLevel.Add(subHudLevel.BgTiles = (backgroundTiles = new BackgroundTiles(position, virtualMap)));
            subHudLevel.Add(subHudLevel.SolidTiles = (solidTiles = new SolidTiles(position, virtualMap2)));
            subHudLevel.BgData = virtualMap;
            subHudLevel.SolidsData = virtualMap2;
            Calc.PopRandom();
            new Entity(position).Add(subHudLevel.FgTilesLightMask = new TileGrid(8, 8, tileBounds.Width, tileBounds.Height));
            subHudLevel.FgTilesLightMask.Color = Color.Black;
            foreach (LevelData level5 in mapData.Levels) {
                int left4 = level5.TileBounds.Left;
                int top5 = level5.TileBounds.Top;
                int width = level5.TileBounds.Width;
                int height = level5.TileBounds.Height;
                if (!string.IsNullOrEmpty(level5.BgTiles)) {
                    int[,] tiles = Calc.ReadCSVIntGrid(level5.BgTiles, width, height);
                    backgroundTiles.Tiles.Overlay(GFX.SceneryTiles, tiles, left4 - tileBounds.X, top5 - tileBounds.Y);
                }
                if (!string.IsNullOrEmpty(level5.FgTiles)) {
                    int[,] tiles2 = Calc.ReadCSVIntGrid(level5.FgTiles, width, height);
                    solidTiles.Tiles.Overlay(GFX.SceneryTiles, tiles2, left4 - tileBounds.X, top5 - tileBounds.Y);
                    subHudLevel.FgTilesLightMask.Overlay(GFX.SceneryTiles, tiles2, left4 - tileBounds.X, top5 - tileBounds.Y);
                }
            }
            if (areaData.OnLevelBegin != null) {
                areaData.OnLevelBegin(subHudLevel);
            }
            //subHudLevel.StartPosition = startPosition;
            //Everest.Events.LevelLoader.LoadingThread(Level);
        }

        #endregion

        public static void Unload() {
            On.Celeste.LevelEnter.Go -= LevelEnter_Go;
            On.Celeste.DustEdges.BeforeRender -= DustEdges_BeforeRender;
            On.Celeste.Audio.CreateInstance -= Audio_CreateInstance;
            On.Celeste.Level.AfterRender -= Level_AfterRender;
            On.Monocle.Scene.BeforeUpdate -= Scene_BeforeUpdate;
            On.Monocle.Scene.AfterUpdate -= Scene_AfterUpdate;
            On.Celeste.Mod.Everest.Events.Player.Spawn -= Player_Spawn;
        }
    }

    public class SubHudLevel : Level {
        [MonoModLinkTo("Monocle.Scene", "System.Void Update()")]
        public void Scene_Update() { }

        public override void Update() {
            if (FrozenOrPaused) {
                Entities.UpdateLists();
            } else if (!Transitioning) {
                Scene_Update();
            }
            foreach (PostUpdateHook component in Tracker.GetComponents<PostUpdateHook>()) {
                if (component.Entity.Active) {
                    component.OnPostUpdate();
                }
            }
        }

        public override void Render() {
            // TODO fix begin getting called too soon
            Entities.RenderExcept((int)Tags.HUD | (int)TagsExt.SubHUD);
            //GameplayRenderer.Render(this);
            //Lighting.Render(this);
            //Engine.Instance.GraphicsDevice.SetRenderTarget(GameplayBuffers.Level);
            //Engine.Instance.GraphicsDevice.Clear(BackgroundColor);
            //Distort.Render((RenderTarget2D)GameplayBuffers.Gameplay, (RenderTarget2D)GameplayBuffers.Displacement, Displacement.HasDisplacement(this));
            //Bloom.Apply(GameplayBuffers.Level, this);

            /*Engine.Instance.GraphicsDevice.SetRenderTarget(null);
            Engine.Instance.GraphicsDevice.Clear(Color.Black);
            Engine.Instance.GraphicsDevice.Viewport = Engine.Viewport;
            Matrix matrix = Matrix.CreateScale(6f) * Engine.ScreenMatrix;
            Vector2 vector = new Vector2(320f, 180f);
            Vector2 vector2 = vector / ZoomTarget;
            Vector2 vector3 = ((ZoomTarget != 1f) ? ((ZoomFocusPoint - vector2 / 2f) / (vector - vector2) * vector) : Vector2.Zero);
            MTexture orDefault = GFX.ColorGrades.GetOrDefault(lastColorGrade, GFX.ColorGrades["none"]);
            MTexture orDefault2 = GFX.ColorGrades.GetOrDefault(Session.ColorGrade, GFX.ColorGrades["none"]);
            if (colorGradeEase > 0f && orDefault != orDefault2) {
                ColorGrade.Set(orDefault, orDefault2, colorGradeEase);
            } else {
                ColorGrade.Set(orDefault2);
            }
            float scale = Zoom * ((320f - ScreenPadding * 2f) / 320f);
            Vector2 vector4 = new Vector2(ScreenPadding, ScreenPadding * 0.5625f);
            if (SaveData.Instance.Assists.MirrorMode) {
                vector4.X = 0f - vector4.X;
                vector3.X = 160f - (vector3.X - 160f);
            }
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, ColorGrade.Effect, matrix);
            Draw.SpriteBatch.Draw((RenderTarget2D)GameplayBuffers.Level, vector3 + vector4, GameplayBuffers.Level.Bounds, Color.White, 0f, vector3, scale, SaveData.Instance.Assists.MirrorMode ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
            Draw.SpriteBatch.End();
            if (Pathfinder != null && Pathfinder.DebugRenderEnabled) {
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, null, Camera.Matrix * matrix);
                Pathfinder.Render();
                Draw.SpriteBatch.End();
            }
            SubHudRenderer.Render(this);
            if (((!Paused || !PauseMainMenuOpen) && !(wasPausedTimer < 1f)) || !Input.MenuJournal.Check || !AllowHudHide) {
                HudRenderer.Render(this);
            }
            if (Wipe != null) {
                Wipe.Render(this);
            }
            if (HiresSnow != null) {
                HiresSnow.Render(this);
            }*/
        }
    }
}
