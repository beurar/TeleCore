using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class TDrawing
    {
        public static void Draw(Graphic graphic, Vector3 drawPos, Rot4 rot, float? rotation, ThingDef def, FXDefExtension extension = null)
        {
            FXGraphic.GetDrawInfo(graphic, ref drawPos, rot, extension, def, out _, out var drawMat, out var drawMesh, out var exactRotation, out _);
            Graphics.DrawMesh(drawMesh, drawPos, rotation?.ToQuat() ?? exactRotation.ToQuat(), drawMat, 0);
        }

        public static void Print(SectionLayer layer, Graphic graphic, ThingWithComps thing, ThingDef def, FXDefExtension extension = null)
        {
            if (graphic is Graphic_Linked || graphic is Graphic_Appearances)
            {
                graphic.Print(layer, thing, 0);
                return;
            }
            if (graphic is Graphic_Random rand)
                graphic = rand.SubGraphicFor(thing);
            var drawPos = thing.DrawPos;
            FXGraphic.GetDrawInfo(graphic, ref drawPos, thing.Rotation, extension, def, out var drawSize, out var drawMat, out _, out var exactRotation, out var flipUV);
            Printer_Plane.PrintPlane(layer, drawPos, drawSize, drawMat, exactRotation, flipUV, null, null, 0.01f, 0f);
            if (graphic.ShadowGraphic != null && thing != null)
            {
                graphic.ShadowGraphic.Print(layer, thing, exactRotation);
            }
            thing.AllComps.ForEach(c => c.PostPrintOnto(layer));
        }
    }
}
