using Microsoft.Xna.Framework.Graphics.PackedVector;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste;
using Microsoft.Xna.Framework;

namespace MadelineParty.SubHud {
    public class SubHudImage(MTexture texture) : Image(texture) {
        public override void Render() {
            var actualPos = Position;
            // Only x5 to counter the Entity.Position added by RenderPosition
            Position += (Entity.Position * 5 - SceneAs<Level>().Camera.Position * 6);
            base.Render();
            Position = actualPos;
        }
    }
}
