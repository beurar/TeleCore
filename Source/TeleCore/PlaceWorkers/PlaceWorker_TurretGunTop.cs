using RimWorld;
using UnityEngine;
using Verse;

namespace TeleCore;

/// <summary>
///     Renders all turrets defined in TeleDefExtension ontop of the placable thing.
/// </summary>
public class PlaceWorker_TurretGunTop : PlaceWorker
{
    public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
    {
        var extension = def.TurretExtension();
        foreach (var turret in extension.turrets)
        {
            var graphic = GhostUtility.GhostGraphicFor(turret.turretTop.topGraphic.Graphic, def, ghostCol);
            graphic.DrawFromDef(
                GenThing.TrueCenter(center, rot, def.Size, AltitudeLayer.MetaOverlays.AltitudeFor()) +
                turret.turretOffset, rot, def);
        }
    }
}