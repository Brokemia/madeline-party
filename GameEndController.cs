using System;
using System.Collections;
using System.Linq;
using BrokemiaHelper;
using Celeste;
using MadelineParty.Multiplayer;
using Microsoft.Xna.Framework;
using Monocle;

namespace MadelineParty {
    [Tracked(false)]
    public class GameEndController : Entity {
        private Level level;
        public int winnerID;
        private Random textRand = new Random((int)(GameData.Instance.turnOrderSeed / 3) + 70);

        public override void Added(Scene scene) {
            base.Added(scene);
            level = SceneAs<Level>();
            Add(new Coroutine(GameEndRoutine()));
            AddTag(Tags.PauseUpdate);
            AddTag(Tags.FrozenUpdate);
        }

        private string GetWinnerText(int player) {
            // First, set the name to use as a dialog entry
            Dialog.Language.Dialog["MadelineParty_Winner_ID_Name"] = GameData.Instance.GetPlayerName(player);
            return textRand.Choose(Dialog.Clean("MadelineParty_Game_Winner_List").Split(','));
        }

        private IEnumerator GameEndRoutine() {
            if (level.Wipe != null) {
                Action onComplete = level.Wipe.OnComplete;
                level.Wipe.OnComplete = delegate {
                    level.Add(new PersistentMiniTextbox(GetWinnerText(winnerID), FancyText.Anchors.Middle, pauseUpdate: true));
                    onComplete?.Invoke();
                };
            } else {
                level.Add(new PersistentMiniTextbox(GetWinnerText(winnerID), FancyText.Anchors.Middle, pauseUpdate: true));
            }

            yield return 10f;
            level.OnEndOfFrame += delegate {
                Player player = level.Tracker.GetEntity<Player>();
                Leader.StoreStrawberries(player.Leader);
                level.Remove(player);
                level.UnloadLevel();

                level.Session.Level = MadelinePartyModule.START_ROOM;
                level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(level.Bounds.Left, level.Bounds.Top));

                level.LoadLevel(Player.IntroTypes.None);

                Leader.RestoreStrawberries(player.Leader);
            };
        }
    }
}
