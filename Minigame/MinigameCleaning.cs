using Celeste;
using MadelineParty.Minigame.Misc;
using MadelineParty.Multiplayer.General;
using MadelineParty.Multiplayer;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using Celeste.Mod;
using System.Collections;
using System.Collections.Generic;
using Celeste.Mod.Entities;
using BrokemiaHelper;
using MadelineParty.Board;

namespace MadelineParty.Minigame
{
    [CustomEntity("madelineparty/minigameCleaning")]
    public class MinigameCleaning : MinigameEntity {
        private Coroutine endCoroutine;
        private Atlas atlas;
        private string[] possibleImages;
        private readonly Dictionary<int, DirtyImage> images = new();
        private DirtyImage myImage;
        private int everyThirdFrame;
        
        public MinigameCleaning(EntityData data, Vector2 offset) : base(data, offset) {
            atlas = data.Attr("atlas") switch {
                "Gui" => GFX.Gui,
                "Portraits" => GFX.Portraits,
                "Checkpoints" => MTN.Checkpoints,
                _ => GFX.Game
            };
            possibleImages = data.Attr("possibleImages").Split(',');
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            var image = possibleImages[GameData.Instance.Random.Next(possibleImages.Length)];
            foreach (DirtyImage img in scene.Tracker.GetEntities<DirtyImage>()) {
                img.SetImage(atlas[image]);
                images[img.PlayerID] = img;
                img.CanClean = false;
            }
            myImage = images[GameData.Instance.realPlayerID];
            myImage.CanClean = true;
            myImage.OnAddPoint += v => MultiplayerSingleton.Instance.Send(new MinigameVector2 { vec = v, extra = GameData.Instance.realPlayerID });
        }
        
        public override void MultiplayerReceiveVector2(Vector2 vec, int extra) {
            images[extra].AddPoint(vec);
        }

        protected override void AfterStart() {
            base.AfterStart();
            var player = level.Tracker.GetEntity<Player>();
            player.JustRespawned = false;

            var state = player.Get<TronState>();
            // TODO fix this when I refactor characters
            state.HairColor = PlayerToken.colors[PlayerToken.GetFullPath(BoardController.TokenPaths[GameData.Instance.realPlayerID])];
            //state.TargetSpeed = 110;
            //state.MaxSpeed = 140;
            state.LeaveTrail = false;
            state.StartTron();

            level.Add(new MinigameTimeDisplay(this, true) { CountdownTime = 15 });
            level.Add(new MinigameScoreDisplay(this, "{0:0}%", s => (s / (float)myImage.TotalPixels) * 100));
        }

        public override void Update() {
            base.Update();
            if (Data.Started && level.RawTimeActive - Data.StartTime >= 15 && endCoroutine == null) {
                Add(endCoroutine = new(FinishMinigame()));
            }

            everyThirdFrame++;
            if(Data.Started && everyThirdFrame >= 2) {
                everyThirdFrame = 0;
                var erased = myImage.GetErasedCount();
                GameData.Instance.minigameStatus[GameData.Instance.realPlayerID] = (uint) erased;
                MultiplayerSingleton.Instance.Send(new MinigameStatus { results = (uint) erased });
            }
        }

        protected IEnumerator FinishMinigame() {
            completed = true;
            level.CanRetry = false;
            while (myImage.IsContentLost) {
                yield return null;
            }
            var erased = myImage.GetErasedCount();
            GameData.Instance.minigameResults.Add(new Tuple<int, uint>(GameData.Instance.realPlayerID, (uint) erased));
            MultiplayerSingleton.Instance.Send(new MinigameEnd { results = (uint) erased });

            yield return new SwapImmediately(EndMinigame(HIGHEST_WINS, null));
        }
    }
}
