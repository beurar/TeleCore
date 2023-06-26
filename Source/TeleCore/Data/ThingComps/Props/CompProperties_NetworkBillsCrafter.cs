using System.Collections.Generic;
using TeleCore.Network.Data;
using Verse;

namespace TeleCore
{
    public class CompProperties_NetworkBillsCrafter : CompProperties_Network
    {
        [Unsaved] 
        private List<CustomRecipeRatioDef> ratioDefsInt;
        [Unsaved] 
        private List<CustomRecipePresetDef> presetDefsInt;

        //Tags are used to get lists of associated defs
        public List<string> ratioTags;
        public List<string> presetTags;

        public List<CustomRecipeRatioDef> UsedRatioDefs
        {
            get
            {
                if (ratioDefsInt == null)
                {
                    ratioDefsInt = new List<CustomRecipeRatioDef>();
                    foreach (var ratioTag in ratioTags)
                    {
                        var list = CustomNetworkRecipeReferences.TryGetRatiosOfTag(ratioTag);
                        if(list == null) continue;
                        ratioDefsInt.AddRange(list);
                    }
                }
                return ratioDefsInt;
            }
        }

        public List<CustomRecipePresetDef> UsedPresetDefs 
        {
            get
            {
                if (presetDefsInt == null)
                {
                    presetDefsInt = new List<CustomRecipePresetDef>();
                    foreach (var ratioTag in presetTags)
                    {
                        var list = CustomNetworkRecipeReferences.TryGetPresetsOfTag(ratioTag);
                        if (list == null) continue;
                        presetDefsInt.AddRange(list);
                    }
                }
                return presetDefsInt;
            }
        }

        public CompProperties_NetworkBillsCrafter()
        {
            this.compClass = typeof(Comp_NetworkBillsCrafter);
        }
    }
}
