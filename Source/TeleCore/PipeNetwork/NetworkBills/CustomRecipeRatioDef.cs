using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TeleCore
{
    public class CustomRecipeRatioDef : Def
    {
        public bool hidden = false;
        public List<DefFloat<NetworkValueDef>> inputRatio;
        public ThingDef result;
    }
}
