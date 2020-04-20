using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Celeste;
using MadelineParty.Ghostnet;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty {
    public class PlayerToken : Entity, IPauseUpdateGhostnetChat {
        private class TokenImage : Component {
            public PlayerToken token => (PlayerToken)base.Entity;

            public TokenImage()
                : base(active: true, visible: true) {
            }

            public override void Render() {
                token.textures[(int)token.frame].DrawCentered(token.Position, Color.White, token.scale);
            }
        }

        public Vector2 currentSpace;

        Random rand = new Random();

        public float AnimationSpeed;

        private Component image;

        private List<MTexture> textures;

        private float timeTilBlink = 10;

        private Vector2 scale;

        private float frame;

        public string Name;

        public PlayerToken(string texture, Vector2 position, Vector2 scale, int depth, Vector2 space) : base(position) {
            if (string.IsNullOrEmpty(Path.GetExtension(texture))) {
                texture += ".png";
            }
            AnimationSpeed = 12f;
            Depth = depth;
            this.scale = scale;
            string extension = Path.GetExtension(texture);
            string input = Path.Combine("madelineparty", "tokens", texture.Replace(extension, "")).Replace('\\', '/');
            Name = Regex.Replace(input, "\\d+$", string.Empty);
            textures = GFX.Gui.GetAtlasSubtextures(Name);
            AddTag(TagsExt.SubHUD);
            AddTag(Tags.PauseUpdate);
            AddTag(Tags.FrozenUpdate);
            currentSpace = space;
        }

        public override void Update() {
            if (textures.Count > 1) {
                timeTilBlink -= AnimationSpeed * Engine.DeltaTime;
                if (timeTilBlink < 0) {
                    frame += AnimationSpeed * Engine.DeltaTime;
                    if (frame >= textures.Count) {
                        frame = 0;
                        timeTilBlink = rand.NextFloat(50) + 25;
                    }
                }
            }
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            if (image == null) {
                Add(image = new TokenImage());
            }
        }
    }
}
