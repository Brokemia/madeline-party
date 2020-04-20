using System;
using System.Reflection;
using Celeste;
using Celeste.Mod;
using Celeste.Mod.Ghost.Net;
using Monocle;

namespace MadelineParty
{
    [Obsolete("Replaces by making custom entities update while paused", true)]
    public class PlayWhilePaused
    {
        private int stateBeforePausing = -1;
        private Level level;

        public void DoThings()
        {
            Everest.Events.Level.OnLoadLevel += (level, playerIntro, isFromLoader) =>
            {
                this.level = level;
                if (MadelinePartyModule.IsSIDMadelineParty(level.Session.Area.GetSID()) && MadelinePartyModule.ghostnetConnected)
                {
                    foreach (Entity e in level.Entities)
                    {
                        e.AddTag(Tags.PauseUpdate);
                    }
                }
            };
            On.Monocle.Entity.Added += (orig, self, scene) =>
            {
                if (MadelinePartyModule.IsSIDMadelineParty(self.SceneAs<Level>()?.Session.Area.GetSID() ?? "") && MadelinePartyModule.ghostnetConnected)
                {
                    self.AddTag(Tags.PauseUpdate);
                }
                orig(self, scene);
            };
            On.Monocle.Scene.Add_Entity += (orig, self, entity) => {
                if (MadelinePartyModule.IsSIDMadelineParty(level?.Session.Area.GetSID() ?? "") && MadelinePartyModule.ghostnetConnected)
                {
                    entity.AddTag(Tags.PauseUpdate);
                }
                orig(self, entity);
            };
            On.Celeste.Level.UnloadLevel += (orig, self) =>
            {
                if (MadelinePartyModule.IsSIDMadelineParty(self.Session.Area.GetSID()))
                {
                    TextMenu menu = self.Entities.FindFirst<TextMenu>();
                    if (menu != null)
                    {
                        self.PauseMainMenuOpen = false;
                        menu.RemoveSelf();
                        self.Paused = false;
                    }
                }
                orig(self);
            };
            On.Celeste.TextMenu.Close += (orig, self) =>
            {
                orig(self);
                if (!MadelinePartyModule.IsSIDMadelineParty(self.SceneAs<Level>().Session.Area.GetSID()) || !MadelinePartyModule.ghostnetConnected)
                {
                    return;
                }
                Player p = self.SceneAs<Level>()?.Entities.FindFirst<Player>();
                if (p != null)
                {
                    //p.StateMachine.Locked = false;
                    //p.StateMachine.State = 0;
                    stateBeforePausing = -1;
                    Input.Initialize();
                }
            };
            On.Celeste.Player.Update += Player_Update;

            // From Ghostnet connection setup
            Everest.Events.Level.OnPause += (level, startIndex, minimal, quickReset) => {
                if (!MadelinePartyModule.IsSIDMadelineParty(level.Session.Area.GetSID()) || !MadelinePartyModule.ghostnetConnected) return;
                Player p = level.Entities.FindFirst<Player>();
                if (p != null)
                {
                    stateBeforePausing = p.StateMachine.State;
                    //int state = p.StateMachine.State;
                    //if (state != 4)
                    //{
                    //    // Dummy state
                    //    p.StateMachine.State = 11;
                    //    p.StateMachine.Locked = true;
                    //}
                    DeregisterPlayerRelatedInputs();
                }
            };
            On.Monocle.Scene.BeforeUpdate += (orig, self) =>
            {
                if (!MadelinePartyModule.IsSIDMadelineParty(level.Session.Area.GetSID()) || !MadelinePartyModule.ghostnetConnected)
                {
                    orig(self);
                    return;
                }
                if (self.Paused)
                {
                    self.TimeActive += Engine.DeltaTime;
                }
                orig(self);
            };
            On.Celeste.Level.Update += (orig, self) =>
            {
                if (!MadelinePartyModule.IsSIDMadelineParty(level.Session.Area.GetSID()) || !MadelinePartyModule.ghostnetConnected)
                {
                    orig(self);
                    return;
                }
                orig(self);
                if (self.Paused)
                    self.RendererList.GetType().GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod).Invoke(self.RendererList, null);
                FieldInfo updateHair = self.GetType().GetField("updateHair", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField);
                updateHair.SetValue(self, true);
                if (self.FrozenOrPaused)
                {
                    self.Particles.Update();
                    self.ParticlesFG.Update();
                    self.ParticlesBG.Update();
                    TrailManager trailManager = Engine.Scene.Tracker.GetEntity<TrailManager>();
                    if (trailManager != null)
                    {
                        TrailManager.Snapshot[] snapshots = trailManager.GetSnapshots();
                        for (int i = 0; i < snapshots.Length; i++)
                        {
                            TrailManager.Snapshot snapshot = snapshots[i];
                            if (snapshot == null)
                                continue;
                            snapshot.Update();
                        }
                    }
                }
            };
        }

