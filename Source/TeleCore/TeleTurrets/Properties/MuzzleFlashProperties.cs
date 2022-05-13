using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TeleCore
{
    public class MuzzleFlashProperties
    {
        private Graphic graphicInt;

        public GraphicData flashGraphicData;

        public float scale = 1;

        public float fadeInTime = 0f;
        public float solidTime = 0.25f;
        public float fadeOutTime = 0f;

        public Graphic Graphic => graphicInt ??= flashGraphicData.Graphic;
    }
}
