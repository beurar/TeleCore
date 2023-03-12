using System.Collections.Generic;
using Verse;

namespace TeleCore
{
    /// <summary>
    /// 
    /// </summary>
    public class CompProperties_NetworkStructure : CompProperties
    {
        public List<NetworkSubPartProperties> networks;
        public string generalIOPattern;

        public CompProperties_NetworkStructure()
        {
            this.compClass = typeof(Comp_NetworkStructure);
        }
    }
}
