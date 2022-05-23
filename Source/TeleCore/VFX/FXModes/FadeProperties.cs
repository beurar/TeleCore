using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TeleCore
{
    public class FadeProperties
    {
        public int opacityDuration = 60;
        public int initialOpacityOffset;
        public FloatRange opacityRange = FloatRange.One;
    }
}
