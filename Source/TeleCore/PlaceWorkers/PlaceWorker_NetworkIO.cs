using TeleCore.Network.IO;
using UnityEngine;
using Verse;

namespace TeleCore;

public class PlaceWorker_NetworkIO : PlaceWorker
{
    public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
    {
        var network = def.GetCompProperties<CompProperties_Network>();

        if (network.generalIOConfig != null)
        {
            Draw(network.generalIOConfig, center, def, rot);
            return;
        }
        
        foreach (var part in network.networks)
        {
            if (part.netIOConfig != null)
            {
                Draw(part.netIOConfig, center, def, rot);
            }
        }
    }

    public void Draw(NetIOConfig config, IntVec3 center, ThingDef def, Rot4 rot)
    {
        var cells = config.GetCellsFor(rot);

        foreach (var ioCell in cells)
        {
            var cell = center + ioCell.offset;
            var drawPos = cell.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays);

            switch (ioCell.mode)
            {
                case NetworkIOMode.Input:
                    Graphics.DrawMesh(MeshPool.plane10, drawPos, (ioCell.direction.AsAngle - 180).ToQuat(),
                        TeleContent.IOArrow, 0);
                    break;
                case NetworkIOMode.Output:
                    Graphics.DrawMesh(MeshPool.plane10, drawPos, ioCell.direction.AsQuat, TeleContent.IOArrow,
                        0);
                    break;
                case NetworkIOMode.TwoWay:
                    Graphics.DrawMesh(MeshPool.plane10, drawPos, ioCell.direction.AsQuat,
                        TeleContent.IOArrowTwoWay, 0);
                    break;
            }
        }
    }
}