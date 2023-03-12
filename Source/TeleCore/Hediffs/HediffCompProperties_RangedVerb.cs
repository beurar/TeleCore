using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TeleCore
{
    public class HediffCompProperties_RangedVerb : HediffCompProperties
    {
        public HediffCompProperties_RangedVerb()
        {
            compClass = typeof(HediffComp_RangedVerb);
        }

        public IEnumerable<VerbProperties> VerbsBase => verbs.Select(v => v as VerbProperties);

        public List<VerbProperties_Extended> verbs;
    }
}
