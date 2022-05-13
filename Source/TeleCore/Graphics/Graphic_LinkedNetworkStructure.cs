using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class Graphic_LinkedNetworkStructure : Graphic_Linked
    {
        public Graphic_LinkedNetworkStructure() { }

        public Graphic_LinkedNetworkStructure(Graphic subGraphic)
        {
            this.subGraphic = subGraphic;
        }

        public override bool ShouldLinkWith(IntVec3 c, Thing parent)
        {
            return c.InBounds(parent.Map) && parent.Map.TeleCore().NetworkInfo.HasConnectionAtFor(parent, c);
        }

        public override void Print(SectionLayer layer, Thing thing, float extraRotation)
        {
            var comp = thing.TryGetComp<Comp_NetworkStructure>();
            if (comp == null) return;

            foreach (var pos in comp.InnerConnectionCells)
            {
                Printer_Plane.PrintPlane(layer, pos.ToVector3ShiftedWithAltitude(AltitudeLayer.FloorEmplacement), Vector2.one, LinkedDrawMatFrom(thing, pos));
            }
        }
    }
}
