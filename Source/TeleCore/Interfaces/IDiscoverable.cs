using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TeleCore
{
    public class DiscoveryDef : Def
    {
        //public WikiEntryDef wikiEntry;

        public void Discover()
        {
            StaticData.WorldCompTele().discoveryTable.Discover(this);
        }
    }

    public interface IDiscoverable
    {
        DiscoveryDef DiscoveryDef { get; }
        bool Discovered { get; }

        string DiscoveredLabel { get; }
        string UnknownLabel { get; }
        string DiscoveredDescription { get; }
        string UnknownDescription { get; }
        string DescriptionExtra { get; }
    }
}
