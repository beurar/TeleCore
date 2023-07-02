using System.Collections.Generic;
using TeleCore.Network.Utility;
using TeleCore.Primitive;
using Verse;

namespace TeleCore.Network.IO;

/// <summary>
/// </summary>
public class NetworkIO
{
    public NetworkIO(NetIOConfig config, IntVec3 refPos, Rot4 parentRotation)
    {
        Connections = new List<IOCell>();
        VisualCells = new List<IOCell>();

        var cells = config.GetCellsFor(parentRotation);
        foreach (var cell in cells)
        {
            if (cell.mode == NetworkIOMode.None) continue;
            if ((NetworkIOMode.ForRender & cell.mode) != 0)
                VisualCells.Add(new IOCell
                {
                    Pos = new IntVec3Rot(cell.offset + refPos, cell.direction),
                    Mode = cell.mode
                });

            Connections.Add(new IOCell
            {
                Pos = new IntVec3Rot(cell.offset + refPos, cell.direction),
                Mode = cell.mode
            });
        }
    }

    public List<IOCell> Connections { get; }

    public List<IOCell> VisualCells { get; }

    public NetworkIOMode IOModeAt(IntVec3 pos)
    {
        foreach (var cell in Connections)
            if (cell.Pos.Pos == pos)
                return cell.Mode;
        return 0;
    }

    public IOConnectionResult ConnectsTo(NetworkIO other)
    {
        foreach (var cell in Connections)
        foreach (var otherCell in other.Connections)
        {
            if (cell.Pos != otherCell.Interface) continue;

            if (cell.Mode.Matches(otherCell.Mode))
                return new IOConnectionResult
                {
                    In = cell.Pos,
                    Out = cell.Pos + cell.Pos.Dir.Opposite.FacingCell,
                    InMode = cell.Mode,
                    OutMode = otherCell.Mode
                };
            if (otherCell.Mode.Matches(cell.Mode)) //Reverse
                return new IOConnectionResult
                {
                    In = otherCell.Pos,
                    Out = otherCell.Pos + otherCell.Pos.Dir.Opposite.FacingCell,
                    InMode = otherCell.Mode,
                    OutMode = cell.Mode
                };
        }

        return IOConnectionResult.Invalid;
    }
}