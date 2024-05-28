using Celeste;
using Celeste.Mod.Entities;
using MadelineParty.Entities;
using MadelineParty.Multiplayer;
using MadelineParty.Multiplayer.General;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;

namespace MadelineParty
{
    [CustomEntity("madelineparty/minigameModeScreen")]
    public class MinigameModeScreen : Entity {
        public class ScreenTerminal : Entity {
            private MinigameModeScreen parent;
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
				MultiplayerSingleton.Instance.Send(new MinigameMenu { selection = parent.menu.Selection });
				parent.menu.Focused = true;
				Interacting = true;
			}

			public void EndInteraction() {
				if (Interacting) {
					Scene.OnEndOfFrame += delegate {
						parent.menu.Focused = false;
						Interacting = false;
						if (Scene.Tracker.GetEntity<Player>() is Player player) {
							player.StateMachine.State = Player.StNormal;
						}
						Input.Dash.ConsumeBuffer();
					};
				}
			}
        }

		private TextMenuPlus menu;

        private int width, height;

		private ScreenTerminal terminal;

		private Level level;

        public MinigameModeScreen(EntityData data, Vector2 offset) : base(data.Position * 6) {
			width = data.Width;
			height = data.Height;
            terminal = new(this, data.NodesOffset(offset)[0]);

            AddTag(TagsExt.SubHUD);
        }

        public override void Added(Scene scene) {
            base.Added(scene);
			level = SceneAs<Level>();
            Scene.Add(terminal);
			GameData.Instance.minigameStatus.Clear();
			foreach(var kvp in GameData.Instance.minigameWins) {
				GameData.Instance.minigameStatus[kvp.Key] = kvp.Value;
			}
			Scene.Add(new MinigameScoreDisplay(new MinigameFinishTrigger(new(), new())));

			menu = new TextMenuPlus() {
				DoCrop = true,
				Crop = new Rectangle((int)Position.X, (int)Position.Y, width * 6, height * 6),
				AlwaysHighlight = true
			};
			menu.Tag = TagsExt.SubHUD;
			menu.AutoScroll = true;
			menu.InnerContent = TextMenu.InnerContentMode.TwoColumn;
            var header = new TextMenuPlus.BetterHeader(Dialog.Clean("MadelineParty_Minigame_List_Title")) {
                Alignment = TextMenuPlus.TextAlignment.Left
            };
            menu.Add(header);

			var minigameLevels = GameData.Instance.GetAllMinigames(level, new() {
				PlayerCount = GameData.Instance.playerNumber
			});

			menu.Add(new TextMenu.Button(Dialog.Clean("MadelineParty_Minigame_List_Random")).Pressed(delegate {
				if (terminal.Interacting) {
					SelectLevel(minigameLevels[new Random().Next(minigameLevels.Count)].Name);
				}
			}));

			minigameLevels.ForEach((lvl) => {
				menu.Add(new TextMenu.Button(Dialog.Clean("MadelineParty_Minigame_Name_" + lvl.Name)).Pressed(delegate {
					if (terminal.Interacting) {
						SelectLevel(lvl.Name);
					}
				}));
			});
			menu.OnCancel = terminal.EndInteraction;
			menu.Position = new Vector2(menu.Width / 2 + Position.X, Engine.Height / 2f);
			menu.OnSelectionChanged += (oldSelect, newSelect) => {
				if (terminal.Interacting) {
					MultiplayerSingleton.Instance.Send(new MinigameMenu { selection = newSelect });
                }
			};
			menu.Focused = false;
            Add(new MultiplayerHandlerComponent<MinigameMenu>("minigameModeScreen", HandleMinigameMenu));
            Scene.Add(menu);
		}

		private void SelectLevel(string levelName) {
			MultiplayerSingleton.Instance.Send(new MinigameStart { choice = levelName, gameStart = DateTime.UtcNow.AddSeconds(3).ToFileTimeUtc() });
			GameData.Instance.minigame = levelName;
			ModeManager.Instance.AfterMinigameChosen();
		}

		private void HandleMinigameMenu(MPData data) {
			if (data is not MinigameMenu mm) return;
			if (GameData.Instance.celestenetIDs.Contains(mm.ID) && mm.ID != MultiplayerSingleton.Instance.CurrentPlayerID()) {
				menu.Selection = mm.selection;
			}
        }
	}
}
