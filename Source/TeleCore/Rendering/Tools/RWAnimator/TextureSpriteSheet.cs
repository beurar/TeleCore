using System.Collections.Generic;
using UnityEngine;

namespace TeleCore
{
    public class TextureSpriteSheet
    {
        private string name;

        private Texture texture;
        private List<SpriteTile> tiles;

        public Texture Texture => texture;
        public List<SpriteTile> Tiles => tiles;

        public TextureSpriteSheet(Texture texture, List<SpriteTile> tiles)
        {
            this.texture = texture;
            this.tiles = tiles;
        }

        public void DrawData(Rect rect)
        {

        }
    }
}
