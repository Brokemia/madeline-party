using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using static MadelineParty.BoardController;

namespace MadelineParty {
    [Tracked]
    public class PlayerToken : Entity {
        private class TokenImage : Component {
            public PlayerToken token => (PlayerToken)Entity;

            public TokenImage()
                : base(active: true, visible: true) {
            }

            public override void Render() {
                token.textures[(int)token.frame].DrawCentered(token.Position - token.level.ShakeVector * 6, Color.White, token.scale);
            }
        }

        private Level level;

        public int id;

        public BoardSpace currentSpace;

        private Random rand = new Random();

        public float AnimationSpeed;

        private Component image;

        public List<MTexture> textures;

        private float timeTilBlink = 10;

        public Vector2 scale { get; private set; }

        public float frame { get; private set; }

        public string Name;

        private DeathEffect deathEffect;

        private float respawnEase = -1;

        public Action<PlayerToken> OnRespawn;

        private static Dictionary<string, Color> colors = new Dictionary<string, Color> {
            { "madeline/normal00", Player.NormalHairColor },
            { "badeline/normal00", Player.NormalBadelineHairColor },
            { "theo/excited00", Color.ForestGreen },
            { "granny/normal00", Color.Blue }, // It's obviously because of the bird
            { "luigi/normal00", Color.LimeGreen }
        };

        private Color color;

        public PlayerToken(int id, string texture, Vector2 position, Vector2 scale, int depth, BoardSpace space) : base(position) {
            this.id = id;
            color = colors[texture];
            if (string.IsNullOrEmpty(Path.GetExtension(texture))) {
                texture += ".png";
            }
            Collider = new Hitbox(20, 20, -5, -5);
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
            base.Update();
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

        public IEnumerator Respawn() {
            if (deathEffect == null) {
                Add(deathEffect = new DeathEffect(color));
                image.Visible = false;
                yield return deathEffect.Duration + 0.1f;
                respawnEase = 1f;
                Tween respawnTween = Tween.Create(Tween.TweenMode.Oneshot, null, 0.6f, true);
                respawnTween.OnUpdate = delegate (Tween t)
                {
                    respawnEase = 1f - t.Eased;
                };
                respawnTween.OnComplete = delegate
                {
                    respawnEase = -1f;
                    deathEffect = null;
                    image.Visible = true;
                    Vector2 normalScale = scale;
                    Add(Wiggler.Create(0.25f, 4f, (f) => scale = normalScale * new Vector2(1, f * 0.15f + 1f), true, true));
                };
                Add(respawnTween);
                OnRespawn?.Invoke(this);
            }
        }

        public override void Render() {
            base.Render();
            if (respawnEase >= 0) {
                DeathEffect.Draw(Position - level.ShakeVector * 6, color, respawnEase);
            }
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            level = SceneAs<Level>();
            if (image == null) {
                Add(image = new TokenImage());
            }
        }
    }
}
