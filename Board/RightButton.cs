using System;
using Celeste;
using MadelineParty.Multiplayer;
using MadelineParty.Multiplayer.General;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty.Board
{

    public class RightButton : Solid, IComparable
    {
        private const string decalPrefix = "madelineparty/rightbutton/";

        public enum Modes
        {
            Cancel,
            // TODO remove in favor of general Cancel mode paired with OnPressButton
            CancelHeartBuy,
            CancelShopEnter,
            CancelItemBuy,
            UseItem,
            SingleItem,
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
        protected int playerID;

        protected Decal associatedDecal = null;
        protected BoardController board = null;

        private Level level;

        public event Action<Modes> OnPressButton;

        public RightButton(Vector2 position, float width, float height, Modes startingMode, int playerID)
            : base(position, width, height, safe: true)
        {
            this.width = width;
            this.height = height;
            this.playerID = playerID;
            currentMode = startingMode;
            OnDashCollide = OnDashed;
            SurfaceSoundIndex = SurfaceIndex.TileToIndex[tileType];
            Depth = Depths.FGDecals - 1;
            AddTag(Tags.PauseUpdate);
            AddTag(Tags.FrozenUpdate);
            AddTag(TagsExt.SubHUD);
        }

        public RightButton(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Height, data.Enum("startingMode", Modes.Inactive), data.Int("playerID", 0))
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
                case Modes.CancelHeartBuy:
                case Modes.CancelItemBuy:
                case Modes.CancelShopEnter:
                    SwapDecal(decalPrefix + "cancel");
                    break;
                case Modes.SingleItem:
                    SwapDecal(decalPrefix + "useitem_" + GameData.Instance.players[playerID].Items.Find(item => item.CanUseInTurn).Name);
                    break;
                default:
                    SwapDecal(decalPrefix + currentMode.ToString().ToLower());
                    break;
            }
            if (mode == Modes.Inactive)
            {
                OnPressButton = null;
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
                    if (DistanceBetween(item, this) < DistanceBetween(associatedDecal, this))
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
        }

        private double DistanceBetween(Entity e1, Entity e2)
        {
            if (e1 == null || e2 == null)
                return double.MaxValue;
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
            if (OnPressButton != null)
            {
                var oldEvent = OnPressButton;
                OnPressButton = null;
                oldEvent(currentMode);
                return;
            }
            switch (currentMode)
            {
                case Modes.UseItem:
                    SetCurrentMode(Modes.Inactive);
                    board.UseItem(playerID);
                    break;
                case Modes.SingleItem:
                    MultiplayerSingleton.Instance.Send(new UseItem { player = playerID, itemIdx = 0 });
                    SetCurrentMode(Modes.Inactive);
                    GameData.Instance.players[playerID].Items.Find(item => item.CanUseInTurn).UseItem(playerID);
                    GameData.Instance.players[playerID].Items.RemoveAt(0);
                    break;
                case Modes.CancelHeartBuy:
                    board.SkipHeart();
                    SetCurrentMode(Modes.Inactive);
                    break;
                case Modes.CancelShopEnter:
                    board.SkipShop();
                    SetCurrentMode(Modes.Inactive);
                    break;
                case Modes.CancelItemBuy:
                    board.SkipItem();
                    break;
                case Modes.Up:
                    board.ContinueMovementAfterIntersection(BoardController.Direction.Up);
                    break;
                case Modes.Down:
                    board.ContinueMovementAfterIntersection(BoardController.Direction.Down);
                    break;
                case Modes.Left:
                    board.ContinueMovementAfterIntersection(BoardController.Direction.Left);
                    break;
                case Modes.Right:
                    board.ContinueMovementAfterIntersection(BoardController.Direction.Right);
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
                Scene.Add(associatedDecal);
            }
        }

        private DashCollisionResults OnDashed(Player player, Vector2 direction)
        {
            Break(player.Center, direction);
            return DashCollisionResults.Rebound;
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;
            return obj is RightButton other ? playerID.CompareTo(other.playerID) : 1;
        }

        public override void Render()
        {
            base.Render();
            if (currentMode == Modes.UseItem)
            {
                var pItems = GameData.Instance.players[playerID].Items.FindAll(item => item.CanUseInTurn);
                for (int i = 0; i < pItems.Count; i++)
                {
                    // 4 pixels of vertical spacing between items
                    GFX.Game["decals/madelineparty/items/" + GameData.Instance.players[playerID].Items[i].Name].DrawCentered((Position - level.LevelOffset) * 6 + new Vector2(8 * 6, 16 * 6 /* center it*/ - 18 * (pItems.Count - 1) /* to top */ + 36 * i /* descend */) - level.ShakeVector * 6, Color.White, new Vector2(2));
                }
            }
        }
    }
}