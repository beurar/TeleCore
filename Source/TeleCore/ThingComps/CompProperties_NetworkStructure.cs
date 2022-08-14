using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
