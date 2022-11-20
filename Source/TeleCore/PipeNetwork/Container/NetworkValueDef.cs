using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeleCore.Static;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class NetworkValueDef : FlowValueDef
    {
        //
        public NetworkDef networkDef;

        //
        public ThingDef specialDroppedContainerDef = null;
        public ThingDef thingDroppedFromContainer;
        public float valueToThingRatio = 1;

        public override void ResolveReferences()
        {
            base.ResolveReferences();
            if (labelShort.NullOrEmpty())
            {
                labelShort = label;
            }

            networkDef.Notify_ResolvedNetworkValueDef(this);
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
