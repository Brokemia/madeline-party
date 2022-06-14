using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace MadelineParty {
	[CustomEntity("madelineparty/persistentMiniTextboxTrigger")]
    public class PersistentMiniTextboxTrigger : Trigger {
		private enum Modes {
			OnPlayerEnter,
			OnLevelStart,
			OnTheoEnter
		}

		private EntityID id;

		private string[] dialogOptions;

		private Modes mode;

		private bool triggered;

		private bool onlyOnce;

		private int deathCount;

		public PersistentMiniTextboxTrigger(EntityData data, Vector2 offset, EntityID id) : base(data, offset) {
			this.id = id;
			mode = data.Enum("mode", Modes.OnPlayerEnter);
			dialogOptions = data.Attr("dialog_id").Split(',');
			onlyOnce = data.Bool("only_once");
			deathCount = data.Int("death_count", -1);
			if (mode == Modes.OnTheoEnter) {
				Add(new HoldableCollider((Action<Holdable>)delegate
				{
					Trigger();
				}, null));
			}
		}

		public override void Awake(Scene scene) {
			base.Awake(scene);
			if (mode == Modes.OnLevelStart) {
				Trigger();
			}
		}

		public override void OnEnter(Player player) {
			if (mode == Modes.OnPlayerEnter) {
				Trigger();
			}
		}

		private void Trigger() {
			if (!triggered && (deathCount < 0 || SceneAs<Level>().Session.DeathsInCurrentLevel == deathCount)) {
				triggered = true;
				Scene.Add(new PersistentMiniTextbox(Calc.Random.Choose(dialogOptions)));
				if (onlyOnce) {
					SceneAs<Level>().Session.DoNotLoad.Add(id);
				}
			}
		}
	}
}
