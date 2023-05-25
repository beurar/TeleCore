using System.Collections.Generic;
using System.Text.RegularExpressions;
using TeleCore.Network.IO;
using Verse;

namespace TeleCore
{
    /// <summary>
    /// 
    /// </summary>
    public class CompProperties_Network : CompProperties
    {
        [Unsaved] 
        private NetworkCellIOSimple _simpleIO;
        
        //
        public List<NetworkSubPartProperties> networks;
        public string generalIOPattern;

        public NetworkCellIOSimple SimpleIO => _simpleIO;
        
        public CompProperties_Network()
        {
            this.compClass = typeof(Comp_Network);
        }

        public override void PostLoadSpecial(ThingDef parent)
        {
            base.PostLoadSpecial(parent);

            foreach (var network in networks)
            {
                network.PostLoadSpecial(parent);
            }
            
            //
            if (generalIOPattern == null) return;
            
            _simpleIO = new NetworkCellIOSimple(generalIOPattern, parent);
            
            var newString = generalIOPattern.Replace("|", "");
            MatchCollection matches = Regex.Matches(newString, NetworkCellIO.regexPattern);
            if (matches.Count != parent.size.Area)
            {
                TLog.Error($"Network IO pattern does not match the size of the thing '{parent}'.");
            }
        }
    }
}
