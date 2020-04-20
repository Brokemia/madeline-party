using System;
using Celeste;
using MadelineParty.Ghostnet;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty
{

    public class LeftButton : Solid, IComparable, IPauseUpdateGhostnetChat
    {
        private const string decalPrefix = "madelineparty/leftbutton";

        public enum Modes
        {
            ConfirmHeartBuy,
            ConfirmShopEnter,
            ConfirmItemBuy,
            Dice,
            Up,
            Down,
            Left,
            Right,
            Inactive
        }

        protected char tileType
        {
            get
            {
                switch (currentMode)
                {
                    case Modes.Inactive:
                        return '3';
                    default:
                        return '3';
                }
            }
        }

        private float width;

        private float height;

        protected bool currentlyBreakable
        {
            get
            {
                return currentMode != Modes.Inactive;
            }
        }

        protected Modes currentMode;

        protected Decal associatedDecal = null;
        protected BoardController board = null;

        private Level level;

        public LeftButton(Vector2 position, float width, float height, Modes startingMode)
            : base(position, width, height, safe: true)
        {
            this.width = width;
            this.height = height;
            currentMode = startingMode;
            OnDashCollide = OnDashed;
            SurfaceSoundIndex = SurfaceIndex.TileToIndex[tileType];
            AddTag(Tags.PauseUpdate);
            AddTag(Tags.FrozenUpdate);
        }

        public LeftButton(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Height, data.Enum<Modes>("startingMode", defaultValue: Modes.Inactive))
        {
        }

        public Modes GetCurrentMode()
        {
            return currentMode;
        }

        public void SetCurrentMode(Modes mode)
        {
            currentMode = mode;
            switch (mode)
            {
                case Modes.ConfirmHeartBuy:
                case Modes.ConfirmItemBuy:
                case Modes.ConfirmShopEnter:
                    SwapDecal(decalPrefix + "confirm");
                    break;
                default:
                    SwapDecal(decalPrefix + currentMode.ToString().ToLower());
                    break;
            }

        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            level = SceneAs<Level>();
        }

        public override void Awake(Scene scene)
        {
            base.Awake(scene);
            // Choose the closest left button decal to display to
            foreach (Decal item in scene.Entities.FindAll<Decal>())
            {
                //Console.WriteLine("Decal found: " + item.Name);
                if (item.Name.StartsWith("decals/" + decalPrefix, StringComparison.InvariantCulture))
                {
                    if(DistanceBetween(item, this) < DistanceBetween(associatedDecal, this))
                    {
                        associatedDecal = item;
                    }
                }
            }
            foreach (BoardController item in scene.Entities.FindAll<BoardController>())
            {
                board = item;
            }
            SwapDecal(decalPrefix + currentMode.ToString().ToLower());
            TileGrid tileGrid = GFX.FGAutotiler.GenerateBox(tileType, (int)width / 8, (int)height / 8).TileGrid;Add(new LightOcclude());
            Add(tileGrid);
            Add(new TileInterceptor(tileGrid, highPriority: true));
        }

        private double DistanceBetween(Entity e1, Entity e2)
        {
            if (e1 == null || e2 == null)
                return Double.MaxValue;
            return Math.Sqrt(Math.Pow(e1.CenterX - e2.CenterX, 2) + Math.Pow(e1.CenterY - e2.CenterY, 2));
        }

        public override void Removed(Scene scene)
        {
            base.Removed(scene);
            Celeste.Celeste.Freeze(0.05f);
        }

        public void Break(Vector2 from, Vector2 direction, bool playSound = true, bool playDebrisSound = true)
        {
            if (playSound)
            {
                if (tileType == '1')
                {
                    Audio.Play("event:/game/general/wall_break_dirt", Position);
                }
                else if (tileType == '3')
                {
                    Audio.Play("event:/game/general/wall_break_ice", Position);
                }
                else if (tileType == '9')
                {
                    Audio.Play("event:/game/general/wall_break_wood", Position);
                }
                else
                {
                    Audio.Play("event:/game/general/wall_break_stone", Position);
                }
            }

            DoBreakAction();
        }

        private void DoBreakAction()
        {
            switch (currentMode)
            {
                case Modes.Dice:
                    SetCurrentMode(Modes.Inactive);
                    board.RollDice(GetTokenID());
                    break;
                case Modes.ConfirmHeartBuy:
                    board.BuyHeart();
                    SetCurrentMode(Modes.Inactive);
                    break;
                case Modes.ConfirmShopEnter:
                    board.EnterShop();
                    break;
                case Modes.ConfirmItemBuy:
                    board.BuyItem();
                    SetCurrentMode(Modes.Inactive);
                    break;
                case Modes.Up:
                    board.ContinueMovementAfterIntersection(BoardController.Direction.UP);
                    break;
                case Modes.Down:
                    board.ContinueMovementAfterIntersection(BoardController.Direction.DOWN);
                    break;
                case Modes.Left:
                    board.ContinueMovementAfterIntersection(BoardController.Direction.LEFT);
                    break;
                case Modes.Right:
                    board.ContinueMovementAfterIntersection(BoardController.Direction.RIGHT);
                    break;
            }
        }

        // Swaps the associatedDecal with a different one
        // Does nothing if associatedDecal is null
        private void SwapDecal(string name)
        {
            if (associatedDecal != null)
            {
                associatedDecal.RemoveSelf();
                associatedDecal = new Decal(name, associatedDecal.Position, associatedDecal.Scale, associatedDecal.Depth);
                base.Scene.Add(associatedDecal);
            }
        }

        private DashCollisionResults OnDashed(Player player, Vector2 direction)
        {
            Break(player.Center, direction);
            return DashCollisionResults.Rebound;
        }

        // Get the ID of the token of the player using it
        public int GetTokenID()
        {
            if (X < level.LevelOffset.X + level.Bounds.Width / 2 && Y < level.LevelOffset.Y + level.Bounds.Height / 2)
            {
                return 0;
            }
            if (X > level.LevelOffset.X + level.Bounds.Width / 2 && Y < level.LevelOffset.Y + level.Bounds.Height / 2)
            {
                return 1;
            }
            if (X < level.LevelOffset.X + level.Bounds.Width / 2 && Y > level.LevelOffset.Y + level.Bounds.Height / 2)
            {
                return 2;
            }
            if (X > level.LevelOffset.X + level.Bounds.Width / 2 && Y > level.LevelOffset.Y + level.Bounds.Height / 2)
            {
                return 3;
            }

            return 0;
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;
            return obj is LeftButton other ? GetTokenID().CompareTo(other.GetTokenID()) : 1;
        }
    }
}