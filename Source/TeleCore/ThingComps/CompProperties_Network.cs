using System.Collections.Generic;
using Verse;

namespace TeleCore
{
    /// <summary>
    /// 
    /// </summary>
    public class CompProperties_Network : CompProperties
    {
        public List<NetworkSubPartProperties> networks;
        public string generalIOPattern;

        public CompProperties_Network()
        {
            this.compClass = typeof(Comp_Network);
        }
    }
}
