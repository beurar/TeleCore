using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using TeleCore.Defs;
using UnityEngine;
using Verse;

namespace TeleCore
{

    public class SectionLayer_NetworkGrid : SectionLayer_Things
    {
        internal static NetworkDef CURRENT_NETWORK;
        private readonly Dictionary<NetworkDef, List<LayerSubMesh>> SubmeshesByNetwork = new();

        public SectionLayer_NetworkGrid(Section section) : base(section)
        {
            this.requireAddToMapMesh = false;
            this.relevantChangeTypes = MapMeshFlag.Buildings;
        }

        public static NetworkDef[] NetworksFromDesignator(Designator designator)
        {
            if (designator is not Designator_Build build) return null;
            return ((build.PlacingDef as ThingDef)?.comps.Find(c => c is CompProperties_Network) as CompProperties_Network)?.networks?.Select(n => n.networkDef).ToArray();
        }

        private void DrawSubLayer(NetworkDef def)
        {
            if (!Visible) return;
            List<LayerSubMesh> subMeshes;

            if (!SubmeshesByNetwork.TryGetValue(def, out subMeshes)) return;
            int count = subMeshes.Count;
            for (int i = 0; i < count; i++)
            {
                LayerSubMesh layerSubMesh = subMeshes[i];
                if (layerSubMesh.finalized && !layerSubMesh.disabled)
                {
                    Graphics.DrawMesh(layerSubMesh.mesh, Matrix4x4.identity, layerSubMesh.material, 0);
                }
            }
        }

        public override void DrawLayer()
        {
            if (Find.DesignatorManager.SelectedDesignator is Designator_Build designator)
            {
                var defs = NetworksFromDesignator(designator);
                if(defs == null) return;

                for (int i = 0; i < defs.Length; i++)
                {
                    DrawSubLayer(defs[i]);
                }
            }
        }

        protected void ClearSubMeshesFull(MeshParts parts)
        {
            foreach (var subMeshList in SubmeshesByNetwork)
            {
                if (subMeshList.Value.NullOrEmpty()) continue;
                foreach (var subMesh in subMeshList.Value)
                {
                    subMesh.Clear(parts);
                }
            }
        }

        protected void FinalizeMeshFull(MeshParts tags)
        {
            foreach (var subMeshList in SubmeshesByNetwork)
            {
                if (subMeshList.Value.NullOrEmpty()) continue;
                subMeshList.Value.RemoveAll(t => t.verts.Count == 0 || t.tris.Count == 0);
                foreach (var subMesh in subMeshList.Value)
                {
                    subMesh.FinalizeMesh(tags);
                }
            }
        }

        public override void Regenerate()
        {
            ClearSubMeshesFull(MeshParts.All);
            foreach (IntVec3 item in section.CellRect)
            {
                List<Thing> list = Map.thingGrid.ThingsListAt(item);
                int count = list.Count;
                for (int i = 0; i < count; i++)
                {
                    Thing thing = list[i];
                    if ((thing.def.seeThroughFog || !Map.fogGrid.fogGrid[CellIndicesUtility.CellToIndex(thing.Position, base.Map.Size.x)]) && thing.def.drawerType != 0 && (thing.def.drawerType != DrawerType.RealtimeOnly || !requireAddToMapMesh) && (!(thing.def.hideAtSnowDepth < 1f) || !(base.Map.snowGrid.GetDepth(thing.Position) > thing.def.hideAtSnowDepth)) && thing.Position.x == item.x && thing.Position.z == item.z)
                    {
                        TakePrintFrom(thing);
                    }
                }
            }
            FinalizeMeshFull(MeshParts.All);
        }

