// Celeste.Editor.LevelTemplate
using Celeste;
using Celeste.Editor;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod;
using System;
using System.Collections.Generic;
using static MadelineParty.BoardController;

namespace MadelineParty.Tools {
    public class BoardSpaceTemplate {
        public BoardSpace originalSpace;

        public char Type;

        public int X;

        public int Y;

        public Vector2 Position { get { return new Vector2(X, Y); } }

        public Rectangle Rect { get { return new Rectangle(X - size / 2, Y - size / 2, size, size); } }

        public bool HeartSpace;

        public const int size = 16;

        private Vector2 moveAnchor;

        public BoardSpaceTemplate(BoardSpace data) {
            originalSpace = data;
            X = data.x;
            Y = data.y;
            Type = data.type;
            HeartSpace = data.heartSpace;
        }

        public void Render(Camera camera) {
            //float t = 1f / camera.Zoom * 2f; // use for thickness of lines
            switch(Type) {
                case 'r':
                    Draw.Circle(X, Y, size / 2, Color.Red, 5);
                    break;
                case 'b':
                    Draw.Circle(X, Y, size / 2, Color.Blue, 5);
                    break;
                case 'i':
                    Draw.Rect(Rect.X, Rect.Y, size, size, Color.Blue);
                    break;
                case 's':
                    Draw.HollowRect(Rect.X, Rect.Y, size, size, Color.Yellow);
                    break;
            }
            
        }

        public bool Check(Vector2 point) {
            return Rect.Contains((int)point.X, (int)point.Y);
        }

        public bool Check(Rectangle rect) {
            return Rect.Intersects(rect);
        }

        public void StartMoving() {
            moveAnchor = new Vector2(X, Y);
        }

        public void Move(Vector2 relativeMove) {
            X = (int)(moveAnchor.X + relativeMove.X);
            Y = (int)(moveAnchor.Y + relativeMove.Y);
        }
    }
}