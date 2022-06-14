using Celeste;
using Celeste.Mod;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MadelineParty {
    public class PersistentMiniTextbox : MiniTextbox {
        public PersistentMiniTextbox(string dialogId) : base(dialogId) {

        }

        public static void Load() {
            On.Celeste.MiniTextbox.Routine += MiniTextbox_Routine;
        }

        public static void Unload() {
            On.Celeste.MiniTextbox.Routine -= MiniTextbox_Routine;
        }

        private static IEnumerator MiniTextbox_Routine(On.Celeste.MiniTextbox.orig_Routine orig, MiniTextbox self) {
            if (self is PersistentMiniTextbox) {
                IEnumerator res = orig(self);
                while (res.MoveNext()) {
                    if (res.Current is float f && f == 3f) {
                        yield break;
                    }
                    yield return res.Current;
                }
            } else {
                yield return new SwapImmediately(orig(self));
            }
        }
    }
}
