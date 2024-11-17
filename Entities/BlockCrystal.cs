using Celeste;
using Celeste.Mod.Entities;
using Celeste.Mod.UI;
using FactoryHelper.Components;
using MadelineParty.Multiplayer.General;
using MadelineParty.Multiplayer;
using MadelineParty.SubHud;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using MadelineParty.Triggers;

namespace MadelineParty.Entities {
    [CustomEntity("madelineparty/blockCrystal")]
    [Tracked]
    public class BlockCrystal : TheoCrystal {
        // I got really lazy with reusability here
        private class BlockCrystalSubHUD : Entity {
            private readonly BlockCrystal parent;
            private Level level;
            private SubHudTileGrid subHudTileGrid;
            private List<Image> spikeImages = [];

            private Vector2 renderOffset;
            private float scale;
            public float Scale {
                get => scale;
                set {
                    scale = value;
                    renderOffset = new(-parent.width * 4 * scale / 6f + 0.5f, -8 - parent.height * 4 * scale / 6f);

                    UpdateGraphicsScale();
                }
            }

            private float alpha = 1;
            public float Alpha {
                get => alpha;
                set {
                    alpha = value;
                    subHudTileGrid.Alpha = alpha;
                    foreach (var spike in spikeImages) {
                        spike.Color = Color.White * alpha;
                    }
                }
            }

            public Vector2? PositionOverride { get; set; }

            // The funny
            public Func<Vector2> PositionOverrideOverride { get; set; }

            public BlockCrystalSubHUD(BlockCrystal parent, float scale) {
                this.parent = parent;
                Scale = scale;

                AddTag(TagsExt.SubHUD);
                Add(new RenderPlayerToBuffer(AfterGameplay));
            }

            public override void Added(Scene scene) {
                base.Added(scene);
                level = SceneAs<Level>();
            }

            public override void Awake(Scene scene) {
                base.Awake(scene);
                level = SceneAs<Level>();

                // Falling blocks do an extra step like this, so we need to do the same
                Calc.PushRandom(parent.blockSeed);
                var doubleSeed = Calc.Random.Next();
                Calc.PushRandom(doubleSeed);

                var tileGrid = GFX.FGAutotiler.GenerateBox(parent.tileType, parent.width, parent.height).TileGrid;
                Calc.PopRandom();
                Calc.PopRandom();
                subHudTileGrid = new(tileGrid.TileWidth, tileGrid.TileHeight, tileGrid.TilesX, tileGrid.TilesY, scale) {
                    Tiles = tileGrid.Tiles
                };
                if (parent.spikesTop) {
                    List<MTexture> subtextures = GetSpikeSubtextures(Spikes.Directions.Up);
                    for (int i = 0; i < parent.width; i++) {
                        var image = new SubHudImage(Calc.Random.Choose(subtextures));
                        AdjustSpikeImage(Spikes.Directions.Up, i, image);
                        spikeImages.Add(image);
                        Add(image);
                    }
                }
                if (parent.spikesBottom) {
                    List<MTexture> subtextures = GetSpikeSubtextures(Spikes.Directions.Down);
                    for (int i = 0; i < parent.width; i++) {
                        var image = new SubHudImage(Calc.Random.Choose(subtextures));
                        AdjustSpikeImage(Spikes.Directions.Down, i, image);
                        spikeImages.Add(image);
                        Add(image);
                    }
                }
                if (parent.spikesLeft) {
                    List<MTexture> subtextures = GetSpikeSubtextures(Spikes.Directions.Left);
                    for (int i = 0; i < parent.height; i++) {
                        var image = new SubHudImage(Calc.Random.Choose(subtextures));
                        AdjustSpikeImage(Spikes.Directions.Left, i, image);
                        spikeImages.Add(image);
                        Add(image);
                    }
                }
                if (parent.spikesRight) {
                    List<MTexture> subtextures = GetSpikeSubtextures(Spikes.Directions.Right);
                    for (int i = 0; i < parent.height; i++) {
                        var image = new SubHudImage(Calc.Random.Choose(subtextures));
                        AdjustSpikeImage(Spikes.Directions.Right, i, image);
                        spikeImages.Add(image);
                        Add(image);
                    }
                }
                Add(subHudTileGrid);
            }

