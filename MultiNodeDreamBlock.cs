using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace MadelineParty {
	[TrackedAs(typeof(DreamBlock))]
	[CustomEntity("madelineparty/multiNodeDreamBlock")]
    public class MultiNodeDreamBlock : DreamBlock {
		private int targetIdx = 0;
		private Vector2 from, to;

        private Vector2[] nodes;
		private DynamicData selfData;

        public MultiNodeDreamBlock(EntityData data, Vector2 offset) : base(data, offset) {
            nodes = data.NodesOffset(offset);
			selfData = DynamicData.For(this);
		}

        public override void Added(Scene scene) {
            base.Added(scene);
			bool playerHasDreamDash = SceneAs<Level>().Session.Inventory.DreamDash;
			if (playerHasDreamDash && nodes.Length > 0) {
				Remove(Components.Get<Tween>());
				StartTween();
			}
		}

		private void StartTween() {
			
			from = Position;
			to = nodes[targetIdx];
			float duration = Vector2.Distance(from, to) / 12f;
			if (selfData.Get<bool>("fastMoving")) {
				duration /= 3f;
			}
			Tween tween = Tween.Create(Tween.TweenMode.Looping, Ease.SineInOut, duration, start: true);
			tween.OnUpdate = delegate (Tween t) {
				if (Collidable) {
					MoveTo(Vector2.Lerp(from, to, t.Eased));
				} else {
					MoveToNaive(Vector2.Lerp(from, to, t.Eased));
				}
			};
			tween.OnComplete = delegate (Tween t) {
				targetIdx++;
				if (targetIdx >= nodes.Length) {
					targetIdx = 0;
				}
				from = Position;
				to = nodes[targetIdx];
				selfData.Invoke("set_Duration", Vector2.Distance(from, to) / (selfData.Get<bool>("fastMoving") ?  36f : 12f));
			};
			Add(tween);
		}
    }
}
