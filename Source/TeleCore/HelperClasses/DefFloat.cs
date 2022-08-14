using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TeleCore
{
    public class DefFloat<T> : DefValueDef<T, float> where T : Def
    {
    }

    public class DefCount<T> : DefValueDef<T, int> where T : Def
    {
    }
}