            private void AdjustSpikeImage(Spikes.Directions dir, int idx, Image image) {
                image.Scale = new(scale);
                switch (dir) {
                    case Spikes.Directions.Up:
                        image.JustifyOrigin(0.5f, 1f);
                        image.Position = Vector2.UnitX * (idx + 0.5f) * 8f * scale + Vector2.UnitY * scale;
                        return;
                    case Spikes.Directions.Down:
                        image.JustifyOrigin(0.5f, 0f);
                        image.Position = Vector2.UnitX * (idx + 0.5f) * 8f * scale + Vector2.UnitY * scale * (-1 + parent.height * 8);
                        return;
                    case Spikes.Directions.Right:
                        image.JustifyOrigin(0f, 0.5f);
                        image.Position = Vector2.UnitY * (idx + 0.5f) * 8f * scale + Vector2.UnitX * scale * (-1 + parent.width * 8);
                        return;
                    case Spikes.Directions.Left:
                        image.JustifyOrigin(1f, 0.5f);
                        image.Position = Vector2.UnitY * (idx + 0.5f) * 8f * scale + Vector2.UnitX * scale;
                        return;
                }
            }

            private void UpdateGraphicsScale() {
                if (subHudTileGrid == null) return;

                subHudTileGrid.Scale = Scale;

                // This makes some assumptions about how the images are set
                // Will break if the order changes at all
                int spikeIdx = 0;
                if (parent.spikesTop) {
                    for (; spikeIdx < parent.width; spikeIdx++) {
                        AdjustSpikeImage(Spikes.Directions.Up, spikeIdx, spikeImages[spikeIdx]);
                    }
                }
                if (parent.spikesBottom) {
                    for (int i = 0; i < parent.width; i++, spikeIdx++) {
                        AdjustSpikeImage(Spikes.Directions.Down, i, spikeImages[spikeIdx]);
                    }
                }
                if (parent.spikesLeft) {
                    for (int i = 0; i < parent.height; i++, spikeIdx++) {
                        AdjustSpikeImage(Spikes.Directions.Left, i, spikeImages[spikeIdx]);
                    }
                }
                if (parent.spikesRight) {
                    for (int i = 0; i < parent.height; i++, spikeIdx++) {
                        AdjustSpikeImage(Spikes.Directions.Right, i, spikeImages[spikeIdx]);
                    }
                }
            }

            private List<MTexture> GetSpikeSubtextures(Spikes.Directions dir) {
                return GFX.Game.GetAtlasSubtextures("danger/spikes/" + parent.spikeType + "_" + dir.ToString().ToLower());
            }

            public override void Render() {
                if (!SubHudRenderer.DrawToBuffer) {
                    SubHudRenderer.EndRender();

                    BoardSelect.BeginRender(false);
                    Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, BoardSelect.renderBehindPlayerShader, Matrix.Identity);

                    RenderEntity();

                    SubHudRenderer.EndRender();
                    SubHudRenderer.BeginRender();
                }
            }

            private void RenderEntity() {
                Position = (PositionOverrideOverride?.Invoke() ?? PositionOverride ?? parent.Position) + renderOffset;
                // Both components handle the subhud aspect on their own, so we just need to copy the crystal position
                base.Render();
            }

