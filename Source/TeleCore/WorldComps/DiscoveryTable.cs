using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TeleCore
{
    internal class DiscoveryTable : IExposable
    {
        public Dictionary<DiscoveryDef, bool> Discoveries = new Dictionary<DiscoveryDef, bool>();
        public Dictionary<ThingDef, bool> DiscoveredMenuOptions = new Dictionary<ThingDef, bool>();

        public void ExposeData()
        {
            Scribe_Collections.Look(ref Discoveries, "discoveredDict");
            Scribe_Collections.Look(ref DiscoveredMenuOptions, "menuDiscovered");
        }


        //Build Menu
        public bool MenuOptionHasBeenSeen(ThingDef def)
        {
            return DiscoveredMenuOptions.TryGetValue(def, out bool value) && value;
        }

        public void DiscoverInMenu(ThingDef def)
        {
            if (MenuOptionHasBeenSeen(def)) return;
            DiscoveredMenuOptions.Add(def, true);
        }

        //Parent Discovery
        public bool IsDiscovered(DiscoveryDef discovery)
        {
            return Discoveries.TryGetValue(discovery, out bool value) && value;
        }

        public bool IsDiscovered(IDiscoverable discoverable)
        {
            return IsDiscovered(discoverable.DiscoveryDef);
        }

        public void Discover(DiscoveryDef discovery)
        {
            if (IsDiscovered(discovery)) return;
            Discoveries.Add(discovery, true);
            //TODO: Letter
            //Find.LetterStack.ReceiveLetter("TR_NewDiscovery".Translate(), "TR_NewDiscoveryDesc".Translate(discovery.description), TiberiumDefOf.DiscoveryLetter);
        }
    }
}

