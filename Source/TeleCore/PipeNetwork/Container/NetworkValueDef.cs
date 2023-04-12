using System.Collections.Generic;
using TeleCore;
using TeleCore.FlowCore;
using Verse;

namespace TeleCore
{
    public class NetworkValueDef : FlowValueDef
    {
        //
        public ThingDef specialDroppedContainerDef = null;
        public ThingDef thingDroppedFromContainer;
        public float valueToThingRatio = 1;
        
        public NetworkDef NetworkDef => (NetworkDef)collectionDef;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var configError in base.ConfigErrors())
            {
                yield return configError;
            }
        }
    }
}
