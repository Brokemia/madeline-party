using System;
using System.Collections.Generic;
using Celeste;
using Celeste.Mod;
using MadelineParty.Multiplayer;
using MadelineParty.Multiplayer.General;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty {

    public class NumberSelect : Solid {
        public class NumberPlus : Solid {
            public MTexture texture;

            public NumberPlus(Vector2 position, NumberSelect parent) : base(position, 16, 16, true) {
                OnDashCollide = parent.OnPlus;
                SurfaceSoundIndex = SurfaceIndex.TileToIndex['3'];
                texture = GFX.Game["objects/madelineparty/numberselect/plus"];
                AddTag(Tags.PauseUpdate);
                AddTag(Tags.FrozenUpdate);
            }

            public override void Render() {
                texture.Draw(Position);
                base.Render();
            }
        }

        public class NumberMinus : Solid {
            public MTexture texture;

            public NumberMinus(Vector2 position, NumberSelect parent) : base(position, 16, 16, true) {
                OnDashCollide = parent.OnMinus;
                SurfaceSoundIndex = SurfaceIndex.TileToIndex['3'];
                texture = GFX.Game["objects/madelineparty/numberselect/minus"];
                AddTag(Tags.PauseUpdate);
                AddTag(Tags.FrozenUpdate);
            }

            public override void Render() {
                texture.Draw(Position);
                base.Render();
            }
        }

        private const string texturePrefix = "objects/madelineparty/numberselect/number";

        protected NumberPlus plus;
        protected NumberMinus minus;

        public int Value => possibleValues[valueIdx];

        protected int valueIdx = 0;

        private int[] possibleValues;

        private static Dictionary<int, MTexture> textures = new();

        public NumberSelect(Vector2 position, Vector2[] nodes, int[] possibleValues)
            : base(position, 32, 32, true) {
            plus = new NumberPlus(nodes[0], this);
            minus = new NumberMinus(nodes[1], this);

            this.possibleValues = possibleValues;
            foreach(int val in possibleValues) {
                if(!textures.ContainsKey(val)) {
                    textures[val] = GFX.Game[texturePrefix + val];
                }
            }
            OnDashCollide = OnDashed;
            SurfaceSoundIndex = SurfaceIndex.TileToIndex['3'];
            AddTag(Tags.PauseUpdate);
            AddTag(Tags.FrozenUpdate);
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            scene.Add(plus);
            scene.Add(minus);
        }

        public override void Render() {
            textures[possibleValues[valueIdx]].Draw(Position);
            base.Render();
        }

        // Goes to the next value (wrapping if needed) and returns what the new value is
        protected int IncremementValue() {
            valueIdx++;
            if(valueIdx >= possibleValues.Length) {
                valueIdx = 0;
            }
            return possibleValues[valueIdx];
        }

        // Goes to the next value (wrapping if needed) and returns what the new value is
        protected int DecremementValue() {
            valueIdx--;
            if (valueIdx < 0) {
                valueIdx = possibleValues.Length - 1;
            }
            return possibleValues[valueIdx];
        }

        protected virtual DashCollisionResults OnDashed(Player player, Vector2 direction) {
            return DashCollisionResults.Rebound;
        }

        protected virtual DashCollisionResults OnMinus(Player player, Vector2 direction) {
            return DashCollisionResults.Rebound;
        }

        protected virtual DashCollisionResults OnPlus(Player player, Vector2 direction) {
            return DashCollisionResults.Rebound;
        }
    }
}