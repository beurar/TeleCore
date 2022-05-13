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
        public int sizeDuration = 60;
        public FloatRange opacityRange = FloatRange.Zero;
        public FloatRange sizeRange = FloatRange.Zero;
        public int opacityOffset;
        public int sizeOffset;
    }
}
