using Celeste;
using Celeste.Mod.Entities;
using MadelineParty.Multiplayer;
using MadelineParty.Multiplayer.General;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;

namespace MadelineParty {
    [CustomEntity("madelineparty/minigameModeScreen")]
    public class MinigameModeScreen : Entity {
        public class ScreenTerminal : Entity {
            private MinigameModeScreen parent;
			private int? lastSelected = null;
			public bool Interacting { get; private set; }

            public ScreenTerminal(MinigameModeScreen parent, Vector2 position) : base(position) {
                this.parent = parent;
                Depth = -8500;
                TalkComponent talk = new TalkComponent(new Rectangle(-24, -8, 48, 8), new Vector2(-0.5f, -20f), Interact);
                talk.PlayerMustBeFacing = false;
                Add(talk);
            }

            private void Interact(Player player) {
                Coroutine routine = new Coroutine(InteractRoutine(player));
                routine.RemoveOnComplete = true;
                Add(routine);
            }

            private IEnumerator InteractRoutine(Player player) {
				Level level = SceneAs<Level>();
				SandwichLava sandwichLava = Scene.Entities.FindFirst<SandwichLava>();
				if (sandwichLava != null) {
					sandwichLava.Waiting = true;
				}
				if (player.Holding != null) {
					player.Drop();
				}
				player.StateMachine.State = 11;
				yield return player.DummyWalkToExact((int)X, walkBackwards: false, 1f, cancelOnFall: true);
				if (Math.Abs(X - player.X) > 4f || player.Dead || !player.OnGround()) {
					if (!player.Dead) {
						player.StateMachine.State = 0;
					}
					yield break;
				}
				Audio.Play("event:/game/general/lookout_use", Position);
                yield return 0.1f;
				parent.menu.Items.ForEach(i => { if (i is TextMenu.Button) i.Selectable = true; });
				parent.menu.Selection = lastSelected ?? parent.menu.FirstPossibleSelection;
				Interacting = true;
			}

			public void EndInteraction() {
				if (Interacting) {
					Scene.OnEndOfFrame += delegate {
						lastSelected = parent.menu.Selection;
						parent.menu.Items.ForEach(i => { if (i is TextMenu.Button) i.Selectable = false; });
						parent.menu.Selection = -1;
						Interacting = false;
						if (Scene.Tracker.GetEntity<Player>() is Player player) {
							player.StateMachine.State = Player.StNormal;
						}
						Input.Dash.ConsumeBuffer();
					};
				}
			}
        }

		private TextMenu menu;

        private int width, height;

		private ScreenTerminal terminal;

        public MinigameModeScreen(EntityData data, Vector2 offset) : base(data.Position * 6) {
			width = data.Width;
			height = data.Height;
            terminal = new(this, data.NodesOffset(offset)[0]);

            AddTag(TagsExt.SubHUD);
        }

        public override void Added(Scene scene) {
            base.Added(scene);
			Level level = SceneAs<Level>();
            Scene.Add(terminal);
			GameData.Instance.minigameStatus.Clear();
			foreach(var kvp in GameData.Instance.minigameWins) {
				GameData.Instance.minigameStatus[kvp.Key] = kvp.Value;
			}
			Scene.Add(new MinigameScoreDisplay(new MinigameFinishTrigger(new(), new())));

			menu = new TextMenuPlus() {
				DoCrop = true,
				Crop = new Rectangle((int)Position.X, (int)Position.Y, width * 6, height * 6)
			};
			menu.Tag = TagsExt.SubHUD;
			menu.AutoScroll = true;
			menu.InnerContent = TextMenu.InnerContentMode.TwoColumn;
            var header = new TextMenuPlus.BetterHeader(Dialog.Clean("MadelineParty_Minigame_List_Title")) {
                Alignment = TextMenuPlus.TextAlignment.Left
            };
            menu.Add(header);

			GameData.Instance.GetAllMinigames(level).ForEach((lvl) => {
				menu.Add(new TextMenu.Button(Dialog.Clean("MadelineParty_Minigame_Name_" + lvl.Name)) { Selectable = false }.Pressed(delegate
				{
					if (terminal.Interacting) {
						MultiplayerSingleton.Instance.Send(new MinigameStart { choice = lvl.Name, gameStart = DateTime.UtcNow.AddSeconds(3).ToFileTimeUtc() });
						GameData.Instance.minigame = lvl.Name;
						GameData.Instance.minigameStatus.Clear();
						level.Remove(level.Entities.FindAll<MinigameDisplay>());
						ModeManager.Instance.AfterMinigameChosen();
					}
				}));
			});
			menu.OnCancel = terminal.EndInteraction;
			menu.Position = new Vector2(menu.Width / 2 + Position.X, Engine.Height / 2f);
			Scene.Add(menu);
		}
    }
}
