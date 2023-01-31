using Celeste;
using Celeste.Mod;
using Celeste.Mod.Entities;
using Celeste.Mod.UI;
using MadelineParty.Multiplayer;
using MadelineParty.Multiplayer.General;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using static MadelineParty.BoardController;

namespace MadelineParty {
    [Tracked]
    [CustomEntity("madelineparty/boardSelect")]
    public class BoardSelect : NumberSelect {
        private readonly Dictionary<string, List<BoardSpace>> boardSpaces = new();
        private readonly Dictionary<string, List<EntityData>> boardEntityData = new();
        private readonly Dictionary<string, LevelData> boardLevels = new();
        private readonly List<string> boardOptions;
        private Level level;
        private readonly Dictionary<string, VirtualRenderTarget> lineRenderTargets = new();
        public static VirtualRenderTarget playerTarget;
        public static Effect renderBehindPlayerShader;

        public BoardSelect(EntityData data, Vector2 offset) : base(data.Position + offset, data.NodesOffset(offset), Enumerable.Range(0, data.Attr("boards").Split(',').Length).ToArray()) {
            boardOptions = data.Attr("boards").Split(',').ToList();
            plus.texture = GFX.Game["objects/madelineparty/numberselect/arrow_left"];
            minus.texture = GFX.Game["objects/madelineparty/numberselect/arrow_right"];
            AddTag(TagsExt.SubHUD);
            Add(new RenderPlayerToBuffer(AfterGameplay));
            Collider = null;
        }

        public static void LoadContent() {
            playerTarget = VirtualContent.CreateRenderTarget("madelineparty-board-select-player-target", 320, 180);
            renderBehindPlayerShader = new Effect(Engine.Graphics.GraphicsDevice, Everest.Content.Get("Effects/MadelineParty/behindPlayer.cso").Data);
        }

        public static void Load() {
            On.Celeste.Player.Render += Player_Render;
            On.Celeste.GameplayRenderer.Render += GameplayRenderer_Render;
        }

        private static void GameplayRenderer_Render(On.Celeste.GameplayRenderer.orig_Render orig, GameplayRenderer self, Scene scene) {
            orig(self, scene);
            foreach (RenderPlayerToBuffer component in scene.Tracker.GetComponents<RenderPlayerToBuffer>()) {
                component.AfterGameplay();
            }
        }

        private static void Player_Render(On.Celeste.Player.orig_Render orig, Player self) {
            if (self.Scene.Tracker.GetComponent<RenderPlayerToBuffer>() != null) {
                GameplayRenderer.End();

                var oldTargets = Engine.Graphics.GraphicsDevice.GetRenderTargets();
                Engine.Graphics.GraphicsDevice.SetRenderTarget(playerTarget);
                Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);
                GameplayRenderer.Begin();
                orig(self);
                Draw.SpriteBatch.End();
                Engine.Graphics.GraphicsDevice.SetRenderTargets(oldTargets);
                
                GameplayRenderer.Begin();
                Draw.SpriteBatch.Draw(playerTarget, GameplayRenderer.instance.Camera.Position, Color.White);
            } else {
                orig(self);
            }
        }

        public static void Unload() {
            On.Celeste.Player.Render -= Player_Render;
            On.Celeste.GameplayRenderer.Render -= GameplayRenderer_Render;
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            level = SceneAs<Level>();
            foreach (string option in boardOptions) {
                boardLevels[option] = level.Session.MapData.Get("Board_" + option);
                boardEntityData[option] = boardLevels[option].Entities.FindAll(d => d.Name.StartsWith("madelineparty/_board"));
                LoadBoardSpaces(option);
            }
            MultiplayerSingleton.Instance.RegisterUniqueHandler<PlayerChoice>("BoardSelect", HandlePlayerChoice);
            GameData.Instance.board = "Board_" + boardOptions[Value];
        }

