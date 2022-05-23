using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TeleCore
{
    public class CompProperties_FX : CompProperties
    {
        //Layers
        public List<FXGraphicData> overlays = new List<FXGraphicData>();
        public IntRange tickOffset = new IntRange(0, 333);
        public bool useParentClass = false;

        public override IEnumerable<string> ConfigErrors(ThingDef def)
        {
            if (def.drawerType == DrawerType.MapMeshOnly && Enumerable.Any(overlays, o => o.mode != FXMode.Static))
                yield return $"{def} has dynamic overlays but is MapMeshOnly";
        }

        public CompProperties_FX()
        {
            this.compClass = typeof(CompFX);
        }
    }
}