            private void AfterGameplay() {
                if (SubHudRenderer.DrawToBuffer) {
                    var oldUsage = Engine.Graphics.GraphicsDevice.PresentationParameters.RenderTargetUsage;
                    Engine.Graphics.GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
                    BoardSelect.BeginRender(true);

                    Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, BoardSelect.renderBehindPlayerShader, Matrix.Identity);

                    RenderEntity();

                    SubHudRenderer.EndRender();

                    Engine.Graphics.GraphicsDevice.SetRenderTarget(GameplayBuffers.Gameplay);
                    Engine.Graphics.GraphicsDevice.PresentationParameters.RenderTargetUsage = oldUsage;
                }
            }
        }

        // Copied from StrawberrySeed.P_Burst
        private static readonly ParticleType DisappearParticle = new ParticleType {
            Source = GFX.Game["particles/shatter"],
            Color = Color.LightSkyBlue,
            Color2 = Color.SkyBlue,
            ColorMode = ParticleType.ColorModes.Fade,
            LifeMin = 0.4f,
            LifeMax = 0.5f,
            Size = 1.3f,
            SizeRange = 0.3f,
            ScaleOut = true,
            Direction = 0f,
            DirectionRange = 0f,
            SpeedMin = 100f,
            SpeedMax = 140f,
            SpeedMultiplier = 1E-05f,
            RotationMode = ParticleType.RotationModes.SameAsDirection
        };

        public EntityID Id { get; private set; }
        private Vector2 respawnPosition;

        private int previewScale;

        private int width;
        private int height;

        private char tileType;

        private string spikeType;
        private bool spikesTop;
        private bool spikesBottom;
        private bool spikesLeft;
        private bool spikesRight;

        private BlockCrystalSubHUD subHudEntity;
        private int blockSeed;

        private MinigameEntity minigame;
        public string MinigameOwnerRole { get; private set; }

        public BlockCrystal(EntityData data, Vector2 offset, EntityID id) : base(data, offset) {
            Id = id;
            respawnPosition = Position;

            blockSeed = Calc.Random.Next();

            width = data.Int("tileWidth", 2);
            height = data.Int("tileHeight", 2);
            tileType = data.Char("tiletype", '3');
            spikeType = data.Attr("spikeType", "default");
            spikesTop = data.Bool("spikesTop");
            spikesBottom = data.Bool("spikesBottom");
            spikesLeft = data.Bool("spikesLeft");
            spikesRight = data.Bool("spikesRight");
            previewScale = data.Int("previewScale", 3);
            MinigameOwnerRole = data.Attr("minigameOwnerRole", null);
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            Add(new MultiplayerHandlerComponent<BlockCrystalUpdate>($"blockCrystalUpdates-{Id.ID}", HandleUpdateData));
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            minigame = Scene.Tracker.GetEntity<MinigameEntity>();
            Scene.Add(subHudEntity = new BlockCrystalSubHUD(this, previewScale));
        }

        public override void Update() {
            base.Update();
            if (string.IsNullOrWhiteSpace(MinigameOwnerRole) || (minigame?.Data.HasRole(GameData.Instance.realPlayerID, MinigameOwnerRole) ?? true)) {
                if (Position != previousPosition) {
                    MultiplayerSingleton.Instance.Send(new BlockCrystalUpdate {
                        crystalId = Id.ID,
                        position = Position,
                        spawning = false
                    });
                }
            } else {
                // Someone else commands the crystal
                Active = false;
            }
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            subHudEntity.RemoveSelf();
        }

        // If the block in this crystal can spawn at the given location
        // Used to avoid blocks overlapping and getting stuck
        public bool CanSpawn(Vector2 topCenter) {
            var blockPos = GetBlockPos(topCenter);
            return !Scene.CollideCheck<Solid>(new Rectangle((int)blockPos.X, (int)blockPos.Y, width * 8, height * 8));
        }

        public void SpawnBlock(Entity spawner, Vector2 topCenter) {
            // Spawn block invisible and stationary to stop other crystals activating during the animation time
            // This is also why it needs to be in this function and not the coroutine
            var blockPos = GetBlockPos(topCenter);
            var pixelWidth = width * 8;
            var pixelHeight = height * 8;

            Calc.PushRandom(blockSeed);
            var block = new FallingBlock(blockPos, tileType, pixelWidth, pixelHeight, false, false, false);
            Calc.PopRandom();
            DynamicData.For(block).Set("MadelineParty_fallFast", true);
            block.Add(new ConveyorMover() {
                OnMove = amt => {
                    block.MoveHCollideSolids(amt * Engine.DeltaTime, false, (dir, _, p) => {
                        BreakBlock(block, dir.X < 0 ? block.CenterLeft : block.CenterRight, -dir);
                    });
                }
            });

            // Spikes too
            var spikes = new List<Spikes>();
            if (spikesTop) {
                spikes.Add(new Spikes(blockPos, pixelWidth, Spikes.Directions.Up, spikeType));
            }
            if (spikesBottom) {
                spikes.Add(new Spikes(blockPos + new Vector2(0, pixelHeight), pixelWidth, Spikes.Directions.Down, spikeType));
            }
            if (spikesLeft) {
                spikes.Add(new Spikes(blockPos, pixelHeight, Spikes.Directions.Left, spikeType));
            }
            if (spikesRight) {
                spikes.Add(new Spikes(blockPos + new Vector2(pixelWidth, 0), pixelHeight, Spikes.Directions.Right, spikeType));
            }

            foreach (var spike in spikes) {
                spike.Visible = false;
                Scene.Add(spike);
            }

            block.Visible = false;
            Scene.Add(block);

            spawner.Add(new Coroutine(SpawnAnimationRoutine(topCenter, block, spikes)));
        }

        private IEnumerator SpawnAnimationRoutine(Vector2 topCenter, FallingBlock block, List<Spikes> spikes) {
            var blockPreview = new BlockCrystalSubHUD(this, 0);
            Scene.Add(blockPreview);

            // Scale in
            var animateTime = 0.3f;
            var t = 0f;
            Vector2 overridePos = default;
            while (t <= 1) {
                blockPreview.Scale = Calc.LerpClamp(0, 6f, Ease.CubeOut(t));
                overridePos = topCenter + new Vector2(-0.5f, 8 + height * 4 * (0.5f + blockPreview.Scale / 12f));
                blockPreview.PositionOverride = overridePos;
                yield return null;
                t += Engine.DeltaTime / animateTime;
            }

            // Show and activate block/spikes
            block.Visible = true;
            block.Triggered = true;
            foreach (var spike in spikes) {
                spike.Visible = true;
            }

            // Fade out preview to make lighting change slightly less sudden
            var fadeTime = 0.2f;
            t = 0f;
            while (t <= 1) {
                blockPreview.PositionOverrideOverride = () => overridePos + block.Shake;
                blockPreview.Alpha = Calc.LerpClamp(1, 0, t);
                yield return null;
                t += Engine.DeltaTime / fadeTime;
            }

            blockPreview.RemoveSelf();

            // No reappear animation, in my minigame I just spawn them offscreen
            Position = respawnPosition;
            Active = true;
            Collidable = true;
            Visible = true;
            subHudEntity.Visible = true;
            Speed = Vector2.Zero;
        }

        private void BreakBlock(FallingBlock block, Vector2 from, Vector2 direction, bool playSound = true, bool playDebrisSound = true) {
            if (playSound) {
                if (block.TileType == '1') {
                    Audio.Play("event:/game/general/wall_break_dirt", block.Position);
                } else if (block.TileType == '3') {
                    Audio.Play("event:/game/general/wall_break_ice", block.Position);
                } else if (block.TileType == '9') {
                    Audio.Play("event:/game/general/wall_break_wood", block.Position);
                } else {
                    Audio.Play("event:/game/general/wall_break_stone", block.Position);
                }
            }
            for (int i = 0; i < block.Width / 8f; i++) {
                for (int j = 0; j < block.Height / 8f; j++) {
                    Scene.Add(Engine.Pooler.Create<Debris>().Init(block.Position + new Vector2(4 + i * 8, 4 + j * 8), block.TileType, playDebrisSound).BlastFrom(from));
                }
            }
            block.Collidable = false;
            block.DestroyStaticMovers();
            block.RemoveSelf();
        }

        private Vector2 GetBlockPos(Vector2 topCenter) {
            return topCenter - new Vector2(width * 4, 0);
        }

        public void Disappear() {
            for (int i = 0; i < 9; i++) {
                float dir = Calc.Random.NextFloat(MathF.PI * 2f);
                SceneAs<Level>().ParticlesFG.Emit(DisappearParticle, 1, Position + Calc.AngleToVector(dir, 5f), Vector2.Zero, dir);
            }

            Visible = false;
            subHudEntity.Visible = false;
        }

        private void HandleUpdateData(MPData data) {
            if (data is not BlockCrystalUpdate update) return;

            // If another player in our party has sent an update for this crystal
            if (update.crystalId == Id.ID
                && Collidable
                && GameData.Instance.celestenetIDs.Contains(update.ID)
                && update.ID != MultiplayerSingleton.Instance.CurrentPlayerID()) {

                Position = update.position;
                if (update.spawning) {
                    var crystalRealizer = Scene.Tracker.GetEntity<BlockCrystalRealizer>();
                    crystalRealizer.OnBlockCrystalCollide(this);
                }
            }
        }

        public static void Load() {
            On.Celeste.FallingBlock.PlayerWaitCheck += FallingBlock_PlayerWaitCheck;
        }

        private static bool FallingBlock_PlayerWaitCheck(On.Celeste.FallingBlock.orig_PlayerWaitCheck orig, FallingBlock self) {
            var selfData = DynamicData.For(self);
            if (selfData.Get<bool?>("MadelineParty_fallFast") ?? false) {
                return false;
            }
            return orig(self);
        }

        public static void Unload() {
            On.Celeste.FallingBlock.PlayerWaitCheck -= FallingBlock_PlayerWaitCheck;
        }
    }
}
