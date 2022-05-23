using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace TeleCore
{
    public class PlaceWorker_AtTurretHub : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            if (checkingDef is ThingDef def && def.HasTeleExtension(out var extension) && extension.turret.hub != null)
            {
                var nearbyHub = FindClosestTurretHub(def, loc, map);
                if (nearbyHub == null)
                {
                    return new AcceptanceReport("TR_HubTurretMissingHub".Translate());
                }
                return true;
            }
            return true;
        }

        public override void PostPlace(Map map, BuildableDef def, IntVec3 loc, Rot4 rot)
        {
            var hub = FindClosestTurretHub(def as ThingDef, loc, Find.CurrentMap);
            hub?.AnticipateTurretAt(loc);
        }

        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            if (!def.HasTeleExtension(out var extension)) return;
            if (extension.turret.hub?.hubDef != null)
            {
                var allHubs = Find.CurrentMap.listerBuildings.AllBuildingsColonistOfDef(extension.turret.hub.hubDef);
                foreach (var building in allHubs)
                {
                    if (!(building is Building_TurretHubCore turretHub)) continue;
                    if (!turretHub.AcceptsTurrets) continue;

                    //Adjust radius because circle does not represent the actual amount of cells very well
                    GenDraw.DrawCircleOutline(turretHub.DrawPos, turretHub.Extension.hub.connectRadius + 0.5f, SimpleColor.Blue);
                }
                var hub = FindClosestTurretHub(def, center, Find.CurrentMap);
                if (hub == null) return;
                DrawHubConnectionGhost(center, def, hub);
            }
        }

        //
        public static Building_TurretHubCore FindClosestTurretHub(ThingDef turretDef, IntVec3 origin, Map map)
        {
            _ = turretDef.HasTeleExtension(out var extension);
            var numCells = GenRadial.NumCellsInRadius(extension.turret.hub.connectRadius);
            for (int i = 0; i < numCells; i++)
            {
                IntVec3 cell = GenRadial.RadialPattern[i] + origin;
                if (!cell.InBounds(map)) continue;
                Building_TurretHubCore hub = (Building_TurretHubCore)cell.GetFirstThing(map, extension.turret.hub.hubDef);
                if (hub != null && hub.AcceptsTurrets)
                {
                    return hub;
                }
            }
            return null;
        }

        public static void DrawHubConnectionGhost(IntVec3 from, ThingDef turretDef, Building_TurretHubCore hubCore)
        {
            Material cableMat = MaterialPool.MatFrom(hubCore.Extension.hub.cableTexturePath, ShaderTypeDefOf.EdgeDetect.Shader);

            Vector3 placingTurret = GenThing.TrueCenter(from, Rot4.North, turretDef.size, AltitudeLayer.MapDataOverlay.AltitudeFor());
            Vector3 hubPos = hubCore.TrueCenter();
            hubPos.y = AltitudeLayer.MapDataOverlay.AltitudeFor();

            Vector3 meanPosBetween = (placingTurret + hubPos) / 2f;
            Vector3 vectorBetween = hubPos - placingTurret;
            Vector3 s = new Vector3(1f, 1f, vectorBetween.MagnitudeHorizontal());
            Quaternion q = Quaternion.LookRotation(hubPos - placingTurret);
            Matrix4x4 matrix = default;
            matrix.SetTRS(meanPosBetween, q, s);
            Graphics.DrawMesh(MeshPool.plane10, matrix, cableMat, 0);
        }
    }
}
