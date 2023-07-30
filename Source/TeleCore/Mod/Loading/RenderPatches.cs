using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using TeleCore.Network.IO;
using UnityEngine;
using Verse;

namespace TeleCore;

internal static class RenderPatches
{
    //Equipment Size Fix
    //Draw Equipment Size Fix
    /*
    [HarmonyPatch(typeof(PawnRenderer)), HarmonyPatch(nameof(PawnRenderer.DrawEquipmentAiming))]
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
    */

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

    [HarmonyPatch(typeof(Graphic_Random), nameof(Graphic_Random.SubGraphicFor))]
    public static class SubGraphcForPatch
    {
        private static bool Prefix(Graphic_Random __instance, ref Graphic __result)
        {
            if (__instance is Graphic_RandomExtra extra)
                if (Rand.Chance(extra.ParamRandChance))
                {
                    __result = TeleContent.ClearGraphic;
                    return false;
                }

            return true;
        }
    }

    //
    //Fix projectile random graphics
    [HarmonyPatch(typeof(Thing), "Graphic", MethodType.Getter)]
    public static class ThingGraphicPatch
    {
        public static bool Prefix(Thing __instance, ref Graphic __result)
        {
            if (__instance is Projectile && TeleCoreMod.Settings.ProjectileGraphicRandomFix)
                if (__instance.DefaultGraphic is Graphic_Random Random)
                {
                    __result = Random.SubGraphicFor(__instance);
                    return false;
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
            var def = __instance.def;
            if (__instance is Blueprint b)
            {
                if (b.def.entityDefToBuild is TerrainDef)
                    return true;
                def = (ThingDef) b.def.entityDefToBuild;
            }

            if (def.HasFXExtension(out var extension))
            {
                TDrawing.Print(layer, __instance.Graphic, __instance, def, extension);
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(GenDraw))]
    [HarmonyPatch(nameof(GenDraw.DrawInteractionCells))]
    public static class GenDrawDrawInteractionCellsPatch
    {
        public static void Postfix(ThingDef tDef, IntVec3 center, Rot4 placingRot)
        {
            //Draw Network IO
            var network = tDef.GetCompProperties<CompProperties_Network>();
            if (network != null)
            {
                if (network.generalIOConfig != null)
                {
                    DrawNetIOConfig(network.generalIOConfig, center, tDef, placingRot);
                    return;
                }

                foreach (var part in network.networks)
                    if (part.netIOConfig != null)
                        DrawNetIOConfig(part.netIOConfig, center, tDef, placingRot);
            }
        }
        
        private static void DrawNetIOConfig(NetIOConfig config, IntVec3 center, ThingDef def, Rot4 rot)
        {
            var cells = config.GetCellsFor(rot);
            foreach (var ioCell in cells)
            {
                var cell = center + ioCell.offset;
                var drawPos = cell.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays);

                switch (ioCell.mode)
                {
                    case NetworkIOMode.Input:
                        Graphics.DrawMesh(MeshPool.plane10, drawPos, (ioCell.direction.AsAngle - 180).ToQuat(),TeleContent.IOArrow, 0);
                        break;
                    case NetworkIOMode.Output:
                        Graphics.DrawMesh(MeshPool.plane10, drawPos, ioCell.direction.AsQuat, TeleContent.IOArrow,0);
                        break;
                    case NetworkIOMode.TwoWay:
                        Graphics.DrawMesh(MeshPool.plane10, drawPos, ioCell.direction.AsQuat,TeleContent.IOArrowTwoWay, 0);
                        break;
                }
            }
        }
    }


    [HarmonyPatch(typeof(GhostDrawer))]
    [HarmonyPatch("DrawGhostThing")]
    public static class DrawGhostThingPatch
    {
        public static bool Prefix(IntVec3 center, Rot4 rot, ThingDef thingDef, Graphic baseGraphic, Color ghostCol,
            AltitudeLayer drawAltitude)
        {
            if (!thingDef.HasFXExtension(out var extension)) return true;
            if (baseGraphic == null) baseGraphic = thingDef.graphic;
            var graphic = GhostUtility.GhostGraphicFor(baseGraphic, thingDef, ghostCol);
            var loc = GenThing.TrueCenter(center, rot, thingDef.Size, drawAltitude.AltitudeFor());
            TDrawing.Draw(graphic, loc, rot, null, thingDef, null, extension);

            foreach (var t in thingDef.comps) 
                t.DrawGhost(center, rot, thingDef, ghostCol, drawAltitude);
            if (thingDef.PlaceWorkers != null)
                foreach (var p in thingDef.PlaceWorkers)
                    p.DrawGhost(thingDef, center, rot, ghostCol);
            return false;
        }
    }

    //
    [HarmonyPatch(typeof(GhostUtility))]
    [HarmonyPatch(nameof(GhostUtility.GhostGraphicFor))]
    public static class GhostUtilityGhostGraphicForPatch
    {
        public static bool Prefix(ref Graphic __result, Graphic baseGraphic, ThingDef thingDef, Color ghostCol,
            ThingDef stuff = null)
        {
            //Network Pipe Ghost Graphic Fix
            if (baseGraphic.IsCustomLinked())
            {
                if (thingDef.useSameGraphicForGhost)
                {
                    __result = baseGraphic;
                    return false;
                }

                var seed = 0;
                seed = Gen.HashCombine(seed, baseGraphic);
                seed = Gen.HashCombine(seed, thingDef);
                seed = Gen.HashCombineStruct(seed, ghostCol);
                seed = Gen.HashCombine(seed, stuff);
                if (!GhostUtility.ghostGraphics.TryGetValue(seed, out var value))
                {
                    value = GraphicDatabase.Get<Graphic_Single>(thingDef.uiIconPath, ShaderTypeDefOf.EdgeDetect.Shader,
                        thingDef.graphicData.drawSize, ghostCol);
                    GhostUtility.ghostGraphics.Add(seed, value);
                }

                __result = baseGraphic;
                return false;
            }

            return true;
        }
    }


    //
    [HarmonyPatch(typeof(CameraDriver))]
    [HarmonyPatch(nameof(CameraDriver.Update))]
    internal static class CameraDriverPatch
    {
        public static void Postfix(CameraDriver __instance)
        {
            var r = __instance.config.sizeRange;
            var value = Mathf.InverseLerp(r.min, r.max, __instance.rootSize);
            Shader.SetGlobalFloat(TeleShaderIDs.CameraZoom, value);
        }
    }

    [HarmonyPatch(typeof(WindManager))]
    [HarmonyPatch(nameof(WindManager.WindManagerTick))]
    internal static class WindManagerPatch
    {
        public static void Postfix(WindManager __instance)
        {
            Shader.SetGlobalFloat(TeleShaderIDs.WindSpeed, __instance.cachedWindSpeed);
        }
    }
}