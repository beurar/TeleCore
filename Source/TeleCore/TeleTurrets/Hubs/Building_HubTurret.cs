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
    public class Building_HubTurret : Building_TeleTurret
    {
        public Building_TurretHubCore parentHub;

        public override CompRefuelable RefuelComp => parentHub.RefuelComp;
        public override CompPowerTrader PowerComp => parentHub.PowerComp;
        public override CompMannable MannableComp => parentHub.MannableComp;
        public override StunHandler Stunner => parentHub.Stunner;
        public override CompPower ForcedPowerComp => PowerComp;

        public bool NeedsRepair => false;
        

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            ConnectToParent();
            Map.mapDrawer.MapMeshDirty(parentHub.Position, MapMeshFlag.Buildings);
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            parentHub.RemoveHubTurret(this);
            Map.mapDrawer.MapMeshDirty(parentHub.Position, MapMeshFlag.Buildings);
            base.DeSpawn(mode);
        }

        public void ConnectToParent()
        {
            var hub = PlaceWorker_AtTurretHub.FindClosestTurretHub(def, Position, Map);
            hub?.AddHubTurret(this);
        }

        public override void Print(SectionLayer layer)
        {
            base.Print(layer);
        }
    }
}