        void Player_Update(On.Celeste.Player.orig_Update orig, Player self)
        {
            Level level = self.SceneAs<Level>();
            if (level != null && MadelinePartyModule.IsSIDMadelineParty(level.Session.Area.GetSID()) && MadelinePartyModule.ghostnetConnected && level.Paused)
            {
                ConsumePlayerRelatedInputs();
            }
            orig(self);
            if (!self.Scene.Paused && stateBeforePausing >= 0)
            {
                //self.StateMachine.Locked = false;
                //self.StateMachine.State = 0;
                stateBeforePausing = -1;
                Input.Initialize();
            }
            if (level != null && MadelinePartyModule.IsSIDMadelineParty(level.Session.Area.GetSID()) && MadelinePartyModule.ghostnetInstalled)
            {
                if (self.Scene.Paused)
                {
                    if (self.Top > (float)level.Bounds.Bottom)
                    {
                        level.Paused = false;
                        level.PauseMainMenuOpen = false;
                        level.Entities.FindFirst<TextMenu>()?.RemoveSelf();
                    }
                }
                if (MadelinePartyModule.ghostnetConnected)
                {
                    // As more entities are found that mess with tags, this will need to be updated
                    foreach (Booster b in level.Entities.FindAll<Booster>())
                    {
                        if (!b.TagCheck(Tags.PauseUpdate))
                            b.AddTag(Tags.PauseUpdate);
                    }
                    //if (self.Scene.Paused && (self.StateMachine.State ==))
                    //{

                    //}
                }
            }

        }

        private void ConsumePlayerRelatedInputs()
        {
            if (Input.QuickRestart != null)
            {
                Input.QuickRestart.ConsumePress();
                Input.QuickRestart.ConsumeBuffer();
            }
            if (Input.Jump != null)
            {
                Input.Jump.ConsumePress();
                Input.Jump.ConsumeBuffer();
            }
            if (Input.Dash != null)
            {
                Input.Dash.ConsumePress();
                Input.Dash.ConsumeBuffer();
            }
            if (Input.Grab != null)
            {
                Input.Grab.ConsumePress();
                Input.Grab.ConsumeBuffer();
            }
            if (Input.Talk != null)
            {
                Input.Talk.ConsumePress();
                Input.Talk.ConsumeBuffer();
            }
        }

        private void DeregisterPlayerRelatedInputs()
        {
            if (Input.QuickRestart != null)
            {
                Input.QuickRestart.Deregister();
            }
            if (Input.MoveX != null)
            {
                Input.MoveX.Deregister();
            }
            if (Input.MoveY != null)
            {
                Input.MoveY.Deregister();
            }
            if (Input.GliderMoveY != null)
            {
                Input.GliderMoveY.Deregister();
            }
            if (Input.Aim != null)
            {
                Input.Aim.Deregister();
            }
            if (Input.MountainAim != null)
            {
                Input.MountainAim.Deregister();
            }
            if (Input.Jump != null)
            {
                Input.Jump.Deregister();
            }
            if (Input.Dash != null)
            {
                Input.Dash.Deregister();
            }
            if (Input.Grab != null)
            {
                Input.Grab.Deregister();
            }
            if (Input.Talk != null)
            {
                Input.Talk.Deregister();
            }
        }
    }
}
