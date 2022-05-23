using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TeleCore
{
    public class CompProperties_NetworkBillsCrafter : CompProperties_NetworkStructure
    {
        [Unsaved] 
        private List<CustomRecipeRatioDef> ratioDefsInt;
        [Unsaved] 
        private List<CustomRecipePresetDef> presetDefsInt;

        public List<string> ratioTags;
        public List<string> presetTags;
        //TODO: use tagged collection generation over direct lists
        //public List<CustomRecipePresetDef> presets;
        //public List<CustomRecipeRatioDef> ratios;

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
