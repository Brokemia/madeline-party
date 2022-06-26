using Celeste;
using Celeste.Mod.Entities;
using MadelineParty.Multiplayer;
using MadelineParty.Multiplayer.General;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MadelineParty {
    [CustomEntity("madelineparty/tieBreakerController")]
    class TiebreakerController : Entity {
        private Level level;
        private List<DieNumber> numbers = new List<DieNumber>();
        private int tiedCount;
        private List<PlayerData> playersSorted;
        private int[] rolls;

        public override void Added(Scene scene) {
            base.Added(scene);
            level = SceneAs<Level>();
            rolls = new int[4];
            List<int> list = new List<int>(BoardController.oneToTen);
            Random rand = new Random((int)GameData.tieBreakerSeed);
            for (int i = 0; i < 4; i++) {
                rolls[i] = list[rand.Next(list.Count)];
                list.Remove(rolls[i]);
            }
        }

        public void RollDice(Vector2 buttonPos, int playerID) {
            if (playerID == GameData.realPlayerID) {
                MultiplayerSingleton.Instance.Send(new TiebreakerRolled { ButtonPosition = buttonPos });
            }

            Add(new Coroutine(DieRollAnimation(buttonPos, playersSorted.FindIndex(pd => pd.TokenSelected == playerID), rolls[playerID])));
        }

        private IEnumerator DieRollAnimation(Vector2 buttonPos, int index, int roll) {
            DieNumber number = new DieNumber(null, roll - 1, index);
            level.Add(number);
            numbers.Add(number);
            // new Vector2(8, 4) for text instead of graphics
            number.MoveNumber(buttonPos + new Vector2(0, 12) + new Vector2(8, 4), level.LevelOffset + new Vector2(level.Bounds.Width / 2 - 8, 0) + new Vector2(40 * number.posIndex - 20 * (tiedCount - 1), 4) + new Vector2(8, 4));
            if(numbers.Count == tiedCount) {
                yield return 3.5f;
                yield return RemoveDieRollsAnimation();
            }
        }

        // Remove all but the winner one by one
        private IEnumerator RemoveDieRollsAnimation() {
            List<Tuple<int, int>> usedRolls = new List<Tuple<int, int>>();
            for(int i = 0; i < tiedCount; i++) {
                usedRolls.Add(new Tuple<int, int>(playersSorted[i].TokenSelected, rolls[playersSorted[i].TokenSelected]));
            }
            // Put lowest rolls first
            usedRolls.Sort((x, y) => x.Item2.CompareTo(y.Item2));
            for(int i = 0; i < usedRolls.Count - 1; i++) {
                var n = numbers.Find(n => n.number + 1 == usedRolls[i].Item2);
                level.ParticlesFG.Emit(Refill.P_Shatter, 5, n.Position + new Vector2(4), Vector2.One * 2f, (float)Math.PI);
                level.ParticlesFG.Emit(Refill.P_Shatter, 5, n.Position + new Vector2(4), Vector2.One * 2f, 0);
                n.RemoveSelf();
                yield return 1f;
            }
            yield return 4f;
            TeleportToVictory(usedRolls[usedRolls.Count - 1].Item1);
        }

        private void TeleportToVictory(int winnerID) {
            PlayerData winner = playersSorted.Find(p => p.TokenSelected == winnerID);
            playersSorted.Remove(winner);
            playersSorted.Insert(0, winner);
            int realPlayerPlace = playersSorted.FindIndex((obj) => obj.TokenSelected == GameData.realPlayerID);
            level.OnEndOfFrame += delegate {
                Player player = level.Tracker.GetEntity<Player>();
                Leader.StoreStrawberries(player.Leader);
                level.Remove(player);
                level.UnloadLevel();

                level.Session.Level = "Game_VictoryRoyale";

                List<Vector2> spawns = new List<Vector2>(level.Session.LevelData.Spawns.ToArray());
                // Sort the spawns so the highest one is first
                spawns.Sort((x, y) => { return x.Y.CompareTo(y.Y); });
                level.Session.RespawnPoint = level.GetSpawnPoint(new Vector2(spawns[realPlayerPlace].X, spawns[realPlayerPlace].Y));

                level.LoadLevel(Player.IntroTypes.Jump);

                level.Tracker.GetEntity<GameEndController>().winnerID = winnerID;

                Leader.RestoreStrawberries(player.Leader);
            };
        }

        public void Initialize(int tieCount, List<PlayerData> playerList) {
            tiedCount = tieCount;
            playersSorted = playerList;
            List<LeftButton> buttons = level.Entities.FindAll<LeftButton>().OrderBy(a => a.X).ToList();
            for(int i = 0; i < tieCount; i++) {
                buttons[i].SetCurrentMode(LeftButton.Modes.Dice);
            }
        }

        public static void Load() {
            MultiplayerSingleton.Instance.RegisterHandler<TiebreakerRolled>(HandleTiebreakerRolled);
        }

        private static void HandleTiebreakerRolled(MPData data) {
            if (data is not TiebreakerRolled rolled) return;
            // If another player in our party has rolled a tiebreaker die
            if (GameData.celestenetIDs.Contains(rolled.ID) && rolled.ID != MultiplayerSingleton.Instance.GetPlayerID()) {
                if (Engine.Scene is not Level level) {
                    return;
                }
                foreach (LeftButton button in level.Entities.FindAll<LeftButton>()) {
                    // Find a close button
                    if ((button.Position - rolled.ButtonPosition).LengthSquared() < 1) {
                        button.SetCurrentMode(LeftButton.Modes.Inactive);
                        level.Entities.FindFirst<TiebreakerController>()?.RollDice(rolled.ButtonPosition, GameData.playerSelectTriggers[rolled.ID]);
                        return;
                    }
                }
            }
        }

    }
}
