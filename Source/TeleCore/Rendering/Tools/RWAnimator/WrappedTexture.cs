using UnityEngine;

namespace TeleCore
{
    public struct WrappedTexture
    {
        private string path;
        private Texture texture;

        public string Path => path;
        public Texture Texture => texture;

        //
        public bool IsValid => path != null && texture != null;

        public WrappedTexture(string path, Texture texture)
        {
            this.path = path;
            this.texture = texture;
        }

        public void Clear()
        {
            path = null;
            texture = null;
        }
    }
}