        public LayerSubMesh GetSubMeshCurrent(Material material)
        {
            if (material == null) return null;

            List<LayerSubMesh> subMeshes;
            if (!SubmeshesByNetwork.TryGetValue(CURRENT_NETWORK, out subMeshes))
            {
                subMeshes = new List<LayerSubMesh>();
                SubmeshesByNetwork.Add(CURRENT_NETWORK, subMeshes);
            }
            else
            {
                for (int i = 0; i < subMeshes.Count; i++)
                {
                    if (subMeshes[i].material == material)
                    {
                        return subMeshes[i];
                    }
                }
            }

            Mesh mesh = new Mesh();
            Bounds value = new Bounds(section.botLeft.ToVector3(), Vector3.zero);
            value.Encapsulate(section.botLeft.ToVector3() + new Vector3(17f, 0f, 0f));
            value.Encapsulate(section.botLeft.ToVector3() + new Vector3(17f, 0f, 17f));
            value.Encapsulate(section.botLeft.ToVector3() + new Vector3(0f, 0f, 17f));
            LayerSubMesh layerSubMesh = new LayerSubMesh(mesh, material, value);
            SubmeshesByNetwork[CURRENT_NETWORK].Add(layerSubMesh);
            return layerSubMesh;
        }

        public override void TakePrintFrom(Thing t)
        {
            var comp = t.TryGetComp<Comp_Network>();
            if (comp == null) return;
            foreach (var networkComponent in comp.NetworkParts)
            {
                CURRENT_NETWORK = networkComponent.Config.networkDef;
                CURRENT_NETWORK.OverlayGraphic?.Print(this, t, 0);
                CURRENT_NETWORK = null;
            }
        }
    }

    public static class SectionLayerPatches
    {
        [HarmonyPatch(typeof(Printer_Plane))]
        [HarmonyPatch(nameof(Printer_Plane.PrintPlane))]
        public static class Printer_Plane_PrintPlanePatch
        {
            public static bool Prefix(SectionLayer layer, Vector3 center, Vector2 size, Material mat, float rot = 0f, bool flipUv = false, Vector2[] uvs = null, Color32[] colors = null, float topVerticesAltitudeBias = 0.01f, float uvzPayload = 0f)
            {
                if (layer is not SectionLayer_NetworkGrid networkLayer) return true;
                if (colors == null)
                {
                    colors = Printer_Plane.defaultColors;
                }
                if (uvs == null)
                {
                    uvs = (flipUv ? Printer_Plane.defaultUvsFlipped : Printer_Plane.defaultUvs);
                }
                LayerSubMesh subMesh = networkLayer.GetSubMeshCurrent(mat);
                int count = subMesh.verts.Count;
                subMesh.verts.Add(new Vector3(-0.5f * size.x, 0f, -0.5f * size.y));
                subMesh.verts.Add(new Vector3(-0.5f * size.x, topVerticesAltitudeBias, 0.5f * size.y));
                subMesh.verts.Add(new Vector3(0.5f * size.x, topVerticesAltitudeBias, 0.5f * size.y));
                subMesh.verts.Add(new Vector3(0.5f * size.x, 0f, -0.5f * size.y));
                if (rot != 0f)
                {
                    float num = rot * ((float)Math.PI / 180f);
                    num *= -1f;
                    for (int i = 0; i < 4; i++)
                    {
                        float x = subMesh.verts[count + i].x;
                        float z = subMesh.verts[count + i].z;
                        float num2 = Mathf.Cos(num);
                        float num3 = Mathf.Sin(num);
                        float x2 = x * num2 - z * num3;
                        float z2 = x * num3 + z * num2;
                        subMesh.verts[count + i] = new Vector3(x2, subMesh.verts[count + i].y, z2);
                    }
                }
                for (int j = 0; j < 4; j++)
                {
                    subMesh.verts[count + j] += center;
                    subMesh.uvs.Add(new Vector3(uvs[j].x, uvs[j].y, uvzPayload));
                    subMesh.colors.Add(colors[j]);
                }
                subMesh.tris.Add(count);
                subMesh.tris.Add(count + 1);
                subMesh.tris.Add(count + 2);
                subMesh.tris.Add(count);
                subMesh.tris.Add(count + 2);
                subMesh.tris.Add(count + 3);
                return false;
            }
        }
    }
}
