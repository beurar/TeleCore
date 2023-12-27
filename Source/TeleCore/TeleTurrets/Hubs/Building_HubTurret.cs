using RimWorld;
using TeleCore.Data.Events;
using Verse;

namespace TeleCore;

public class Building_HubTurret : Building_TeleTurret
{
    public Building_TurretHubCore parentHub;

    public override CompRefuelable RefuelComp => parentHub.RefuelComp;
    public override CompPowerTrader PowerComp => parentHub.PowerComp;
    public override CompMannable MannableComp => parentHub.MannableComp;
    public override StunHandler Stunner => parentHub.Stunner;

    public bool NeedsRepair => false;

    //
    public override CompPowerTrader FX_PowerProviderFor(FXArgs args)
    {
        return PowerComp;
    }


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
        if (hub == null)
        {
            TLog.Error($"{this} failed to find a parent hub! Destroying...");
            Destroy();
            return;
        }
        hub?.AddHubTurret(this);
    }

    public override void Print(SectionLayer layer)
    {
        base.Print(layer);
    }
}