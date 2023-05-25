using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TeleCore.Network.IO;

public class NetworkCellIOSimple
{
    private readonly Dictionary<Rot4, RenderIOCell[]> _cellyByRot;

    public NetworkCellIOSimple(string pattern, ThingDef def)
    {
        _cellyByRot = new Dictionary<Rot4, RenderIOCell[]>();
        for (int i = 0; i < 4; i++)
        {
            Rot4 rot = new Rot4(i);
            var rotatedSize = RotatedSize(rot, def.size);
            var rect = new CellRect(0 - rotatedSize.x / 2, 0 - rotatedSize.z / 2, rotatedSize.x, rotatedSize.z).ToArray();
            var cells = NetworkCellIO.RotateIOCells(NetworkCellIO.GetIOArr(NetworkCellIO.DefaultFallbackIfNecessary(pattern, rotatedSize)), rot, def.size);

            var newCells = new List<RenderIOCell>(); //new RenderIOCell[cells.Length*4];
            for (var c = 0; c < cells.Length; c++)
            {
                var cell = rect[c];
                var ioCell = cells[c];
                var cellNorth = cell + GenAdj.CardinalDirections[0];
                var cellEast = cell + GenAdj.CardinalDirections[1];
                var cellSouth = cell + GenAdj.CardinalDirections[2];
                var cellWest = cell + GenAdj.CardinalDirections[3];

                if (!rect.Contains(cellNorth))
                {
                    newCells.Add(new RenderIOCell(new IntVec3Rot(cellNorth, Rot4.North), ioCell.North));
                }

                if (!rect.Contains(cellEast))
                {
                    newCells.Add(new RenderIOCell(new IntVec3Rot(cellEast, Rot4.East), ioCell.East));
                }

                if (!rect.Contains(cellSouth))
                {
                    newCells.Add(new RenderIOCell(new IntVec3Rot(cellSouth, Rot4.South), ioCell.South));
                }

                if (!rect.Contains(cellWest))
                {
                    newCells.Add(new RenderIOCell(new IntVec3Rot(cellWest, Rot4.West), ioCell.West));
                }
            }
            _cellyByRot.Add(rot, newCells.ToArray());
        }
    }

    private static IntVec2 RotatedSize(Rot4 rotation, IntVec2 size)
    {
        return !rotation.IsHorizontal ? size : new IntVec2(size.z, size.x);
    }

    public void Draw(IntVec3 center, ThingDef def, Rot4 rot)
    {
        if (_cellyByRot.TryGetValue(rot, out var cells))
        {
            foreach (var renderIOCell in cells)
            {
                var cell = center + renderIOCell.pos;
                var drawPos = cell.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays);

                switch (renderIOCell.mode)
                {
                    case NetworkIOMode.Input:
                        Graphics.DrawMesh(MeshPool.plane10, drawPos, (renderIOCell.pos.Direction.AsAngle - 180).ToQuat(), TeleContent.IOArrow, 0);
                        break;
                    case NetworkIOMode.Output:
                        Graphics.DrawMesh(MeshPool.plane10, drawPos, renderIOCell.pos.Direction.AsQuat, TeleContent.IOArrow, 0);
                        break;
                    case NetworkIOMode.TwoWay:
                        Graphics.DrawMesh(MeshPool.plane10, drawPos, renderIOCell.pos.Direction.AsQuat, TeleContent.IOArrowTwoWay, 0); 
                        break;
                }
            }
        }
    }
}