        public void AfterGameplay() {
            if (!lineRenderTargets.ContainsKey(boardOptions[Value])) {
                lineRenderTargets[boardOptions[Value]] = VirtualContent.CreateRenderTarget("madelineparty-board-select-lines-" + boardOptions[Value], 160, 160);
                Engine.Graphics.GraphicsDevice.SetRenderTarget(lineRenderTargets[boardOptions[Value]]);
                Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);
                Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null,
                    Matrix.Identity);
                foreach (BoardSpace space in boardSpaces[boardOptions[Value]]) {
                    Vector2 spacePos = new Vector2(space.x, space.y) * 2;
                    foreach (BoardSpace dest in space.GetDestinations(boardSpaces[boardOptions[Value]])) {
                        Draw.Line(spacePos, new Vector2(dest.x, dest.y) * 2, pathColor);
                    }
                }
                Draw.SpriteBatch.End();
            }
            
            Engine.Graphics.GraphicsDevice.Textures[1] = playerTarget;
            var oldUsage = Engine.Graphics.GraphicsDevice.PresentationParameters.RenderTargetUsage;
            Engine.Graphics.GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
            Engine.Graphics.GraphicsDevice.SetRenderTarget(SubHudRenderer.Buffer);
            Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullNone, renderBehindPlayerShader, Matrix.Identity);

            Vector2 renderPos = (Position - level.Camera.Position) * 6;
            Draw.SpriteBatch.Draw(lineRenderTargets[boardOptions[Value]], renderPos + new Vector2(3, 0), lineRenderTargets[boardOptions[Value]].Bounds, pathOutlineColor, 0, Vector2.Zero, 3, SpriteEffects.None, 0);
            Draw.SpriteBatch.Draw(lineRenderTargets[boardOptions[Value]], renderPos - new Vector2(3, 0), lineRenderTargets[boardOptions[Value]].Bounds, pathOutlineColor, 0, Vector2.Zero, 3, SpriteEffects.None, 0);
            Draw.SpriteBatch.Draw(lineRenderTargets[boardOptions[Value]], renderPos + new Vector2(0, 3), lineRenderTargets[boardOptions[Value]].Bounds, pathOutlineColor, 0, Vector2.Zero, 3, SpriteEffects.None, 0);
            Draw.SpriteBatch.Draw(lineRenderTargets[boardOptions[Value]], renderPos - new Vector2(0, 3), lineRenderTargets[boardOptions[Value]].Bounds, pathOutlineColor, 0, Vector2.Zero, 3, SpriteEffects.None, 0);
            Draw.SpriteBatch.Draw(lineRenderTargets[boardOptions[Value]], renderPos, lineRenderTargets[boardOptions[Value]].Bounds, pathColor, 0, Vector2.Zero, 3, SpriteEffects.None, 0);

            foreach (BoardSpace space in boardSpaces[boardOptions[Value]]) {
                spaceTextures.OrDefault(space.type, null)?.DrawCentered((Position - level.Camera.Position + new Vector2(space.x, space.y)) * 6, Color.White, 3);
            }

            ActiveFont.DrawOutline(Dialog.Clean("MadelineParty_Board_Name_" + boardOptions[Value]), (Position - level.Camera.Position + new Vector2(40, 85f)) * 6, new Vector2(0.5f), new Vector2(0.7f), Color.White, 2, Color.Black);

            SubHudRenderer.EndRender();
            Engine.Graphics.GraphicsDevice.SetRenderTarget(GameplayBuffers.Gameplay);
            Engine.Graphics.GraphicsDevice.PresentationParameters.RenderTargetUsage = oldUsage;
        }

        private void LoadBoardSpaces(string board) {
            boardSpaces[board] = new();
            foreach (EntityData data in boardEntityData[board]) {
                Vector2 pos = (data.Position - new Vector2(80, 0)) / 2;
                boardSpaces[board].Add(new BoardSpace {
                    ID = data.ID,
                    type = data.Enum("type", BoardSpaceType.Blue),
                    x = (int)pos.X,
                    y = (int)pos.Y,
                    heartSpace = data.Bool("heart_space", false),
                    greenSpaceEvent = data.Attr("event_ID"),
                    destIDs_DONTUSE = new()
                });
                foreach (var node in data.Nodes) {
                    boardSpaces[board].Last().destIDs_DONTUSE.Add(
                        boardEntityData[board].OrderBy(e => (e.Position - node).LengthSquared())
                                       .First(e => e.ID != boardSpaces[board].Last().ID)
                                       .ID);
                }
            }
        }

        protected override DashCollisionResults OnPlus(Player player, Vector2 direction) {
            Audio.Play("event:/game/general/wall_break_ice", Position);

            IncremementValue();
            GameData.Instance.board = "Board_" + boardOptions[Value];
            MultiplayerSingleton.Instance.Send(new PlayerChoice { choiceType = "BOARDSELECT", choice = valueIdx });
            return base.OnPlus(player, direction);
        }

        protected override DashCollisionResults OnMinus(Player player, Vector2 direction) {
            Audio.Play("event:/game/general/wall_break_ice", Position);

            DecremementValue();
            GameData.Instance.board = "Board_" + boardOptions[Value];
            MultiplayerSingleton.Instance.Send(new PlayerChoice { choiceType = "BOARDSELECT", choice = valueIdx });
            return base.OnMinus(player, direction);
        }

        private void HandlePlayerChoice(MPData data) {
            if (data is not PlayerChoice playerChoice) return;
            // If another player in our party has changed the turn count
            if (GameData.Instance.celestenetIDs.Contains(playerChoice.ID) && playerChoice.ID != MultiplayerSingleton.Instance.CurrentPlayerID() && playerChoice.choiceType.Equals("BOARDSELECT")) {
                valueIdx = playerChoice.choice;
                GameData.Instance.board = "Board_" + boardOptions[Value];
            }
        }
    }
}
