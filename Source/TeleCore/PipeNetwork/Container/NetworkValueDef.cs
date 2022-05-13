using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class NetworkValueDef : Def
    {
        public string labelShort;
        public Color valueColor;
        public NetworkDef networkDef;
        public ThingDef dropThing;
        public float ratioForThing = 1;

        public List<ThingDefCountClass> valueThings;

        public override void ResolveReferences()
        {
            base.ResolveReferences();
            if (labelShort.NullOrEmpty())
            {
                labelShort = label;
            }

            networkDef.ResolvedValueDef(this);
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var configError in base.ConfigErrors())
            {
                yield return configError;
            }
        }
    }
}
