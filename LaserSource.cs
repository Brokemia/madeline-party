using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadelineParty {
    [CustomEntity("madelineparty/laserSource")]
    [Tracked]
    public class LaserSource : Entity {
        private const float ActiveTime = 0.12f;

        private const float CollideCheckSep = 4f;

        // Per side, so 2 means a total of 5 counting the middle
        private const int CollideCheckCount = 2;

        private const float BeamLength = 1000f;

        private const int BeamsDrawn = 15;

        private Sprite beamSprite;

        private Sprite beamStartSprite;

        private float angle;

        private float beamAlpha;

        private float sideFadeAlpha;

        private VertexPositionColor[] fade = new VertexPositionColor[24];

        public int laserID;

        private bool active;

        public Vector2 BeamOrigin => Position;

        public LaserSource(EntityData data, Vector2 offset) : base(data.Position + offset) {
            laserID = data.Int("laserID");
            angle = data.Float("angle").ToRad();
            Add(beamSprite = GFX.SpriteBank.Create("badeline_beam"));
            //beamSprite.SetAnimationFrame(Calc.Random.Next(beamSprite.CurrentAnimationTotalFrames));
            beamSprite.OnLastFrame = delegate (string anim) {
                if (anim == "shoot") {
                    active = false;
                }
            };
            Add(beamStartSprite = GFX.SpriteBank.Create("badeline_beam_start"));
            //beamStartSprite.SetAnimationFrame(Calc.Random.Next(beamStartSprite.CurrentAnimationTotalFrames));
            beamSprite.Visible = false;
            Depth = -1000000;
            AddTag(Tags.PauseUpdate);
            AddTag(Tags.FrozenUpdate);
        }

        public void Lase(float delay) {
            Add(new Coroutine(LaseRoutine(delay)));
        }

        private IEnumerator LaseRoutine(float delay) {
            active = true;
            beamAlpha = 0f;
            sideFadeAlpha = 0f;
            beamSprite.Play("charge", true, true);

            var chargeTimer = delay - ActiveTime;
            while (chargeTimer > 0) {
                sideFadeAlpha = Calc.Approach(sideFadeAlpha, 1f, Engine.DeltaTime);
                chargeTimer -= Engine.DeltaTime;
                yield return null;
            }
            beamSprite.Play("lock");
            yield return FinalBossBeam.ChargeTime - FinalBossBeam.FollowTime;
            SceneAs<Level>().DirectionalShake(Calc.AngleToVector(angle, 1f), 0.15f);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            DissipateParticles();

            var activeTimer = ActiveTime;
            while (activeTimer > 0f) {
                sideFadeAlpha = Calc.Approach(sideFadeAlpha, 0f, Engine.DeltaTime * 8f);
                if (beamSprite.CurrentAnimationID != "shoot") {
                    beamSprite.Play("shoot");
                    beamStartSprite.Play("shoot", restart: true);
                }
                activeTimer -= Engine.DeltaTime;
                if (activeTimer > 0f) {
                    PlayerCollideCheck();
                }
            }
        }

        public override void Update() {
            base.Update();
            beamAlpha = Calc.Approach(beamAlpha, 1f, 4f * Engine.DeltaTime);
        }

        private void DissipateParticles() {
            Level level = SceneAs<Level>();
            Vector2 screenCenter = level.Camera.Position + new Vector2(160f, 90f);
            Vector2 closeTarget = BeamOrigin + Calc.AngleToVector(angle, 12f);
            Vector2 farTarget = BeamOrigin + Calc.AngleToVector(angle, BeamLength);
            Vector2 tangential = (farTarget - closeTarget).Perpendicular().SafeNormalize();
            Vector2 normal = (farTarget - closeTarget).SafeNormalize();
            Vector2 min = -tangential * 1f;
            Vector2 max = tangential * 1f;
            float direction = tangential.Angle();
            float direction2 = (-tangential).Angle();
            float num = Vector2.Distance(screenCenter, closeTarget) - 12f;
            screenCenter = Calc.ClosestPointOnLine(closeTarget, farTarget, screenCenter);
            for (int i = 0; i < (int)BeamLength / 10; i += 12) {
                for (int j = -1; j <= 1; j += 2) {
                    level.ParticlesFG.Emit(FinalBossBeam.P_Dissipate, screenCenter + normal * i + tangential * 2f * j + Calc.Random.Range(min, max), direction);
                    level.ParticlesFG.Emit(FinalBossBeam.P_Dissipate, screenCenter + normal * i - tangential * 2f * j + Calc.Random.Range(min, max), direction2);
                    if (i != 0 && i < num) {
                        level.ParticlesFG.Emit(FinalBossBeam.P_Dissipate, screenCenter - normal * i + tangential * 2f * j + Calc.Random.Range(min, max), direction);
                        level.ParticlesFG.Emit(FinalBossBeam.P_Dissipate, screenCenter - normal * i - tangential * 2f * j + Calc.Random.Range(min, max), direction2);
                    }
                }
            }
        }

        private void PlayerCollideCheck() {
            Vector2 closeTarget = BeamOrigin + Calc.AngleToVector(angle, 12f);
            Vector2 farTarget = BeamOrigin + Calc.AngleToVector(angle, BeamLength);
            Vector2 tangential = (farTarget - closeTarget).Perpendicular().SafeNormalize(CollideCheckSep);
            
            Player player = Scene.CollideFirst<Player>(closeTarget, farTarget);
            for(int i = 1; i <= CollideCheckCount && player == null; i++) {
                player ??= Scene.CollideFirst<Player>(closeTarget + tangential * i, farTarget + tangential * i);
                player ??= Scene.CollideFirst<Player>(closeTarget - tangential * i, farTarget - tangential * i);
            }
            
            player?.Die((player.Center - BeamOrigin).SafeNormalize())?.AddTag(Tags.PauseUpdate | Tags.FrozenUpdate);
        }

        public override void Render() {
            if(!active) {
                return;
            }
            Vector2 beamOrigin = BeamOrigin;
            Vector2 vector = Calc.AngleToVector(angle, beamSprite.Width);
            beamSprite.Rotation = angle;
            beamSprite.Color = Color.White * beamAlpha;
            beamStartSprite.Rotation = angle;
            beamStartSprite.Color = Color.White * beamAlpha;
            if (beamSprite.CurrentAnimationID == "shoot") {
                beamOrigin += Calc.AngleToVector(angle, 8f);
            }
            for (int i = 0; i < BeamsDrawn; i++) {
                beamSprite.RenderPosition = beamOrigin;
                beamSprite.Render();
                beamOrigin += vector;
            }
            if (beamSprite.CurrentAnimationID == "shoot") {
                beamStartSprite.RenderPosition = BeamOrigin;
                beamStartSprite.Render();
            }
        }
    }
}
