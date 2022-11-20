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

        public void Print(SectionLayer layer, Thing thing, float extraRotation, NetworkSubPart forPart)
        {
            foreach (var pos in forPart.CellIO.InnerConnnectionCells)
            {
                Printer_Plane.PrintPlane(layer, pos.ToVector3ShiftedWithAltitude(AltitudeLayer.FloorEmplacement), Vector2.one, LinkedDrawMatFrom(thing, pos));
            }
        }

        public override void Print(SectionLayer layer, Thing thing, float extraRotation)
        {
            var comp = thing.TryGetComp<Comp_NetworkStructure>();
            if (comp == null) return;

            foreach (var subPart in comp.NetworkParts)
            {
                foreach (var pos in subPart.CellIO.InnerConnnectionCells)
                {
                    Printer_Plane.PrintPlane(layer, pos.ToVector3ShiftedWithAltitude(AltitudeLayer.FloorEmplacement), Vector2.one, LinkedDrawMatFrom(thing, pos));
                }
            }
        }
    }
}
