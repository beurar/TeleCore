using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using RimWorld.BaseGen;
using UnityEngine;
using Verse;

namespace TeleCore
{
    internal static class RenderPatches
    {
        //Equipment Size Fix
        //Draw Equipment Size Fix
        [HarmonyPatch(typeof(PawnRenderer)), HarmonyPatch("DrawEquipmentAiming")]
        public static class DrawEquipmentAimingPatch
        {
            static readonly MethodInfo injection = AccessTools.Method(typeof(DrawEquipmentAimingPatch), nameof(RenderInjection));

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var instructionList = instructions.ToList();
                for (var i = 0; i < instructionList.Count; i++)
                {
                    var code = instructionList[i];
                    var nextCode = i + 1 < instructionList.Count ? instructionList[i + 1] : null;
                    if (nextCode != null && nextCode.opcode == OpCodes.Ret)
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_1);
                        yield return new CodeInstruction(OpCodes.Call, injection);
                        continue;
                    }
                    yield return code;
                }
            }

            public static void RenderInjection(Mesh mesh, Vector3 drawLoc, Quaternion quat, Material mat, int layer, Thing thing)
            {
                var size = thing.Graphic.drawSize;
                Graphics.DrawMesh(mesh, Matrix4x4.TRS(drawLoc, quat, new Vector3(size.x, 1, size.y)), mat, layer);
            }
        }

        /*
        //Fix pipe designator drawghost
        [HarmonyPatch(typeof(Designator_Place))]
        [HarmonyPatch(nameof(Designator_Place.DrawGhost))]
        internal static class Designator_Place_DrawGhost
        {
            public static bool Prefix(Designator_Place __instance, Color ghostCol)
            {
                if (__instance.PlacingDef is TRThingDef trDef)
                {
                    if (trDef.building.blueprintGraphicData is { } data)
                    {
                        IntVec3 center = UI.MouseCell();
                        Rot4 rot = __instance.placingRot;

                        Graphic graphic = GhostUtility.GhostGraphicFor(data.Graphic, trDef, ghostCol, __instance.StuffDef);
                        Vector3 loc = GenThing.TrueCenter(center, rot, trDef.Size, AltitudeLayer.Blueprint.AltitudeFor());
                        graphic.DrawFromDef(loc, rot, trDef, 0f);
                        return false;
                    }
                }
                return true;
            }
        }
        */

        //
        //Fix projectile random graphics
        [HarmonyPatch(typeof(Thing), "Graphic", MethodType.Getter)]
        public static class ThingGraphicPatch
        {
            public static bool Prefix(Thing __instance, ref Graphic __result)
            {
                if (__instance is Projectile && TeleCoreMod.Settings.ProjectileGraphicRandomFix)
                {
                    if (__instance.DefaultGraphic is Graphic_Random Random)
                    {
                        __result = Random.SubGraphicFor(__instance);
                        return false;
                    }
                }
                return true;
            }
        }

        //
        [HarmonyPatch(typeof(ThingWithComps))]
        [HarmonyPatch("Print")]
        public static class PrintPatch
        {
            public static bool Prefix(ThingWithComps __instance, SectionLayer layer)
            {
                ThingDef def = __instance.def;
                if (__instance is Blueprint b)
                {
                    if (b.def.entityDefToBuild is TerrainDef)
                        return true;
                    def = (ThingDef)b.def.entityDefToBuild;
                }
                if (def.HasFXExtension(out var extension))
                {
                    TDrawing.Print(layer, __instance.Graphic, __instance, def, extension);
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(GhostDrawer))]
        [HarmonyPatch("DrawGhostThing")]
        public static class DrawGhostThingPatch
        {
            public static bool Prefix(IntVec3 center, Rot4 rot, ThingDef thingDef, Graphic baseGraphic, Color ghostCol, AltitudeLayer drawAltitude)
            {
                if (!thingDef.HasFXExtension(out var extension)) return true;
                if (baseGraphic == null)
                {
                    baseGraphic = thingDef.graphic;
                }
                Graphic graphic = GhostUtility.GhostGraphicFor(baseGraphic, thingDef, ghostCol);
                Vector3 loc = GenThing.TrueCenter(center, rot, thingDef.Size, drawAltitude.AltitudeFor());
                TDrawing.Draw(graphic, loc, rot, null, thingDef, extension);

                foreach (var t in thingDef.comps)
                {
                    t.DrawGhost(center, rot, thingDef, ghostCol, drawAltitude);
                }
                if (thingDef.PlaceWorkers != null)
                {
                    foreach (var p in thingDef.PlaceWorkers)
                    {
                        p.DrawGhost(thingDef, center, rot, ghostCol);
                    }
                }
                return false;
            }
        }

        //
        [HarmonyPatch(typeof(GhostUtility))]
        [HarmonyPatch(nameof(GhostUtility.GhostGraphicFor))]
        public static class GhostUtilityGhostGraphicForPatch
        {
            public static bool Prefix(ref Graphic __result, Graphic baseGraphic, ThingDef thingDef, Color ghostCol, ThingDef stuff = null)
            {
                //Network Pipe Ghost Graphic Fix
                if (baseGraphic.IsCustomLinked())
                {
                    if (thingDef.useSameGraphicForGhost)
                    {
                        __result = baseGraphic;
                        return false;
                    }
                    int seed = 0;
                    seed = Gen.HashCombine(seed, baseGraphic);
                    seed = Gen.HashCombine(seed, thingDef);
                    seed = Gen.HashCombineStruct(seed, ghostCol);
                    seed = Gen.HashCombine(seed, stuff);
                    if (!GhostUtility.ghostGraphics.TryGetValue(seed, out var value))
                    {
                        value = GraphicDatabase.Get<Graphic_Single>(thingDef.uiIconPath, ShaderTypeDefOf.EdgeDetect.Shader, thingDef.graphicData.drawSize, ghostCol);
                        GhostUtility.ghostGraphics.Add(seed, value);
                    }
                    __result = baseGraphic;
                    return false;
                }
                return true;
            }
        }
    }
}
