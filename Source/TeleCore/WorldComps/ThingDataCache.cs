using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TeleCore
{
    internal class ThingDataCache : IExposable
    {
        private Dictionary<DiscoveryDef, bool> discoveries = new Dictionary<DiscoveryDef, bool>();
        private Dictionary<ThingDef, bool> discoveredMenuOptions = new Dictionary<ThingDef, bool>();
        private Dictionary<ThingDef, bool> favoritedOptions = new Dictionary<ThingDef, bool>();
        
        public Dictionary<DiscoveryDef, bool> Discoveries => discoveries;
        public Dictionary<ThingDef, bool> DiscoveredMenuOptions => discoveredMenuOptions;
        public Dictionary<ThingDef, bool> FavoritedOptions => favoritedOptions;
        
        public void ExposeData()
        {
            Scribe_Collections.Look(ref discoveries, "discoveredDict");
            Scribe_Collections.Look(ref discoveredMenuOptions, "menuDiscovered");
            Scribe_Collections.Look(ref favoritedOptions, "favoritedOptions");
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

        //
        public void ToggleMenuOptionFavorite(ThingDef def)
        {
            if (favoritedOptions.TryGetValue(def, out var value))
            {
                favoritedOptions[def] = !value;
            }
            else
            {
                favoritedOptions.Add(def, true);
            }
        }
        
        public bool MenuOptionIsFavorited(ThingDef def)
        {
            return favoritedOptions.TryGetValue(def, out bool value) && value;;
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

