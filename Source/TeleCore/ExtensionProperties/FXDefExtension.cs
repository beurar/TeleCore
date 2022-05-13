using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class FXDefExtension
    {
        public bool rotateDrawSize = true;
        public bool alignToBottom = false;
        public bool? drawRotatedOverride = null;

        //public List<string> linkStrings;
        public List<DynamicTextureParameter> textureParams;
    }
}
