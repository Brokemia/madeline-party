using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;

namespace MadelineParty.SubHud {
    [Tracked(false)]
    [CustomEntity("madelineparty/subHudTiles")]
    public class SubHudTiles : Entity, SubHudPixelPerfectRendered {
        public int RenderDepth => Depth;

        private SubHudTileGrid tiles;

        private char tileType;

        public List<SubHudTiles> Group;

        public Point GroupBoundsMin;

        public Point GroupBoundsMax;

        public bool HasGroup { get; private set; }

        public bool MasterOfGroup { get; private set; }

        private Vector2 subpixelOffset;

        private int width, height;

        private int scale = 2;

        private Vector2 screenOffset;

        public SubHudTiles(EntityData data, Vector2 offset) : base(data.Position + offset) {
            width = data.Width;
            height = data.Height;
            subpixelOffset = new(data.Int("subpixelX"), data.Int("subpixelY"));
            tileType = data.Char("tiletype");
            Depth = -9000;
            AddTag(TagsExt.SubHUD);

            screenOffset = Position * 6 / scale;
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            SubHudPixelPerfectRenderer.Add(scene, this);
            if (!HasGroup) {
                MasterOfGroup = true;
                Group = new();
                GroupBoundsMin = new Point((int)screenOffset.X, (int)screenOffset.Y);
                GroupBoundsMax = new Point((int)screenOffset.X + width, (int)screenOffset.Y + height);
                AddToGroupAndFindChildren(this);
                var rectangle = new Rectangle(GroupBoundsMin.X / 8, GroupBoundsMin.Y / 8, (GroupBoundsMax.X - GroupBoundsMin.X) / 8 + 1, (GroupBoundsMax.Y - GroupBoundsMin.Y) / 8 + 1);
                var virtualMap = new VirtualMap<char>(rectangle.Width, rectangle.Height, '0');
                foreach (var item in Group) {
                    int num = (int)(item.screenOffset.X / 8f) - rectangle.X;
                    int num2 = (int)(item.screenOffset.Y / 8f) - rectangle.Y;
                    int num3 = (int)(item.width / 8f);
                    int num4 = (int)(item.height / 8f);
                    for (int i = num; i < num + num3; i++) {
                        for (int j = num2; j < num2 + num4; j++) {
                            virtualMap[i, j] = tileType;
                        }
                    }
                }
                var vanillaTiles = GFX.FGAutotiler.GenerateMap(virtualMap, new Autotiler.Behaviour {
                    EdgesExtend = false,
                    EdgesIgnoreOutOfLevel = false,
                    PaddingIgnoreOutOfLevel = false
                }).TileGrid;
                tiles = new(vanillaTiles.TileWidth, vanillaTiles.TileHeight, vanillaTiles.TilesX, vanillaTiles.TilesY, scale) {
                    Tiles = vanillaTiles.Tiles,

                    Position = new Vector2(GroupBoundsMin.X - screenOffset.X, GroupBoundsMin.Y - screenOffset.Y)
                };
                Add(tiles);
            }
        }

        public void SubHudRender() {
            base.Render();
        }

        public override void Render() { }

        private void AddToGroupAndFindChildren(SubHudTiles from) {
            if (from.screenOffset.X < GroupBoundsMin.X) {
                GroupBoundsMin.X = (int)from.screenOffset.X;
            }
            if (from.screenOffset.Y < GroupBoundsMin.Y) {
                GroupBoundsMin.Y = (int)from.screenOffset.Y;
            }
            if (from.screenOffset.X + from.width > GroupBoundsMax.X) {
                GroupBoundsMax.X = (int)from.screenOffset.X + from.width;
            }
            if (from.screenOffset.Y + from.height > GroupBoundsMax.Y) {
                GroupBoundsMax.Y = (int)from.screenOffset.Y + from.height;
            }
            from.HasGroup = true;
            Group.Add(from);
            // All tiles of the same tiletype are one group
            foreach (SubHudTiles entity in Scene.Tracker.GetEntities<SubHudTiles>()) {
                if (!entity.HasGroup && entity.tileType == tileType) {
                    AddToGroupAndFindChildren(entity);
                }
            }
        }
    }
}
