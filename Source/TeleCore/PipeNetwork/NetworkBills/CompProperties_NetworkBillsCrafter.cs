using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleCore
{
    public class CompProperties_NetworkBillsCrafter : CompProperties_NetworkStructure
    {
        public List<CustomRecipePresetDef> presets;
        public List<CustomRecipeRatioDef> ratios;

        public CompProperties_NetworkBillsCrafter()
        {
            this.compClass = typeof(Comp_NetworkBillsCrafter);
        }
    }
}
