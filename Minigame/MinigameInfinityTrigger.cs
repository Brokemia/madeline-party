using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste;
using Celeste.Mod.CelesteNet.Client;
using Celeste.Mod.CelesteNet.Client.Components;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace MadelineParty {
    [CustomEntity("madelineparty/minigameInfinityTrigger")]
    public class MinigameInfinityTrigger : MinigameEntity {
        public static uint loops = 0;
        public static uint dist;
        public Coroutine endCoroutine;
        public MinigameDistanceDisplay display;
        public Vector2 backwardsSpot;
        public float xDiff;
        private bool everyOtherFrame;

        // Most things DreamParticle related were taken from CommunalHelper
        /*
        MIT License

        Copyright (c) 2020 Flynn Swainston-Calcutt

        Permission is hereby granted, free of charge, to any person obtaining a copy
        of this software and associated documentation files (the "Software"), to deal
        in the Software without restriction, including without limitation the rights
        to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        copies of the Software, and to permit persons to whom the Software is
        furnished to do so, subject to the following conditions:

        The above copyright notice and this permission notice shall be included in all
        copies or substantial portions of the Software.

        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
        SOFTWARE.
         */

        // Don't worry about it
        internal class NoInliningException : Exception { public NoInliningException() : base("Something went horribly wrong.") { } }

        /*
         * We want to tie the Custom DreamParticles to the vanilla DreamParticles, but since DreamBlock.DreamParticle is private it would require a lot of reflection.
         * Instead we just IL hook stuff and ignore accessibility modifiers entirely. It's fine.
         */
        protected struct DreamParticle {
            internal static Type t_DreamParticle = typeof(DreamBlock).GetNestedType("DreamParticle", BindingFlags.NonPublic);

#pragma warning disable IDE0052, CS0414, CS0649 // Remove unread private members; Field is assigned to but never read; Field is never assigned to
            // Used in IL hooks
            private readonly DreamBlock dreamBlock;
            private readonly int idx;
            private static Vector2 tempVec2;
#pragma warning restore IDE0052, CS0414, CS0649

            public Vector2 Position {
                get { UpdatePos(); return tempVec2; }
                //[MethodImpl(MethodImplOptions.NoInlining)]
                //get { Console.Error.Write("NoInlining"); throw new NoInliningException(); }
                [MethodImpl(MethodImplOptions.NoInlining)]
                set { Console.Error.Write("NoInlining"); throw new NoInliningException(); }
            }
            [MethodImpl(MethodImplOptions.NoInlining)]
            private void UpdatePos() { Console.Error.Write("NoInlining"); throw new NoInliningException(); }

            public int Layer {
                [MethodImpl(MethodImplOptions.NoInlining)]
                get { Console.Error.Write("NoInlining"); throw new NoInliningException(); }
                [MethodImpl(MethodImplOptions.NoInlining)]
                set { Console.Error.Write("NoInlining"); throw new NoInliningException(); }
            }
            public Color Color {
                [MethodImpl(MethodImplOptions.NoInlining)]
                get { Console.Error.Write("NoInlining"); throw new NoInliningException(); }
                [MethodImpl(MethodImplOptions.NoInlining)]
                set { Console.Error.Write("NoInlining"); throw new NoInliningException(); }
            }
            public float TimeOffset {
                [MethodImpl(MethodImplOptions.NoInlining)]
                get { Console.Error.Write("NoInlining"); throw new NoInliningException(); }
                [MethodImpl(MethodImplOptions.NoInlining)]
                set { Console.Error.Write("NoInlining"); throw new NoInliningException(); }
            }

            public DreamParticle(DreamBlock block, int idx)
                : this() {
                dreamBlock = block;
                this.idx = idx;
            }
        }

        private static List<IDetour> hooks_DreamParticle_Properties = new List<IDetour>();

        public static void Load() {
            foreach (PropertyInfo prop in typeof(DreamParticle).GetProperties()) {
                FieldInfo targetField = DreamParticle.t_DreamParticle.GetField(prop.Name);
                if (targetField != null) {
                    if (prop.Name == "Position") {
                        hooks_DreamParticle_Properties.Add(new ILHook(
                            typeof(DreamParticle).GetMethod("UpdatePos", BindingFlags.NonPublic | BindingFlags.Instance),
                            ctx => DreamParticle_UpdatePos(ctx, targetField)));
                    } else {
                        hooks_DreamParticle_Properties.Add(new ILHook(prop.GetGetMethod(), ctx => DreamParticle_get_Prop(ctx, targetField)));
                    }
                    hooks_DreamParticle_Properties.Add(new ILHook(prop.GetSetMethod(), ctx => DreamParticle_set_Prop(ctx, targetField)));
                }
            }
        }

        public MinigameInfinityTrigger(EntityData data, Vector2 offset) : base(data, offset) {
            backwardsSpot = data.Nodes[0];
            xDiff = Position.X - backwardsSpot.X;
        }

        protected override void AfterStart() {
            base.AfterStart();
            // Reset timer so it starts at 30 instead of (30 - the time it takes to count down)
            startTime = level.RawTimeActive;
            level.Add(display = new MinigameDistanceDisplay(this));
        }

        public override void Update() {
            base.Update();
            if (level.RawTimeActive - startTime >= 30 && endCoroutine == null) {
                Add(endCoroutine = new Coroutine(EndMinigame()));
            }

            Player player = level.Tracker.GetEntity<Player>();
            
            if (player != null && loops > 0 && player.X < backwardsSpot.X - 3 * 8) {
                loops--;
                Teleport(player, xDiff);
            }

            if(everyOtherFrame) {
                CelesteNetSendMinigameStatus(dist);
            }
            everyOtherFrame = !everyOtherFrame;

            if (player != null) {
                dist = calculateDist(loops, player.X);
                GameData.minigameStatus[GameData.realPlayerID] = dist;
            }
        }

        private uint calculateDist(uint loops, float x) {
            return (uint)Math.Max(loops * xDiff + x - backwardsSpot.X - 10, 0);
        }

        protected IEnumerator EndMinigame() {
            Player player = level.Tracker.GetEntity<Player>();
            // This check is probably unnecessary, but I left it in for safety
            while (player == null) {
                yield return null;
                player = level.Tracker.GetEntity<Player>();
            }
            completed = true;
            // Freeze the player so they can't do any more moving until everyone else is done
            player.StateMachine.State = Player.StFrozen;
            player.Speed = Vector2.Zero;
            startTime = -1;
            started = false;
            didRespawn = false;
            level.CanRetry = false;
            dist = calculateDist(loops, player.X);
            Console.WriteLine("Minigame Distance: " + dist);
            GameData.minigameResults.Add(new Tuple<int, uint>(GameData.realPlayerID, dist));
            if (MadelinePartyModule.IsCelesteNetInstalled()) {
                CelesteNetSendMinigameResults(dist);
            }

            // Wait until all players have finished
            while (GameData.minigameResults.Count < GameData.playerNumber) {
                yield return null;
            }

            GameData.minigameResults.Sort((x, y) => { return y.Item2.CompareTo(x.Item2); });

            int winnerID = GameData.minigameResults[0].Item1;
            int realPlayerPlace = GameData.minigameResults.FindIndex((obj) => obj.Item1 == GameData.realPlayerID);
            // A check to stop the game from crashing when I hit one of these while testing
            if (winnerID >= 0 && GameData.players[winnerID] != null) {
                dist = 0;
                loops = 0;
                GameData.players[winnerID].ChangeStrawberries(10);
                level.OnEndOfFrame += delegate {
                    Leader.StoreStrawberries(player.Leader);
                    level.Remove(player);
                    level.UnloadLevel();

                    level.Session.Level = "Game_PlayerRanking";
                    List<Vector2> spawns = new List<Vector2>(level.Session.LevelData.Spawns.ToArray());
                    // Sort the spawns so the highest ones are first
                    spawns.Sort((x, y) => { return x.Y.CompareTo(y.Y); });
                    level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(spawns[realPlayerPlace].X, spawns[realPlayerPlace].Y));

                    level.LoadLevel(Player.IntroTypes.None);

                    Leader.RestoreStrawberries(player.Leader);
                };
            }
        }

        public override void OnEnter(Player player) {
            loops++;
            Teleport(player, -xDiff);
        }

        private void Teleport(Player player, float dx) {
            Vector2 asVector = new Vector2(dx, 0);
            Level level = SceneAs<Level>();
            foreach (TrailManager.Snapshot snapshot in level.Tracker.GetEntities<TrailManager.Snapshot>()) {
                snapshot.X += dx;
            }
            foreach (SlashFx slash in level.Tracker.GetEntities<SlashFx>()) {
                slash.X += dx;
            }
            foreach (SpeedRing ring in level.Tracker.GetEntities<SpeedRing>()) {
                ring.X += dx;
            }
            player.X += dx;
            player.Hair.MoveHairBy(asVector);
            level.Camera.Position += asVector;
            foreach (Backdrop backdrop in level.Background.Backdrops) {
                backdrop.Position += new Vector2(dx * backdrop.Scroll.X, 0);
            }
            foreach (Backdrop backdrop in level.Foreground.Backdrops) {
                backdrop.Position += new Vector2(dx * backdrop.Scroll.X, 0);
            }

            DynData<ParticleSystem> fgParticles = new DynData<ParticleSystem>(level.ParticlesFG);
            Particle[] particles = fgParticles.Get<Particle[]>("particles");
            for(int i = 0; i < particles.Length; i++) {
                particles[i].Position += asVector;
            }
            DynData<ParticleSystem> bgParticles = new DynData<ParticleSystem>(level.ParticlesBG);
            particles = bgParticles.Get<Particle[]>("particles");
            for (int i = 0; i < particles.Length; i++) {
                particles[i].Position += asVector;
            }
            DynData<ParticleSystem> particlesParticles = new DynData<ParticleSystem>(level.Particles);
            particles = particlesParticles.Get<Particle[]>("particles");
            for (int i = 0; i < particles.Length; i++) {
                particles[i].Position += asVector;
            }

            foreach (DreamBlock db in level.Tracker.GetEntities<DreamBlock>()) {
                DynamicData dbData = new DynamicData(db);
                DynamicData particlesData = new DynamicData(dbData.Get("particles"));
                for (int i = 0; i < particlesData.Get<int>("Length"); i++) {
                    DreamParticle particleProxy = new DreamParticle(db, i);
                    //Console.WriteLine(db.Position + " " + (particleProxy.Position + (level.Camera.Position - asVector) * (0.3f + 0.25f * particleProxy.Layer)) + " " +
                        //(particleProxy.Position + (level.Camera.Position) * (0.3f + 0.25f * particleProxy.Layer)) + " " + asVector * (0.3f + 0.25f * particleProxy.Layer));
                    particleProxy.Position -= (asVector) * .5f * (0.3f + 0.25f * particleProxy.Layer);
                }
            }
        }

        #region Cursed

        private static FieldInfo f_CustomDreamParticle_dreamBlock = typeof(DreamParticle).GetField("dreamBlock", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo f_DreamBlock_particles = typeof(DreamBlock).GetField("particles", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo f_CustomDreamParticle_idx = typeof(DreamParticle).GetField("idx", BindingFlags.NonPublic | BindingFlags.Instance);

        /*
         * Position needs some extra care because of issues with methods that return Structs.
         * We use a static field (not threadsafe!) to temporarily store the variable, then return it normally in the property accessor.
         */
        private static void DreamParticle_UpdatePos(ILContext context, FieldInfo targetField) {
            FieldInfo f_DreamParticle_tempVec2 = typeof(DreamParticle).GetField("tempVec2", BindingFlags.NonPublic | BindingFlags.Static);
            context.Instrs.Clear();

            ILCursor cursor = new ILCursor(context);
            // this.dreamBlock.particles
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, f_CustomDreamParticle_dreamBlock);
            cursor.Emit(OpCodes.Ldfld, f_DreamBlock_particles);
            // [this.idx].Position
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, f_CustomDreamParticle_idx);
            cursor.Emit(OpCodes.Ldelema, DreamParticle.t_DreamParticle);
            cursor.Emit(OpCodes.Ldfld, targetField);
            // -> DreamParticle.tempVec2
            cursor.Emit(OpCodes.Stsfld, f_DreamParticle_tempVec2);
            cursor.Emit(OpCodes.Ret);
        }

        private static void DreamParticle_set_Prop(ILContext context, FieldInfo targetField) {
            context.Instrs.Clear();

            ILCursor cursor = new ILCursor(context);
            // this.dreamBlock.particles[this.idx].{targetField} = value
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, f_CustomDreamParticle_dreamBlock);
            cursor.Emit(OpCodes.Ldfld, f_DreamBlock_particles);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, f_CustomDreamParticle_idx);
            cursor.Emit(OpCodes.Ldelema, DreamParticle.t_DreamParticle);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.Emit(OpCodes.Stfld, targetField);
            // return
            cursor.Emit(OpCodes.Ret);
        }

        private static void DreamParticle_get_Prop(ILContext context, FieldInfo targetField) {
            context.Instrs.Clear();

            ILCursor cursor = new ILCursor(context);
            // return this.dreamBlock.particles[this.idx].{targetField}
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, f_CustomDreamParticle_dreamBlock);
            cursor.Emit(OpCodes.Ldfld, f_DreamBlock_particles);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, f_CustomDreamParticle_idx);
            cursor.Emit(OpCodes.Ldelema, DreamParticle.t_DreamParticle);
            cursor.Emit(OpCodes.Ldfld, targetField);
            cursor.Emit(OpCodes.Ret);
        }

        #endregion
    }
}
