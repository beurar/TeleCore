using System.Collections.Generic;
using Verse;

namespace TeleCore
{
    public class FXDefExtension : DefModExtension
    {
        public bool rotateDrawSize = true;
        public bool alignToBottom = false;
        public bool? drawRotatedOverride = null;

        //public List<string> linkStrings;
        public List<DynamicTextureParameter> textureParams;
    }
}
